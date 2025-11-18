using interpreter.Api.Controllers;
using interpreter.Api.Models;
using interpreter.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Opus.Services;
using Xunit.Abstractions;

namespace interpreter.Api.Tests.Controllers;

public class InterpreterControllerTests : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly InterpreterController _controller;
    private readonly ILogger<InterpreterController> _logger;
    private readonly WhisperService _whisperService;
    private readonly IOpusCodecService _opusCodecService;
    private readonly string _testAudioPath;
    private readonly string _modelPath;

    public InterpreterControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        // Setup logger mock
        _logger = Substitute.For<ILogger<InterpreterController>>();
        
        // Setup paths - using the test model and audio file in the project
        _modelPath = Path.Combine(AppContext.BaseDirectory, "WhisperTestModel", "ggml-tiny.bin");
        _testAudioPath = Path.Combine(AppContext.BaseDirectory, "AudioSample", "sample-audio.wav");

        // Setup WhisperSettings
        var whisperSettings = new WhisperSettings
        {
            ModelPath = _modelPath,
            Language = "auto"
        };
        var whisperOptions = Options.Create(whisperSettings);
        var whisperLogger = Substitute.For<ILogger<WhisperService>>();
        
        // Use real WhisperService to get actual transcription results
        _whisperService = new WhisperService(whisperOptions, whisperLogger);
        
        // Use real OpusCodecService for encoding/decoding
        _opusCodecService = new OpusCodecService();

        // Create controller instance with real services
        _controller = new InterpreterController(_logger, _whisperService, _opusCodecService);
    }

    [Fact]
    public async Task UploadFile_WithEncodedSampleAudio_ShouldReturnOk()
    {
        // Skip if model doesn't exist
        if (!File.Exists(_modelPath))
        {
            return;
        }

        // Arrange - Encode the sample-audio.wav file to Opus format
        await using var wavStream = File.OpenRead(_testAudioPath);
        var encodedOpusStream = await _opusCodecService.EncodeAsync(wavStream);
        
        // Create an IFormFile from the encoded stream
        encodedOpusStream.Position = 0;
        var formFile = CreateFormFile(encodedOpusStream, "sample-audio.opus", "audio/opus");

        // Act - Use real WhisperService to get actual transcription
        var result = await _controller.UploadFile(formFile, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        
        // Verify the response contains expected properties
        var response = okResult.Value;
        Assert.NotNull(response);
        
        // Get the transcription from the response using reflection
        var transcriptionProperty = response.GetType().GetProperty("transcription");
        Assert.NotNull(transcriptionProperty);
        
        var transcription = transcriptionProperty.GetValue(response) as string;
        Assert.NotNull(transcription);
        Assert.NotEmpty(transcription);
        
        // Log the actual transcription for verification
        _logger.LogInformation("Actual Whisper transcription: {Transcription}", transcription);
        
        await encodedOpusStream.DisposeAsync();
    }

    [Fact]
    public async Task UploadFile_WithEmptyFile_ShouldReturnBadRequest()
    {
        // Skip if model doesn't exist
        if (!File.Exists(_modelPath))
        {
            return;
        }

        // Arrange
        var emptyFile = CreateFormFile(new MemoryStream(), "empty.opus", "audio/opus");

        // Act
        var result = await _controller.UploadFile(emptyFile, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UploadFile_WithValidFile_ShouldTranscribeSuccessfully()
    {
        // Skip if model doesn't exist
        if (!File.Exists(_modelPath))
        {
            return;
        }

        // Arrange
        await using var wavStream = File.OpenRead(_testAudioPath);
        var encodedOpusStream = await _opusCodecService.EncodeAsync(wavStream);
        encodedOpusStream.Position = 0;
        
        var formFile = CreateFormFile(encodedOpusStream, "test.opus", "audio/opus");

        // Act - Use real WhisperService
        var result = await _controller.UploadFile(formFile, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        
        // Verify transcription was produced
        var response = okResult.Value;
        var transcriptionProperty = response?.GetType().GetProperty("transcription");
        var transcription = transcriptionProperty?.GetValue(response) as string;
        
        Assert.NotNull(transcription);
        Assert.NotEmpty(transcription);
        
        // Output the actual transcription to console for verification
        _testOutputHelper.WriteLine($"Test transcription result: {transcription}");
        
        await encodedOpusStream.DisposeAsync();
    }

    /// <summary>
    /// Helper method to create an IFormFile from a stream
    /// </summary>
    private static IFormFile CreateFormFile(Stream stream, string fileName, string contentType)
    {
        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns(fileName);
        formFile.Length.Returns(stream.Length);
        formFile.ContentType.Returns(contentType);
        formFile.OpenReadStream().Returns(stream);
        
        return formFile;
    }

    public void Dispose()
    {
        _whisperService?.Dispose();
    }
}

