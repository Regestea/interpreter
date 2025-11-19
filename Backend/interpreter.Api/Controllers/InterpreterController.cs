using Microsoft.AspNetCore.Mvc;
using interpreter.Api.Services;
using Opus.Services;

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

    public InterpreterController(
        ILogger<InterpreterController> logger,
        IWhisperService whisperService,
        IOpusCodecService opusCodecService,
        IPiperService piperService,
        ITranslationService translationService)
    {
        _logger = logger;
        _whisperService = whisperService;
        _opusCodecService = opusCodecService;
        _piperService = piperService;
        _translationService = translationService;
    }

    /// <summary>
    /// Upload an Opus-encoded audio file for interpretation
    /// </summary>
    /// <param name="file">The Opus-encoded audio file to upload</param>
    /// <param name="targetLanguage">The target language code for translation (e.g., "en", "fa", "fr")</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request</param>
    /// <returns>Result of the file processing with Opus-encoded translated audio</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TranslateAudio(IFormFile file, [FromForm] string targetLanguage = "en", CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "File is empty" });
        }

        // Validate that the file is an audio file
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (string.IsNullOrEmpty(fileExtension))
        {
            return BadRequest(new { error = "Invalid file extension" });
        }

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            return BadRequest(new { error = "Target language is required" });
        }

        try
        {
            _logger.LogInformation("Starting audio translation pipeline for file: {FileName}, Size: {FileSize} bytes, Target Language: {TargetLanguage}", 
                file.FileName, file.Length, targetLanguage);

            // Step 1: Decode the Opus stream to WAV format
            _logger.LogInformation("Step 1: Decoding Opus audio to WAV");
            await using var opusStream = file.OpenReadStream();
            await using var decodedWavStream = await _opusCodecService.DecodeAsync(opusStream, cancellationToken);
            
            // Step 2: Process the decoded WAV stream with Whisper (transcription)
            _logger.LogInformation("Step 2: Transcribing audio with Whisper");
            var transcription = await _whisperService.TranscribeStreamAsync(decodedWavStream, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(transcription))
            {
                _logger.LogWarning("Transcription returned empty result");
                return BadRequest(new { error = "No speech detected in audio" });
            }
            
            _logger.LogInformation("Transcription result: {Transcription}", transcription);

            // Step 3: Translate the transcribed text
            _logger.LogInformation("Step 3: Translating text to {TargetLanguage}", targetLanguage);
            var translationResult = await _translationService.TranslateAsync(transcription, targetLanguage, cancellationToken);
            
            _logger.LogInformation("Translation result: {TranslatedText} (from {DetectedLanguage})", 
                translationResult.TranslatedText, translationResult.DetectedLanguage);

            // Step 4: Convert translated text to speech using Piper
            _logger.LogInformation("Step 4: Converting translated text to speech with Piper");
            var piperAudioBytes = await _piperService.TextToSpeechAsync(
                translationResult.TranslatedText, 
                PiperSharp.Models.AudioOutputType.Wav, 
                cancellationToken);
            
            _logger.LogInformation("Piper generated audio: {AudioSize} bytes", piperAudioBytes.Length);

            // Step 5: Encode the Piper WAV output to Opus format
            _logger.LogInformation("Step 5: Encoding audio to Opus format");
            await using var piperWavStream = new MemoryStream(piperAudioBytes);
            await using var encodedOpusStream = await _opusCodecService.EncodeAsync(piperWavStream, cancellationToken);
            
            // Read the encoded stream to a byte array for response
            encodedOpusStream.Position = 0;
            var encodedOpusBytes = new byte[encodedOpusStream.Length];
            var totalBytesRead = 0;
            while (totalBytesRead < encodedOpusBytes.Length)
            {
                var bytesRead = await encodedOpusStream.ReadAsync(
                    encodedOpusBytes.AsMemory(totalBytesRead, encodedOpusBytes.Length - totalBytesRead), 
                    cancellationToken);
                if (bytesRead == 0) break;
                totalBytesRead += bytesRead;
            }
            
            _logger.LogInformation("Audio translation pipeline completed successfully. Output size: {OutputSize} bytes", encodedOpusBytes.Length);

            // Step 6: Return the result with Opus-encoded audio
            return Ok(new
            {
                message = "Audio translated successfully",
                fileName = file.FileName,
                inputFileSize = file.Length,
                outputFileSize = encodedOpusBytes.Length,
                transcription,
                detectedLanguage = translationResult.DetectedLanguage,
                targetLanguage,
                translatedText = translationResult.TranslatedText,
                audioData = Convert.ToBase64String(encodedOpusBytes)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while processing the file", details = ex.Message });
        }
    }

}