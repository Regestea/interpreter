using System.Diagnostics;
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
    /// Upload a WAV audio file for interpretation
    /// </summary>
    /// <param name="file">The WAV audio file to upload</param>
    /// <param name="targetLanguage">The target language code for translation (e.g., "en", "fa", "fr")</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request</param>
    /// <returns>Result of the file processing with WAV translated audio</returns>
    [HttpPost("UploadWavAudio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TranslateWavAudio(IFormFile file, [FromForm] string targetLanguage = "en", CancellationToken cancellationToken = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== WAV Audio Translation Request Started ===");
        _logger.LogInformation("Request received - File: {FileName}, Size: {FileSize} bytes, Target Language: {TargetLanguage}", 
            file.FileName, file.Length, targetLanguage);

        if (file.Length == 0)
        {
            _logger.LogWarning("Request validation failed: File is empty");
            return BadRequest(new { error = "File is empty" });
        }

        // Validate that the file is a WAV file
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (string.IsNullOrEmpty(fileExtension) || fileExtension != ".wav")
        {
            _logger.LogWarning("Request validation failed: Invalid file extension '{Extension}'", fileExtension);
            return BadRequest(new { error = "Invalid file extension. Only WAV files are supported." });
        }

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            _logger.LogWarning("Request validation failed: Target language is required");
            return BadRequest(new { error = "Target language is required" });
        }

        _logger.LogInformation("Request validation passed");

        try
        {
            _logger.LogInformation("Starting WAV audio translation pipeline");

            // Step 1: Process the WAV stream with Whisper (transcription)
            _logger.LogInformation("Step 1: Starting audio transcription with Whisper");
            var step1Stopwatch = Stopwatch.StartNew();
            
            await using var wavStream = file.OpenReadStream();
            _logger.LogInformation("Audio stream opened, starting transcription...");
            var transcription = await _whisperService.TranscribeStreamAsync(wavStream, cancellationToken);
            
            step1Stopwatch.Stop();
            _logger.LogInformation("Step 1 completed in {ElapsedMs}ms ({ElapsedSeconds}s)", 
                step1Stopwatch.ElapsedMilliseconds, step1Stopwatch.Elapsed.TotalSeconds);
            
            if (string.IsNullOrWhiteSpace(transcription))
            {
                _logger.LogWarning("Transcription returned empty result after {ElapsedMs}ms", step1Stopwatch.ElapsedMilliseconds);
                return BadRequest(new { error = "No speech detected in audio" });
            }
            
            _logger.LogInformation("Transcription successful - Length: {Length} characters, Text: {Transcription}", 
                transcription.Length, transcription);

            // Step 2: Translate the transcribed text
            _logger.LogInformation("Step 2: Starting text translation to {TargetLanguage}", targetLanguage);
            var step2Stopwatch = Stopwatch.StartNew();
            
            var translationResult = await _translationService.TranslateAsync(transcription, targetLanguage, cancellationToken);
            
            step2Stopwatch.Stop();
            _logger.LogInformation("Step 2 completed in {ElapsedMs}ms ({ElapsedSeconds}s)", 
                step2Stopwatch.ElapsedMilliseconds, step2Stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogInformation("Translation successful - Detected Language: {DetectedLanguage}, Translated Text: {TranslatedText}", 
                translationResult.DetectedLanguage, translationResult.TranslatedText);

            // Step 3: Convert translated text to speech using Piper
            _logger.LogInformation("Step 3: Starting text-to-speech conversion with Piper");
            var step3Stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Converting text to speech: '{Text}'", translationResult.TranslatedText);
            var piperAudioBytes = await _piperService.TextToSpeechAsync(
                translationResult.TranslatedText, 
                PiperSharp.Models.AudioOutputType.Wav, 
                cancellationToken);
            
            step3Stopwatch.Stop();
            _logger.LogInformation("Step 3 completed in {ElapsedMs}ms ({ElapsedSeconds}s)", 
                step3Stopwatch.ElapsedMilliseconds, step3Stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogInformation("Piper audio generated successfully - Size: {AudioSize} bytes", piperAudioBytes.Length);

            overallStopwatch.Stop();
            _logger.LogInformation("=== WAV Audio Translation Pipeline Completed Successfully ===");
            _logger.LogInformation("Total execution time: {TotalMs}ms ({TotalSeconds}s)", 
                overallStopwatch.ElapsedMilliseconds, overallStopwatch.Elapsed.TotalSeconds);
            _logger.LogInformation("Timing breakdown - Step 1 (Transcription): {Step1Ms}ms, Step 2 (Translation): {Step2Ms}ms, Step 3 (TTS): {Step3Ms}ms",
                step1Stopwatch.ElapsedMilliseconds, step2Stopwatch.ElapsedMilliseconds, step3Stopwatch.ElapsedMilliseconds);

            // Step 4: Return the result with WAV audio
            _logger.LogInformation("Preparing response - Output size: {OutputSize} bytes", piperAudioBytes.Length);
            return Ok(new
            {
                message = "Audio translated successfully",
                fileName = file.FileName,
                inputFileSize = file.Length,
                outputFileSize = piperAudioBytes.Length,
                transcription,
                detectedLanguage = translationResult.DetectedLanguage,
                targetLanguage,
                translatedText = translationResult.TranslatedText,
                audioData = Convert.ToBase64String(piperAudioBytes),
                timingMs = new
                {
                    total = overallStopwatch.ElapsedMilliseconds,
                    transcription = step1Stopwatch.ElapsedMilliseconds,
                    translation = step2Stopwatch.ElapsedMilliseconds,
                    textToSpeech = step3Stopwatch.ElapsedMilliseconds
                }
            });
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex, "Error processing file: {FileName} after {ElapsedMs}ms", file.FileName, overallStopwatch.ElapsedMilliseconds);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while processing the file", details = ex.Message });
        }
    }

    /// <summary>
    /// Upload an Opus-encoded audio file for interpretation
    /// </summary>
    /// <param name="file">The Opus-encoded audio file to upload</param>
    /// <param name="targetLanguage">The target language code for translation (e.g., "en", "fa", "fr")</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request</param>
    /// <returns>Result of the file processing with Opus-encoded translated audio</returns>
    [HttpPost("UploadEncodeAudio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TranslateAudio(IFormFile file, [FromForm] string targetLanguage = "en", CancellationToken cancellationToken = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Opus Audio Translation Request Started ===");
        _logger.LogInformation("Request received - File: {FileName}, Size: {FileSize} bytes, Target Language: {TargetLanguage}", 
            file.FileName, file.Length, targetLanguage);

        if (file.Length == 0)
        {
            _logger.LogWarning("Request validation failed: File is empty");
            return BadRequest(new { error = "File is empty" });
        }

        // Validate that the file is an audio file
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (string.IsNullOrEmpty(fileExtension))
        {
            _logger.LogWarning("Request validation failed: Invalid file extension");
            return BadRequest(new { error = "Invalid file extension" });
        }

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            _logger.LogWarning("Request validation failed: Target language is required");
            return BadRequest(new { error = "Target language is required" });
        }

        _logger.LogInformation("Request validation passed - File extension: {Extension}", fileExtension);

        try
        {
            _logger.LogInformation("Starting Opus audio translation pipeline");

            // Step 1: Decode the Opus stream to WAV format
            _logger.LogInformation("Step 1: Starting Opus to WAV decoding");
            var step1Stopwatch = Stopwatch.StartNew();
            
            await using var opusStream = file.OpenReadStream();
            _logger.LogInformation("Opus stream opened, starting decoding...");
            await using var decodedWavStream = await _opusCodecService.DecodeAsync(opusStream, cancellationToken);
            
            step1Stopwatch.Stop();
            _logger.LogInformation("Step 1 completed in {ElapsedMs}ms ({ElapsedSeconds}s)", 
                step1Stopwatch.ElapsedMilliseconds, step1Stopwatch.Elapsed.TotalSeconds);
            _logger.LogInformation("Decoding successful - WAV stream size: {StreamSize} bytes", decodedWavStream.Length);
            
            // Step 2: Process the decoded WAV stream with Whisper (transcription)
            _logger.LogInformation("Step 2: Starting audio transcription with Whisper");
            var step2Stopwatch = Stopwatch.StartNew();
            
            var transcription = await _whisperService.TranscribeStreamAsync(decodedWavStream, cancellationToken);
            
            step2Stopwatch.Stop();
            _logger.LogInformation("Step 2 completed in {ElapsedMs}ms ({ElapsedSeconds}s)", 
                step2Stopwatch.ElapsedMilliseconds, step2Stopwatch.Elapsed.TotalSeconds);
            
            if (string.IsNullOrWhiteSpace(transcription))
            {
                _logger.LogWarning("Transcription returned empty result after {ElapsedMs}ms", step2Stopwatch.ElapsedMilliseconds);
                return BadRequest(new { error = "No speech detected in audio" });
            }
            
            _logger.LogInformation("Transcription successful - Length: {Length} characters, Text: {Transcription}", 
                transcription.Length, transcription);

            // Step 3: Translate the transcribed text
            _logger.LogInformation("Step 3: Starting text translation to {TargetLanguage}", targetLanguage);
            var step3Stopwatch = Stopwatch.StartNew();
            
            var translationResult = await _translationService.TranslateAsync(transcription, targetLanguage, cancellationToken);
            
            step3Stopwatch.Stop();
            _logger.LogInformation("Step 3 completed in {ElapsedMs}ms ({ElapsedSeconds}s)", 
                step3Stopwatch.ElapsedMilliseconds, step3Stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogInformation("Translation successful - Detected Language: {DetectedLanguage}, Translated Text: {TranslatedText}", 
                translationResult.DetectedLanguage, translationResult.TranslatedText);

            // Step 4: Convert translated text to speech using Piper
            _logger.LogInformation("Step 4: Starting text-to-speech conversion with Piper");
            var step4Stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Converting text to speech: '{Text}'", translationResult.TranslatedText);
            var piperAudioBytes = await _piperService.TextToSpeechAsync(
                translationResult.TranslatedText, 
                PiperSharp.Models.AudioOutputType.Wav, 
                cancellationToken);
            
            step4Stopwatch.Stop();
            _logger.LogInformation("Step 4 completed in {ElapsedMs}ms ({ElapsedSeconds}s)", 
                step4Stopwatch.ElapsedMilliseconds, step4Stopwatch.Elapsed.TotalSeconds);
            
            _logger.LogInformation("Piper audio generated successfully - Size: {AudioSize} bytes", piperAudioBytes.Length);

            // Step 5: Encode the Piper WAV output to Opus format
            _logger.LogInformation("Step 5: Starting WAV to Opus encoding");
            var step5Stopwatch = Stopwatch.StartNew();
            
            await using var piperWavStream = new MemoryStream(piperAudioBytes);
            _logger.LogInformation("WAV stream created, starting Opus encoding...");
            await using var encodedOpusStream = await _opusCodecService.EncodeAsync(piperWavStream, cancellationToken);
            
            step5Stopwatch.Stop();
            _logger.LogInformation("Step 5 completed in {ElapsedMs}ms ({ElapsedSeconds}s)", 
                step5Stopwatch.ElapsedMilliseconds, step5Stopwatch.Elapsed.TotalSeconds);
            _logger.LogInformation("Encoding successful - Opus stream size: {StreamSize} bytes", encodedOpusStream.Length);
            
            // Read the encoded stream to a byte array for response
            _logger.LogInformation("Reading encoded Opus stream to byte array...");
            var readStopwatch = Stopwatch.StartNew();
            
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
            
            readStopwatch.Stop();
            _logger.LogInformation("Stream read completed in {ElapsedMs}ms - Total bytes read: {BytesRead}", 
                readStopwatch.ElapsedMilliseconds, totalBytesRead);
            
            overallStopwatch.Stop();
            _logger.LogInformation("=== Opus Audio Translation Pipeline Completed Successfully ===");
            _logger.LogInformation("Total execution time: {TotalMs}ms ({TotalSeconds}s)", 
                overallStopwatch.ElapsedMilliseconds, overallStopwatch.Elapsed.TotalSeconds);
            _logger.LogInformation("Timing breakdown - Step 1 (Decode): {Step1Ms}ms, Step 2 (Transcription): {Step2Ms}ms, Step 3 (Translation): {Step3Ms}ms, Step 4 (TTS): {Step4Ms}ms, Step 5 (Encode): {Step5Ms}ms",
                step1Stopwatch.ElapsedMilliseconds, step2Stopwatch.ElapsedMilliseconds, step3Stopwatch.ElapsedMilliseconds, 
                step4Stopwatch.ElapsedMilliseconds, step5Stopwatch.ElapsedMilliseconds);

            // Step 6: Return the result with Opus-encoded audio
            _logger.LogInformation("Preparing response - Output size: {OutputSize} bytes", encodedOpusBytes.Length);
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
                audioData = Convert.ToBase64String(encodedOpusBytes),
                timingMs = new
                {
                    total = overallStopwatch.ElapsedMilliseconds,
                    opusDecode = step1Stopwatch.ElapsedMilliseconds,
                    transcription = step2Stopwatch.ElapsedMilliseconds,
                    translation = step3Stopwatch.ElapsedMilliseconds,
                    textToSpeech = step4Stopwatch.ElapsedMilliseconds,
                    opusEncode = step5Stopwatch.ElapsedMilliseconds
                }
            });
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex, "Error processing file: {FileName} after {ElapsedMs}ms", file.FileName, overallStopwatch.ElapsedMilliseconds);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while processing the file", details = ex.Message });
        }
    }

}