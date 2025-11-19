using Microsoft.Extensions.Options;
using PiperSharp;
using PiperSharp.Models;
using interpreter.Api.Models;

namespace interpreter.Api.Services
{
    public class PiperService : IPiperService, IDisposable
    {
        private readonly ILogger<PiperService> _logger;
        private readonly PiperSettings _settings;
        private PiperProvider _provider;
        private VoiceModel _currentModel;
        private readonly object _lock = new object();

        public PiperService(IOptions<PiperSettings> settings, ILogger<PiperService> logger)
        {
            _logger = logger;
            _settings = settings.Value;

            // Load the default model
            var defaultModelKey = _settings.DefaultModel ?? "en_US-hfc_female-medium";
            var model = PiperDataExtractor.GetModelByKey(defaultModelKey);
            
            if (model == null)
            {
                var availableModels = PiperDataExtractor.GetAvailableModels();
                if (availableModels.Count == 0)
                {
                    throw new InvalidOperationException("No voice models available. Ensure Piper data has been extracted.");
                }
                
                // Use the first available model
                model = availableModels.First().Value;
                _logger.LogWarning("Default model '{DefaultModel}' not found. Using '{ModelKey}' instead", 
                    defaultModelKey, model.Key);
            }

            _currentModel = model;
            _provider = CreateProvider(_currentModel);
            
            _logger.LogInformation("PiperService initialized with model: {ModelKey}", _currentModel.Key);
        }

        private PiperProvider CreateProvider(VoiceModel model)
        {
            return new PiperProvider(new PiperConfiguration
            {
                ExecutableLocation = PiperDataExtractor.DefaultPiperExecutableLocation,
                WorkingDirectory = PiperDataExtractor.DefaultPiperLocation,
                Model = model,
                SpeakingRate = _settings.SpeakingRate,
                SpeakerId = _settings.SpeakerId,
                UseCuda = _settings.UseCuda
            });
        }

        public async Task<byte[]> TextToSpeechAsync(string text, AudioOutputType outputType = AudioOutputType.Wav, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or empty", nameof(text));
            }

            try
            {
                _logger.LogDebug("Converting text to speech: {TextLength} characters", text.Length);
                var audioData = await _provider.InferAsync(text, outputType, cancellationToken);
                _logger.LogDebug("Text to speech conversion completed: {AudioSize} bytes", audioData.Length);
                return audioData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during text-to-speech conversion");
                throw;
            }
        }

        public Dictionary<string, VoiceModel> GetAvailableModels()
        {
            return PiperDataExtractor.GetAvailableModels();
        }

        public void SetModel(string modelKey)
        {
            lock (_lock)
            {
                var model = PiperDataExtractor.GetModelByKey(modelKey);
                if (model == null)
                {
                    throw new ArgumentException($"Model '{modelKey}' not found", nameof(modelKey));
                }

                _currentModel = model;
                _provider = CreateProvider(_currentModel);
                _logger.LogInformation("Switched to model: {ModelKey}", modelKey);
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}

