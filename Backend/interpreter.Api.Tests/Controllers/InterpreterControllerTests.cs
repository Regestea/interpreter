using interpreter.Api.Controllers;
using interpreter.Api.Models;
using interpreter.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opus.Services;
using Xunit.Abstractions;

namespace interpreter.Api.Tests.Controllers;

/// <summary>
/// Integration tests for InterpreterController testing the complete audio translation pipeline.
/// These tests use real services (no mocking) to verify the entire workflow.
/// </summary>
public class InterpreterControllerTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly InterpreterController _controller;
    private readonly WhisperService? _whisperService;
    private readonly PiperService? _piperService;
    private readonly OpusCodecService _opusCodecService;
    private readonly TranslationService _translationService;
    private readonly string _testAudioPath;
    private readonly string _whisperModelPath;
    
    public InterpreterControllerTests(ITestOutputHelper output)
    {
        _output = output;
        _testAudioPath = Path.Combine(AppContext.BaseDirectory, "AudioSample", "sample-audio.wav");
        _whisperModelPath = Path.Combine(AppContext.BaseDirectory, "WhisperTestModel", "ggml-tiny.bin");
        
        // Setup real services
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // Setup WhisperService
        if (File.Exists(_whisperModelPath))
        {
            var whisperSettings = new WhisperSettings
            {
                ModelPath = _whisperModelPath,
                Language = "auto"
            };
            var whisperOptions = Options.Create(whisperSettings);
            var whisperLogger = loggerFactory.CreateLogger<WhisperService>();
            _whisperService = new WhisperService(whisperOptions, whisperLogger);
            _output.WriteLine($"WhisperService initialized with model: {_whisperModelPath}");
        }
        else
        {
            _output.WriteLine($"Whisper model not found at: {_whisperModelPath}");
        }
        
        // Setup PiperService
        try
        {
            // Ensure Piper is extracted
            PiperSharp.PiperDataExtractor.EnsurePiperExtracted();
            
            var piperSettings = new PiperSettings
            {
                DefaultModel = "en_US-hfc_female-medium",
                SpeakingRate = 1.0f,
                SpeakerId = 0,
                UseCuda = false
            };
            var piperOptions = Options.Create(piperSettings);
            var piperLogger = loggerFactory.CreateLogger<PiperService>();
            _piperService = new PiperService(piperOptions, piperLogger);
            _output.WriteLine("PiperService initialized successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to initialize PiperService: {ex.Message}");
        }
        
        // Setup OpusCodecService
        _opusCodecService = new OpusCodecService();
        _output.WriteLine("OpusCodecService initialized");
        
        // Setup TranslationService
        var httpClientFactory = new TestHttpClientFactory();
        var translationLogger = loggerFactory.CreateLogger<TranslationService>();
        _translationService = new TranslationService(httpClientFactory, translationLogger);
        _output.WriteLine("TranslationService initialized");
        
        // Create controller with real services
        var controllerLogger = loggerFactory.CreateLogger<InterpreterController>();
        _controller = new InterpreterController(
            controllerLogger,
            _whisperService!,
            _opusCodecService,
            _piperService!,
            _translationService
        );
        _output.WriteLine("InterpreterController initialized with all real services");
    }
    
    [Fact]
    public async Task TranslateAudio_WithValidOpusFile_ShouldReturnTranslatedAudio()
    {
        // Skip if required files don't exist
        if (!File.Exists(_testAudioPath) || !File.Exists(_whisperModelPath) || _whisperService == null || _piperService == null)
        {
            _output.WriteLine("Skipping test - required files or services not available");
            return;
        }
        
        // Arrange
        _output.WriteLine("=== Starting TranslateAudio Integration Test ===");
        _output.WriteLine($"Test audio file: {_testAudioPath}");
        
        // Step 1: Encode WAV to Opus to create test input
        _output.WriteLine("\n[Test Setup] Encoding WAV to Opus for test input...");
        await using var wavStream = File.OpenRead(_testAudioPath);
        var originalWavSize = wavStream.Length;
        _output.WriteLine($"Original WAV size: {originalWavSize} bytes ({originalWavSize / 1024.0:F2} KB)");
        
        await using var opusTestStream = await _opusCodecService.EncodeAsync(wavStream);
        _output.WriteLine($"Encoded Opus size: {opusTestStream.Length} bytes ({opusTestStream.Length / 1024.0:F2} KB)");
        
        // Create IFormFile from the Opus stream
        opusTestStream.Position = 0;
        var opusBytes = new byte[opusTestStream.Length];
        await opusTestStream.ReadAsync(opusBytes);
        
        var formFile = new FormFile(
            new MemoryStream(opusBytes),
            0,
            opusBytes.Length,
            "file",
            "test-audio.opus"
        )
        {
            Headers = new HeaderDictionary(),
            ContentType = "audio/opus"
        };
        
        _output.WriteLine($"\n[Test Input] IFormFile created: {formFile.FileName}, Size: {formFile.Length} bytes");
        
        // Act
        _output.WriteLine("\n[Controller Action] Calling TranslateAudio...");
        var result = await _controller.TranslateAudio(formFile, "en", CancellationToken.None);
        
        // Assert
        _output.WriteLine("\n[Assertions] Validating results...");
        Assert.NotNull(result);
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        
        // Get the response object
        dynamic response = okResult.Value!;
        var responseType = response.GetType();
        
        // Verify response structure
        var messageProperty = responseType.GetProperty("message");
        var transcriptionProperty = responseType.GetProperty("transcription");
        var detectedLanguageProperty = responseType.GetProperty("detectedLanguage");
        var targetLanguageProperty = responseType.GetProperty("targetLanguage");
        var translatedTextProperty = responseType.GetProperty("translatedText");
        var audioDataProperty = responseType.GetProperty("audioData");
        var outputFileSizeProperty = responseType.GetProperty("outputFileSize");
        
        Assert.NotNull(messageProperty);
        Assert.NotNull(transcriptionProperty);
        Assert.NotNull(detectedLanguageProperty);
        Assert.NotNull(targetLanguageProperty);
        Assert.NotNull(translatedTextProperty);
        Assert.NotNull(audioDataProperty);
        Assert.NotNull(outputFileSizeProperty);
        
        var message = messageProperty.GetValue(response)?.ToString();
        var transcription = transcriptionProperty.GetValue(response)?.ToString();
        var detectedLanguage = detectedLanguageProperty.GetValue(response)?.ToString();
        var targetLanguage = targetLanguageProperty.GetValue(response)?.ToString();
        var translatedText = translatedTextProperty.GetValue(response)?.ToString();
        var audioData = audioDataProperty.GetValue(response)?.ToString();
        var outputFileSize = (long)outputFileSizeProperty.GetValue(response)!;
        
        // Log the pipeline results
        _output.WriteLine("\n=== Pipeline Results ===");
        _output.WriteLine($"Message: {message}");
        _output.WriteLine($"Transcription: {transcription}");
        _output.WriteLine($"Detected Language: {detectedLanguage}");
        _output.WriteLine($"Target Language: {targetLanguage}");
        _output.WriteLine($"Translated Text: {translatedText}");
        _output.WriteLine($"Output Audio Size: {outputFileSize} bytes ({outputFileSize / 1024.0:F2} KB)");
        _output.WriteLine($"Audio Data (Base64) Length: {audioData?.Length ?? 0} characters");
        
        // Assertions
        Assert.Equal("Audio translated successfully", message);
        Assert.NotNull(transcription);
        Assert.NotEmpty(transcription);
        Assert.NotNull(detectedLanguage);
        Assert.Equal("en", targetLanguage);
        Assert.NotNull(translatedText);
        Assert.NotEmpty(translatedText);
        Assert.NotNull(audioData);
        Assert.NotEmpty(audioData);
        Assert.True(outputFileSize > 0);
        
        // Verify the audio data is valid Base64
        var decodedAudio = Convert.FromBase64String(audioData);
        Assert.True(decodedAudio.Length > 0);
        _output.WriteLine($"Decoded audio bytes: {decodedAudio.Length}");
        
        _output.WriteLine("\n=== Test Completed Successfully ===");
    }
    
    [Fact]
    public async Task TranslateAudio_WithEmptyFile_ShouldReturnBadRequest()
    {
        // Arrange
        _output.WriteLine("Testing with empty file...");
        var emptyFormFile = new FormFile(
            new MemoryStream(),
            0,
            0,
            "file",
            "empty.opus"
        );
        
        // Act
        var result = await _controller.TranslateAudio(emptyFormFile, "en", CancellationToken.None);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        _output.WriteLine($"Correctly returned BadRequest for empty file");
    }
    
    [Fact]
    public async Task TranslateAudio_WithNoExtension_ShouldReturnBadRequest()
    {
        // Arrange
        _output.WriteLine("Testing with file without extension...");
        var formFile = new FormFile(
            new MemoryStream(new byte[100]),
            0,
            100,
            "file",
            "noextension"
        );
        
        // Act
        var result = await _controller.TranslateAudio(formFile, "en", CancellationToken.None);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        _output.WriteLine($"Correctly returned BadRequest for file without extension");
    }
    
    [Fact]
    public async Task TranslateAudio_WithEmptyTargetLanguage_ShouldReturnBadRequest()
    {
        // Arrange
        _output.WriteLine("Testing with empty target language...");
        var formFile = new FormFile(
            new MemoryStream(new byte[100]),
            0,
            100,
            "file",
            "test.opus"
        );
        
        // Act
        var result = await _controller.TranslateAudio(formFile, "", CancellationToken.None);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        _output.WriteLine($"Correctly returned BadRequest for empty target language");
    }
    
    [Fact]
    public async Task TranslateAudio_DifferentTargetLanguages_ShouldTranslateCorrectly()
    {
        // Skip if required files don't exist
        if (!File.Exists(_testAudioPath) || !File.Exists(_whisperModelPath) || _whisperService == null || _piperService == null)
        {
            _output.WriteLine("Skipping test - required files or services not available");
            return;
        }
        
        // Arrange - Create Opus test file
        _output.WriteLine("Testing translation to different target languages...");
        await using var wavStream = File.OpenRead(_testAudioPath);
        await using var opusTestStream = await _opusCodecService.EncodeAsync(wavStream);
        opusTestStream.Position = 0;
        var opusBytes = new byte[opusTestStream.Length];
        await opusTestStream.ReadAsync(opusBytes);
        
        var targetLanguages = new[] { "en", "fa", "fr", "es", "de" };
        
        foreach (var targetLang in targetLanguages)
        {
            _output.WriteLine($"\n--- Testing translation to: {targetLang} ---");
            
            var formFile = new FormFile(
                new MemoryStream(opusBytes),
                0,
                opusBytes.Length,
                "file",
                "test-audio.opus"
            );
            
            // Act
            var result = await _controller.TranslateAudio(formFile, targetLang, CancellationToken.None);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value!;
            
            var targetLanguageProperty = response.GetType().GetProperty("targetLanguage");
            var translatedTextProperty = response.GetType().GetProperty("translatedText");
            
            var actualTargetLang = targetLanguageProperty?.GetValue(response)?.ToString();
            var translatedText = translatedTextProperty?.GetValue(response)?.ToString();
            
            Assert.Equal(targetLang, actualTargetLang);
            Assert.NotNull(translatedText);
            Assert.NotEmpty(translatedText);
            
            _output.WriteLine($"Target: {actualTargetLang}, Translated: {translatedText}");
        }
        
        _output.WriteLine("\n=== All target languages tested successfully ===");
    }
    
    public void Dispose()
    {
        _whisperService?.Dispose();
        _piperService?.Dispose();
    }
    
    /// <summary>
    /// Simple HttpClientFactory implementation for testing
    /// </summary>
    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}