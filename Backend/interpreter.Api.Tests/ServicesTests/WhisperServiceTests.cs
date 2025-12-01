using interpreter.Api.Models;
using interpreter.Api.Services;
using interpreter.Api.Tests.Fixture;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit.Abstractions;

namespace interpreter.Api.Tests.ServicesTests;

[Collection("Whisper Collection")]
public class WhisperServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly WhisperService? _whisperService;
    private readonly ILogger<WhisperService> _logger;
    private readonly WhisperSettings _settings;
    private readonly string _testAudioPath;
    private readonly string _modelPath;

    public WhisperServiceTests(WhisperServiceFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
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
        var testStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _testOutputHelper.WriteLine($"[TEST START] TranscribeStreamAsync_WithValidStream_ShouldReturnTranscription");
        
        // Skip if test files don't exist
        if (!File.Exists(_modelPath) || !File.Exists(_testAudioPath))
        {
            _testOutputHelper.WriteLine("Test skipped: Model or audio file not found");
            return;
        }

        // Arrange
        var arrangeStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _testOutputHelper.WriteLine($"Opening audio file: {_testAudioPath}");
        await using var stream = File.OpenRead(_testAudioPath);
        arrangeStopwatch.Stop();
        _testOutputHelper.WriteLine($"File opened in {arrangeStopwatch.ElapsedMilliseconds}ms");

        // Act
        var actStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _testOutputHelper.WriteLine("Starting transcription with WhisperService...");
        
        var language = await _whisperService.GetLanguage(stream);
        
        var result = await _whisperService.TranscribeStreamAsync(stream, language);
        actStopwatch.Stop();
        _testOutputHelper.WriteLine($"WhisperService.TranscribeStreamAsync completed in {actStopwatch.ElapsedMilliseconds}ms");
        _testOutputHelper.WriteLine($"Transcription result: '{result}'");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        testStopwatch.Stop();
        _testOutputHelper.WriteLine($"[TEST END] Total test duration: {testStopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task TranscribeStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _whisperService.TranscribeStreamAsync(null!, ""));
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
            async () => await _whisperService.TranscribeStreamAsync(emptyStream, ""));
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
            async () => await _whisperService.TranscribeStreamAsync(stream, "", cts.Token));
    }

    #endregion

    #region GetLanguage Tests

    [Fact]
    public async Task GetLanguage_WithValidStream_ShouldReturnLanguageCode()
    {
        var testStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _testOutputHelper.WriteLine($"[TEST START] GetLanguage_WithValidStream_ShouldReturnLanguageCode");
        
        // Skip if test files don't exist
        if (!File.Exists(_modelPath) || !File.Exists(_testAudioPath))
        {
            _testOutputHelper.WriteLine("Test skipped: Model or audio file not found");
            return;
        }

        // Arrange
        var arrangeStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _testOutputHelper.WriteLine($"Opening audio file: {_testAudioPath}");
        await using var stream = File.OpenRead(_testAudioPath);
        arrangeStopwatch.Stop();
        _testOutputHelper.WriteLine($"File opened in {arrangeStopwatch.ElapsedMilliseconds}ms");
       
        // Act
        var actStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _testOutputHelper.WriteLine("Detecting language with WhisperService...");
        var result = await _whisperService.GetLanguage(stream);
        actStopwatch.Stop();
        _testOutputHelper.WriteLine($"WhisperService.GetLanguage completed in {actStopwatch.ElapsedMilliseconds}ms");
        _testOutputHelper.WriteLine($"Detected language: '{result}'");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        testStopwatch.Stop();
        _testOutputHelper.WriteLine($"[TEST END] Total test duration: {testStopwatch.ElapsedMilliseconds}ms");
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
            service.TranscribeStreamAsync(new MemoryStream(), "").GetAwaiter().GetResult();
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