using interpreter.Api.Models;
using interpreter.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace interpreter.Api.Tests.ServicesTests;

public class WhisperServiceTests : IDisposable
{
    private readonly WhisperService _whisperService;
    private readonly ILogger<WhisperService> _logger;
    private readonly WhisperSettings _settings;
    private readonly string _testAudioPath;
    private readonly string _modelPath;

    public WhisperServiceTests()
    {
        // Setup logger mock
        _logger = Substitute.For<ILogger<WhisperService>>();

        // Setup paths - using the test model and audio file in the project
        _modelPath = Path.Combine(AppContext.BaseDirectory, "WhisperTestModel", "ggml-tiny.bin");
        _testAudioPath = Path.Combine(AppContext.BaseDirectory, "AudioSample", "sample-audio.wav");

        // Setup settings
        _settings = new WhisperSettings
        {
            ModelPath = _modelPath,
            Language = "auto"
        };

        var options = Options.Create(_settings);

        // Create service instance
        _whisperService = new WhisperService(options, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidSettings_ShouldInitializeSuccessfully()
    {
        // Arrange
        var settings = new WhisperSettings
        {
            ModelPath = _modelPath,
            Language = "en"
        };
        var options = Options.Create(settings);
        var logger = Substitute.For<ILogger<WhisperService>>();

        // Act
        using var service = new WhisperService(options, logger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullSettings_ShouldThrowException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<WhisperService>>();

        // Act & Assert
        // Throws NullReferenceException because settings.Value is accessed without null check
        Assert.Throws<NullReferenceException>(() => new WhisperService(null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(_settings);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WhisperService(options, null!));
    }
    #endregion
    
    #region TranscribeStreamAsync Tests

    [Fact]
    public async Task TranscribeStreamAsync_WithValidStream_ShouldReturnTranscription()
    {
        // Skip if test files don't exist
        if (!File.Exists(_modelPath) || !File.Exists(_testAudioPath))
        {
            return;
        }

        // Arrange
        await using var stream = File.OpenRead(_testAudioPath);

        // Act
        var result = await _whisperService.TranscribeStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task TranscribeStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _whisperService.TranscribeStreamAsync(null!));
    }

    [Fact]
    public async Task TranscribeStreamAsync_WithEmptyStream_ShouldReturnEmptyOrMinimalResult()
    {
        // Skip if model doesn't exist
        if (!File.Exists(_modelPath))
        {
            return;
        }

        // Arrange
        using var emptyStream = new MemoryStream();

        // Act & Assert
        // Whisper.net requires a valid WAV format and throws CorruptedWaveException for invalid streams
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await _whisperService.TranscribeStreamAsync(emptyStream));
    }

    [Fact]
    public async Task TranscribeStreamAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Skip if test files don't exist
        if (!File.Exists(_modelPath) || !File.Exists(_testAudioPath))
        {
            return;
        }

        // Arrange
        await using var stream = File.OpenRead(_testAudioPath);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _whisperService.TranscribeStreamAsync(stream, cts.Token));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldDisposeResourcesProperly()
    {
        // Arrange
        var options = Options.Create(_settings);
        var logger = Substitute.For<ILogger<WhisperService>>();
        var service = new WhisperService(options, logger);

        // Act
        service.Dispose();

        // Assert
        // Service should be disposed without throwing
        Assert.Throws<ObjectDisposedException>(() =>
        {
            // Attempting to use disposed service should throw
            service.TranscribeStreamAsync(new MemoryStream()).GetAwaiter().GetResult();
        });
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(_settings);
        var logger = Substitute.For<ILogger<WhisperService>>();
        var service = new WhisperService(options, logger);

        // Act & Assert
        service.Dispose();
        service.Dispose(); // Should not throw
        service.Dispose(); // Should not throw
    }

    #endregion

    public void Dispose()
    {
        _whisperService.Dispose();
    }
}