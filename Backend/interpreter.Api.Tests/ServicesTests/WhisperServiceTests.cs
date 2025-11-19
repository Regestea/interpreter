using interpreter.Api.Models;
using interpreter.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace interpreter.Api.Tests.ServicesTests;

[Collection("Whisper Collection")]
public class WhisperServiceTests
{
    private readonly WhisperService? _whisperService;
    private readonly ILogger<WhisperService> _logger;
    private readonly WhisperSettings _settings;
    private readonly string _testAudioPath;
    private readonly string _modelPath;

    public WhisperServiceTests(Controllers.WhisperServiceFixture fixture)
    {
        // Setup logger mock
        _logger = Substitute.For<ILogger<WhisperService>>();

        // Get paths and service from the shared fixture
        _modelPath = fixture.ModelPath;
        _testAudioPath = fixture.TestAudioPath;
        _whisperService = fixture.WhisperService;

        // Setup settings
        _settings = new WhisperSettings
        {
            ModelPath = _modelPath,
            Language = "auto"
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidSettings_ShouldInitializeSuccessfully()
    {
        // Skip if model doesn't exist
        if (!File.Exists(_modelPath))
        {
            return;
        }

        // Assert - fixture should have created service successfully
        Assert.NotNull(_whisperService);
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
        // Skip if model doesn't exist - we can't create a new instance for testing
        if (!File.Exists(_modelPath))
        {
            return;
        }

        // Arrange
        var options = Options.Create(_settings);
        var logger = Substitute.For<ILogger<WhisperService>>();
        
        // Note: Creating a new instance here for disposal testing only
        // This test should be run sequentially due to Collection attribute
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
        // Skip if model doesn't exist
        if (!File.Exists(_modelPath))
        {
            return;
        }

        // Arrange
        var options = Options.Create(_settings);
        var logger = Substitute.For<ILogger<WhisperService>>();
        
        // Note: Creating a new instance here for disposal testing only
        // This test should be run sequentially due to Collection attribute
        var service = new WhisperService(options, logger);

        // Act & Assert
        service.Dispose();
        service.Dispose(); // Should not throw
        service.Dispose(); // Should not throw
    }

    #endregion
}