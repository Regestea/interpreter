using System.Diagnostics;
using interpreter.Api.Data;
using Microsoft.AspNetCore.Mvc;
using interpreter.Api.Services;
using Microsoft.EntityFrameworkCore;
using Models.Shared.Enums;
using Models.Shared.Extensions;
using Models.Shared.Requests;
using Models.Shared.Responses;
using Opus.Services;
using SpeechBrain;

namespace interpreter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterpreterController : ControllerBase
{
    private readonly ILogger<InterpreterController> _logger;
    private readonly IWhisperService _whisperService;
    private readonly IOpusCodecService _opusCodecService;
    private readonly IPiperService _piperService;
    private readonly ITranslationService _translationService;
    private readonly ISpeechBrainRecognition _speechBrainRecognition;
    private readonly InterpreterDbContext _dbContext;

    public InterpreterController(
        ILogger<InterpreterController> logger,
        IWhisperService whisperService,
        IOpusCodecService opusCodecService,
        IPiperService piperService,
        ITranslationService translationService,
        ISpeechBrainRecognition speechBrainRecognition,
        InterpreterDbContext dbContext)
    {
        _logger = logger;
        _whisperService = whisperService;
        _opusCodecService = opusCodecService;
        _piperService = piperService;
        _translationService = translationService;
        _speechBrainRecognition = speechBrainRecognition;
        _dbContext = dbContext;
    }


    /// <summary>
    /// Upload an Opus-encoded audio file for interpretation
    /// </summary>
    /// <param name="request">The interpreter request containing audio file and configuration</param>
    /// <returns>Result of the file processing with Opus-encoded translated audio</returns>
    [HttpPost("UploadEncodeAudio")]
    [ProducesResponseType(typeof(InterpreterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TranslateAudio([FromBody] InterpreterRequest request)
    {
        long bytes = request.GetAudioBytes().Length;

        double sizeInKB = bytes / 1024.0;
        double sizeInMB = bytes / (1024.0 * 1024.0);

        Console.WriteLine($"Size: {sizeInKB:F2} KB");
        Console.WriteLine($"Size: {sizeInMB:F2} MB");
        
        
        var st = new Stopwatch();
        st.Start();
        try
        {

            if (string.IsNullOrWhiteSpace(request.UserVoiceDetectorName))
            {
                _logger.LogWarning("User voice detector name is missing");
                return BadRequest(new { error = "User voice detector name is required" });
            }

            var streamAudio = new MemoryStream(request.GetAudioBytes());
            
            // Decode the Opus audio to PCM
            _logger.LogInformation("Decoding audio file");
            var decodedAudio = await _opusCodecService.DecodeAsync(streamAudio);

            // Determine the source language
            string sourceLanguage;
            if (request.CurrentAudioLanguages == CurrentAudioLanguages.AutoDetect)
            {
                _logger.LogInformation("Auto-detecting language from audio");
                sourceLanguage = await _whisperService.GetLanguageAsync(decodedAudio);
                _logger.LogInformation("Auto-detected language: {Language}", sourceLanguage);
            }
            else
            {
                sourceLanguage = request.CurrentAudioLanguages.ToValue();
                _logger.LogInformation("Using specified language: {Language}", sourceLanguage);
            }

            var targetLanguage = request.OutputLanguages.ToValue();

            // Check if source and target languages are the same
            if (sourceLanguage == targetLanguage)
            {
                _logger.LogInformation("Source and target languages are the same ({Language}), no translation needed", sourceLanguage);
                return Ok(new InterpreterResponse
                {
                    AudioInputLanguage = sourceLanguage
                });
            }

            // Perform voice recognition
            // _logger.LogInformation("Performing voice recognition for user: {UserName}", request.UserVoiceDetectorName);
            // var mainEmbedding = await _dbContext.VoiceEmbeddings
            //     .FirstOrDefaultAsync(x => x.Name == request.UserVoiceDetectorName);

            // if (mainEmbedding == null)
            // {
            //     _logger.LogWarning("Voice embedding not found for user: {UserName}", request.UserVoiceDetectorName);
            //     return BadRequest(new { error = $"Voice embedding '{request.UserVoiceDetectorName}' not found" });
            // }

            bool isMainUser=false;
            // using (var ms = new MemoryStream())
            // {
            //     decodedAudio.Position = 0; // Reset stream position
            //     await decodedAudio.CopyToAsync(ms);
            //     isMainUser = _speechBrainRecognition.CompareAudio(ms.ToArray(), mainEmbedding.Embedding).IsMatch;
            // }
            
            _logger.LogInformation("Voice recognition result - IsMainUser: {IsMainUser}", isMainUser);

            // Handle "IgnoreMyTalks" mode - skip processing if main user is talking
            if (request.Modes == Modes.IgnoreMyTalks && isMainUser)
            {
                _logger.LogInformation("Ignoring main user's talk as per IgnoreMyTalks mode");
                return Ok(new InterpreterResponse());
            }

            // Handle "HelpMeToTalk" mode - only translate if main user is NOT speaking target language
            if (request.Modes == Modes.HelpMeToTalk)
            {
                if (isMainUser && sourceLanguage == targetLanguage)
                {
                    _logger.LogInformation("Main user already speaking in target language ({Language}) in HelpMeToTalk mode", targetLanguage);
                    return Ok(new InterpreterResponse());
                }
                
                // Skip if not the main user in HelpMeToTalk mode
                if (!isMainUser)
                {
                    _logger.LogInformation("Skipping non-main user audio in HelpMeToTalk mode");
                    return Ok(new InterpreterResponse());
                }
            }

            // Transcribe the audio
            _logger.LogInformation("Transcribing audio to text");
            decodedAudio.Position = 0; // Reset stream position
            var audioText = await _whisperService.TranscribeStreamAsync(decodedAudio, sourceLanguage);
            _logger.LogInformation("Transcribed text: {Text}", audioText);

            if (string.IsNullOrWhiteSpace(audioText))
            {
                _logger.LogWarning("No text could be transcribed from audio");
                return Ok(new InterpreterResponse());
            }

            // Translate the text
            _logger.LogInformation("Translating text from {SourceLang} to {TargetLang}", sourceLanguage, targetLanguage);
            var translatedText = await _translationService.TranslateAsync(audioText, targetLanguage);
            _logger.LogInformation("Translated text: {Text}", translatedText.TranslatedText);

            if (string.IsNullOrWhiteSpace(translatedText.TranslatedText))
            {
                _logger.LogWarning("Translation resulted in empty text");
                return Ok(new InterpreterResponse());
            }

            // Generate speech from translated text
            _logger.LogInformation("Generating speech from translated text");
            byte[] audioBytes;
            if (request.OutputLanguages == OutputLanguages.English)
            {
                var piperVoiceModel = request.EnglishVoiceModels.ToValue();
                _logger.LogInformation("Using English voice model: {Model}", piperVoiceModel);
                _piperService.SetModel(piperVoiceModel);
                audioBytes = await _piperService.TextToSpeechAsync(translatedText.TranslatedText);
            }
            else if (request.OutputLanguages == OutputLanguages.Persian)
            {
                _logger.LogInformation("Using Persian voice model: fa_IR-gyro-medium");
                _piperService.SetModel("fa_IR-gyro-medium");
                audioBytes = await _piperService.TextToSpeechAsync(translatedText.TranslatedText);
            }
            else
            {
                _logger.LogWarning("Unsupported output language: {Language}", request.OutputLanguages);
                return BadRequest(new { error = $"Unsupported output language: {request.OutputLanguages}" });
            }

            // Encode the audio to Opus
            _logger.LogInformation("Encoding audio to Opus format");
            byte[] encodedAudioBytes;
            using (var audioMemory = new MemoryStream(audioBytes))
            {
                var encodedAudioStream = await _opusCodecService.EncodeAsync(audioMemory);
                using (var outputMemory = new MemoryStream())
                {
                    await encodedAudioStream.CopyToAsync(outputMemory);
                    encodedAudioBytes = outputMemory.ToArray();
                }
            }

            _logger.LogInformation("Translation completed successfully");
            
            // Return the complete response with all translation data
            var response = new InterpreterResponse
            {
                TranslatedAudio = encodedAudioBytes,
                OriginalText = audioText,
                TranslatedText = translatedText.TranslatedText,
                AudioInputLanguage = sourceLanguage
            };
            st.Stop();
            _logger.LogError("it takes aobut :"+st.ElapsedMilliseconds);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio translation");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while processing the audio", details = ex.Message });
        }
    }

    /// <summary>
    /// Test action endpoint
    /// </summary>
    /// <returns>Ok result</returns>
    [HttpGet("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TestAction()
    {
        return Ok(new { message = "Test action successful" });
    }
}
