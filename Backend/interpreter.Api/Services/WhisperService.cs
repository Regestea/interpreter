using Microsoft.Extensions.Options;
using interpreter.Api.Models;
using System.Collections.Concurrent;
using Whisper.net;

namespace interpreter.Api.Services
{
    /// <summary>
    /// A singleton service to handle audio transcription using Whisper.net.
    /// Provides file-based and streaming transcription with proper resource management.
    /// Thread-safe implementation using processor pooling.
    /// </summary>
    public class WhisperService : IWhisperService, IDisposable
    {
        private readonly WhisperFactory _factory;
        private readonly WhisperSettings _settings;
        private readonly ILogger<WhisperService> _logger;
        private readonly ConcurrentBag<WhisperProcessor> _processorPool;
        private readonly SemaphoreSlim _poolSemaphore;
        private bool _disposed;
        private const int MaxPoolSize = 4; // Maximum concurrent transcriptions

        /// <summary>
        /// Initializes a new instance of the WhisperService.
        /// This should be registered as a singleton in the DI container.
        /// </summary>
        /// <param name="settings">Whisper configuration settings.</param>
        /// <param name="logger">Logger instance.</param>
        /// <exception cref="FileNotFoundException">Thrown if the model file does not exist.</exception>
        public WhisperService(IOptions<WhisperSettings> settings, ILogger<WhisperService> logger)
        {
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));


            _logger.LogInformation("Initializing Whisper service with model: {ModelPath}, Language: {Language}",
                _settings.ModelPath, _settings.Language);

            var modelPath = Path.Combine(AppContext.BaseDirectory, _settings.ModelPath);

            if (!File.Exists(modelPath))
            {
                var errorMessage = $"Whisper model file not found at: {modelPath}\n" +
                                   $"Please download a Whisper model file (e.g., ggml-large-v3.bin) from https://huggingface.co/ggerganov/whisper.cpp/tree/main\n" +
                                   $"and place it in the '{Path.GetDirectoryName(modelPath)}' directory.\n" +
                                   $"Update the 'Whisper:ModelPath' setting in appsettings.json if you use a different model.";

                _logger.LogError("Whisper model file not found at: {ModelPath}", modelPath);
                _logger.LogError("Download instructions: {Instructions}", errorMessage);
                throw new FileNotFoundException(errorMessage, modelPath);
            }

            _logger.LogDebug("Loading Whisper model from: {ModelPath}", modelPath);
            _factory = WhisperFactory.FromPath(modelPath);
            _processorPool = new ConcurrentBag<WhisperProcessor>();
            _poolSemaphore = new SemaphoreSlim(MaxPoolSize, MaxPoolSize);

            _logger.LogInformation("Whisper service initialized successfully");
        }

        /// <summary>
        /// Gets or creates a processor from the pool.
        /// </summary>
        private WhisperProcessor GetProcessor()
        {
            if (_processorPool.TryTake(out var processor))
            {
                return processor;
            }

            _logger.LogDebug("Creating new WhisperProcessor instance");
            return _factory.CreateBuilder()
                .WithLanguageDetection()
                .WithLanguage(_settings.Language)
                .Build();
        }

        /// <summary>
        /// Returns a processor to the pool.
        /// </summary>
        private void ReturnProcessor(WhisperProcessor processor)
        {
            if (_processorPool.Count < MaxPoolSize)
            {
                _processorPool.Add(processor);
            }
            else
            {
                processor.Dispose();
            }
        }


        /// <summary>
        /// Transcribes an audio stream.
        /// </summary>
        /// <param name="audioStream">The audio stream to transcribe.</param>
        /// <param name="language">The language code for transcription.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full transcription text.</returns>
        public async Task<string> TranscribeStreamAsync(
            Stream audioStream,
            string language,
            CancellationToken cancellationToken = default)
        {
            if (audioStream == null)
            {
                throw new ArgumentNullException(nameof(audioStream));
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            await _poolSemaphore.WaitAsync(cancellationToken);
            var processor = GetProcessor();

            try
            {
                // Set the language for transcription
                if (!string.IsNullOrWhiteSpace(language))
                {
                    processor.ChangeLanguage(language);
                    _logger.LogDebug("Changed processor language to: {Language}", language);
                }

                _logger.LogDebug("Processing audio stream");

                var transcription = new System.Text.StringBuilder();
                var segmentCount = 0;

                await foreach (var result in processor.ProcessAsync(audioStream, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!string.IsNullOrWhiteSpace(result.Text))
                    {
                        _logger.LogDebug("Transcription segment: {Text}", result.Text);
                        transcription.AppendLine(result.Text);
                        segmentCount++;
                    }
                }

                _logger.LogInformation("Transcription completed with {SegmentCount} segments", segmentCount);
                return transcription.ToString();
            }
            finally
            {
                ReturnProcessor(processor);
                _poolSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets the language from an audio stream.
        /// </summary>
        /// <param name="audioStream">The audio stream to analyze.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The detected or configured language code.</returns>
        public async Task<string> GetLanguageAsync(Stream audioStream, CancellationToken cancellationToken = default)
        {
            if (audioStream == null)
            {
                throw new ArgumentNullException(nameof(audioStream));
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            await _poolSemaphore.WaitAsync(cancellationToken);
            var processor = GetProcessor();
            
            try
            {
                _logger.LogDebug("Detecting language from audio stream");
                
                var language = processor.DetectLanguage(ConvertPcm16StreamToFloatArray(audioStream));
                
                // Reset stream position to allow reusing the stream
                if (audioStream.CanSeek)
                {
                    audioStream.Position = 0;
                }
                
                return language;
            }
            finally
            {
                ReturnProcessor(processor);
                _poolSemaphore.Release();
            }
        }


        private static float[] ConvertPcm16StreamToFloatArray(Stream pcmStream)
        {
            using var ms = new MemoryStream();
            pcmStream.CopyTo(ms);
            var bytes = ms.ToArray();

            int sampleCount = bytes.Length / 2; // هر نمونه 2 بایت
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short pcmValue = (short)(bytes[2 * i] | (bytes[2 * i + 1] << 8)); // little-endian
                samples[i] = pcmValue / 32768f; // تبدیل به float -1 تا 1
            }

            return samples;
        }


        /// <summary>
        /// Disposes the resources used by the WhisperService.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogInformation("Disposing Whisper service");

            try
            {
                // Dispose all pooled processors
                while (_processorPool.TryTake(out var processor))
                {
                    processor?.Dispose();
                }

                _poolSemaphore?.Dispose();
                _factory?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Whisper service disposal");
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}