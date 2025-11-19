using interpreter.Api.Models;
using interpreter.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace interpreter.Api.Tests.Fixture;

/// <summary>
/// Fixture to share WhisperService instance across all tests to avoid parallel model loading
/// </summary>
public class WhisperServiceFixture : IDisposable
{
    public WhisperService? WhisperService { get; }
    public string ModelPath { get; }
    public string TestAudioPath { get; }

    public WhisperServiceFixture()
    {
        ModelPath = Path.Combine(AppContext.BaseDirectory, "WhisperTestModel", "ggml-tiny.bin");
        TestAudioPath = Path.Combine(AppContext.BaseDirectory, "AudioSample", "sample-audio.wav");

        if (File.Exists(ModelPath))
        {
            var whisperSettings = new WhisperSettings
            {
                ModelPath = ModelPath,
                Language = "auto"
            };
            var whisperOptions = Options.Create(whisperSettings);
            var whisperLogger = Substitute.For<ILogger<WhisperService>>();
            
            WhisperService = new WhisperService(whisperOptions, whisperLogger);
        }
    }

    public void Dispose()
    {
        WhisperService?.Dispose();
    }
}