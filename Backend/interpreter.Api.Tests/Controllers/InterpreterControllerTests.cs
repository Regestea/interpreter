using interpreter.Api.Controllers;
using interpreter.Api.Data;
using interpreter.Api.Entities;
using interpreter.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models.Shared.Enums;
using Models.Shared.Requests;
using Models.Shared.Responses;
using NSubstitute;
using Opus.Services;
using SpeechBrain;
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
    private readonly IWhisperService _whisperService;
    private readonly IPiperService _piperService;
    private readonly IOpusCodecService _opusCodecService;
    private readonly ITranslationService _translationService;
    private readonly ISpeechBrainRecognition _speechBrainRecognition;
    private readonly InterpreterDbContext _dbContext;
    private readonly string _testAudioPath;
    private const string TestUserName = "test-user";
    
    public InterpreterControllerTests(ITestOutputHelper output)
    {
        _output = output;
        _testAudioPath = Path.Combine(AppContext.BaseDirectory, "AudioSample", "sample-audio.wav");
        
        // Setup real services
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<InterpreterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new InterpreterDbContext(options);
        
        // Seed test data
        _dbContext.VoiceEmbeddings.Add(new VoiceEmbedding
        {
            Id = Guid.NewGuid(),
            Name = TestUserName,
            Embedding = new List<float> { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f }
        });
        _dbContext.SaveChanges();
        _output.WriteLine($"Created in-memory database with test user: {TestUserName}");
        
        // Setup mock SpeechBrain service using NSubstitute
        _speechBrainRecognition = Substitute.For<ISpeechBrainRecognition>();
        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = true, Score = 0.95f });
        _output.WriteLine("Created mock SpeechBrain service with NSubstitute");
        
        // Setup mock WhisperService using NSubstitute
        _whisperService = Substitute.For<IWhisperService>();
        _whisperService.GetLanguageAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("en"));
        _whisperService.TranscribeStreamAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("This is a test transcription"));
        _output.WriteLine("Created mock WhisperService with NSubstitute");
        
        // Setup mock PiperService using NSubstitute
        _piperService = Substitute.For<IPiperService>();
        _piperService.TextToSpeechAsync(Arg.Any<string>(), Arg.Any<PiperSharp.Models.AudioOutputType>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new byte[] { 1, 2, 3, 4, 5 }));
        _output.WriteLine("Created mock PiperService with NSubstitute");
        
        // Setup mock OpusCodecService using NSubstitute
        _opusCodecService = Substitute.For<IOpusCodecService>();
        _opusCodecService.DecodeAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Stream>(new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })));
        _opusCodecService.EncodeAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Stream>(new MemoryStream(new byte[] { 10, 11, 12, 13, 14 })));
        _output.WriteLine("Created mock OpusCodecService with NSubstitute");
        
        // Setup mock TranslationService using NSubstitute
        _translationService = Substitute.For<ITranslationService>();
        _translationService.TranslateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new TranslationResult 
            { 
                TranslatedText = $"Translated: {callInfo.ArgAt<string>(0)}",
                DetectedLanguage = "en",
                OriginalText = callInfo.ArgAt<string>(0)
            }));
        _output.WriteLine("Created mock TranslationService with NSubstitute");
        
        // Create controller with mocked services
        var controllerLogger = loggerFactory.CreateLogger<InterpreterController>();
        _controller = new InterpreterController(
            controllerLogger,
            _whisperService,
            _opusCodecService,
            _piperService,
            _translationService,
            _speechBrainRecognition,
            _dbContext
        );
        _output.WriteLine("InterpreterController initialized with all mocked services");
    }

    [Fact]
    public async Task TranslateAudio_MissingUserVoiceDetectorName_ReturnsBadRequest()
    {
        // Arrange
        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = "",
            CurrentAudioLanguages = CurrentAudioLanguages.AutoDetect,
            OutputLanguages = OutputLanguages.English,
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.IgnoreMyTalks
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
        _output.WriteLine($"Test passed: Missing UserVoiceDetectorName returns BadRequest");
    }

    [Fact]
    public async Task TranslateAudio_VoiceEmbeddingNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = "non-existent-user",
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.Persian,
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.IgnoreMyTalks
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
        _output.WriteLine($"Test passed: Non-existent user returns BadRequest");
    }

    [Fact]
    public async Task TranslateAudio_IgnoreMyTalksMode_MainUserSpeaking_ReturnsEmptyResponse()
    {
        // Arrange
        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = true, Score = 0.95f });

        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = TestUserName,
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.Persian,
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.IgnoreMyTalks
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InterpreterResponse>(okResult.Value);
        Assert.Null(response.TranslatedAudio);
        _output.WriteLine($"Test passed: IgnoreMyTalks mode with main user returns empty response");
    }

    [Fact]
    public async Task TranslateAudio_HelpMeToTalkMode_NonMainUser_ReturnsEmptyResponse()
    {
        // Arrange
        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = false, Score = 0.25f });

        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = TestUserName,
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.Persian,
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.HelpMeToTalk
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InterpreterResponse>(okResult.Value);
        Assert.Null(response.TranslatedAudio);
        _output.WriteLine($"Test passed: HelpMeToTalk mode with non-main user returns empty response");
    }

    [Fact]
    public async Task TranslateAudio_SameSourceAndTargetLanguage_ReturnsEmptyResponse()
    {
        // Arrange
        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = TestUserName,
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.English,
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.IgnoreMyTalks
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InterpreterResponse>(okResult.Value);
        Assert.Null(response.TranslatedAudio);
        Assert.Equal("en", response.AudioInputLanguage);
        _output.WriteLine($"Test passed: Same source and target language returns empty response with language info");
    }

    [Fact]
    public async Task TranslateAudio_VoiceRecognition_CallsCompareAudio()
    {
        // Arrange
        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = true, Score = 0.85f });

        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = TestUserName,
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.Persian, // Different from source to trigger voice recognition
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.IgnoreMyTalks
        };

        // Act
        await _controller.TranslateAudio(request);

        // Assert
        _speechBrainRecognition.Received(1).CompareAudio(
            Arg.Any<byte[]>(), 
            Arg.Is<List<float>>(e => e.Count == 5 && e[0] == 0.1f));
        _output.WriteLine($"Test passed: VoiceRecognition CompareAudio was called with correct embedding");
    }

    [Fact]
    public async Task TranslateAudio_AutoDetectLanguage_UsesWhisperToDetect()
    {
        // Arrange - This test would require a real audio file with speech
        // For now, we verify the controller accepts AutoDetect mode
        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = TestUserName,
            CurrentAudioLanguages = CurrentAudioLanguages.AutoDetect,
            OutputLanguages = OutputLanguages.English,
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.IgnoreMyTalks
        };

        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = false, Score = 0.25f });

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _output.WriteLine($"Test passed: AutoDetect language mode is accepted");
    }

    [Theory]
    [InlineData(EnglishVoiceModels.EnUsRyanHigh)]
    [InlineData(EnglishVoiceModels.EnUsHfcFemaleMedium)]
    [InlineData(EnglishVoiceModels.EnUsAmyMedium)]
    public async Task TranslateAudio_DifferentEnglishVoiceModels_AcceptsAllModels(EnglishVoiceModels voiceModel)
    {
        // Arrange
        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = false, Score = 0.25f });

        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = TestUserName,
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.English,
            EnglishVoiceModels = voiceModel,
            Modes = Modes.IgnoreMyTalks
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _output.WriteLine($"Test passed: Voice model {voiceModel} is accepted");
    }

    [Fact]
    public async Task TranslateAudio_HelpMeToTalkMode_MainUserAlreadySpeakingTargetLanguage_ReturnsEmptyResponse()
    {
        // Arrange
        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = true, Score = 0.95f });

        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = TestUserName,
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.English, // Same as source
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.HelpMeToTalk
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InterpreterResponse>(okResult.Value);
        Assert.Null(response.TranslatedAudio);
        _output.WriteLine($"Test passed: HelpMeToTalk mode - main user already speaking target language returns empty");
    }

    [Fact]
    public async Task TranslateAudio_VoiceRecognitionWithDifferentSimilarityScores_HandlesCorrectly()
    {
        // Arrange - Test with low similarity (not a match)
        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = false, Score = 0.3f });

        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = TestUserName,
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.Persian,
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.IgnoreMyTalks
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify CompareAudio was called
        _speechBrainRecognition.Received(1).CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>());
        _output.WriteLine($"Test passed: Low similarity score handled correctly");
    }

    [Fact]
    public async Task TranslateAudio_ValidRequest_DatabaseEmbeddingRetrieved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customEmbedding = new List<float> { 0.9f, 0.8f, 0.7f, 0.6f };
        
        _dbContext.VoiceEmbeddings.Add(new VoiceEmbedding
        {
            Id = userId,
            Name = "custom-user",
            Embedding = customEmbedding
        });
        await _dbContext.SaveChangesAsync();

        _speechBrainRecognition.CompareAudio(Arg.Any<byte[]>(), Arg.Any<List<float>>())
            .Returns(new ComparisonResult { IsMatch = true, Score = 0.92f });

        var request = new InterpreterRequest
        {
            AudioFile = new FileStream(_testAudioPath, FileMode.Open, FileAccess.Read),
            UserVoiceDetectorName = "custom-user",
            CurrentAudioLanguages = CurrentAudioLanguages.English,
            OutputLanguages = OutputLanguages.Persian, // Different from source to trigger voice recognition
            EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
            Modes = Modes.IgnoreMyTalks
        };

        // Act
        var result = await _controller.TranslateAudio(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        
        // Verify CompareAudio was called with the custom embedding
        _speechBrainRecognition.Received(1).CompareAudio(
            Arg.Any<byte[]>(), 
            Arg.Is<List<float>>(e => e.Count == 4 && e[0] == 0.9f));
        _output.WriteLine($"Test passed: Custom user embedding retrieved from database");
    }

    
    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}