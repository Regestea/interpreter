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

            if (!File.Exists(_settings.ModelPath))
            {
                throw new FileNotFoundException("Whisper model file not found.", _settings.ModelPath);
            }

            _logger.LogInformation("Initializing Whisper service with model: {ModelPath}, Language: {Language}", 
                _settings.ModelPath, _settings.Language);

            _factory = WhisperFactory.FromPath(_settings.ModelPath);
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
        /// Transcribes an audio file.
        /// </summary>
        /// <param name="audioPath">The path to the audio file.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full transcription text.</returns>
        public async Task<string> TranscribeAsync(
            string audioPath,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(audioPath))
            {
                throw new FileNotFoundException("Audio file not found.", audioPath);
            }

            _logger.LogInformation("Starting transcription of file: {AudioPath}", audioPath);

            await using var fileStream = File.OpenRead(audioPath);
            return await TranscribeStreamAsync(fileStream, cancellationToken);
        }

        /// <summary>
        /// Transcribes an audio stream.
        /// </summary>
        /// <param name="audioStream">The audio stream to transcribe.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full transcription text.</returns>
        public async Task<string> TranscribeStreamAsync(
            Stream audioStream,
            CancellationToken cancellationToken = default)
        {
            if (audioStream == null)
            {
                throw new ArgumentNullException(nameof(audioStream));
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            var transcription = new System.Text.StringBuilder();
            var segmentCount = 0;

            await foreach (var segment in TranscribeStreamingAsync(audioStream, cancellationToken))
            {
                transcription.AppendLine(segment);
                segmentCount++;
            }

            _logger.LogInformation("Transcription completed with {SegmentCount} segments", segmentCount);
            return transcription.ToString();
        }

        /// <summary>
        /// Transcribes an audio stream and yields results as they become available.
        /// </summary>
        /// <param name="audioStream">The audio stream to transcribe.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>An async enumerable of transcription segments.</returns>
        public async IAsyncEnumerable<string> TranscribeStreamingAsync(
            Stream audioStream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                _logger.LogDebug("Processing audio stream");

                await foreach (var result in processor.ProcessAsync(audioStream, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (!string.IsNullOrWhiteSpace(result.Text))
                    {
                        _logger.LogDebug("Transcription segment: {Text}", result.Text);
                        yield return result.Text;
                    }
                }
            }
            finally
            {
                ReturnProcessor(processor);
                _poolSemaphore.Release();
            }
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