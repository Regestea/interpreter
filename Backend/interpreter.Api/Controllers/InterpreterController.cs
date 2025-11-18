using Microsoft.AspNetCore.Mvc;
using interpreter.Api.Services;

namespace interpreter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterpreterController : ControllerBase
{
    private readonly ILogger<InterpreterController> _logger;
    private readonly IWhisperService _whisperService;

    public InterpreterController(
        ILogger<InterpreterController> logger,
        IWhisperService whisperService)
    {
        _logger = logger;
        _whisperService = whisperService;
    }

    /// <summary>
    /// Upload a file for interpretation
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <returns>Result of the file processing</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationToken)
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

        try
        {
            _logger.LogInformation("Receiving file: {FileName}, Size: {FileSize} bytes", 
                file.FileName, file.Length);

            // Process the file stream with Whisper
            await using var stream = file.OpenReadStream();
            var transcription = await _whisperService.TranscribeStreamAsync(stream, cancellationToken);
            
            _logger.LogInformation("File processed successfully: {FileName}", file.FileName);

            return Ok(new
            {
                message = "File uploaded and transcribed successfully",
                fileName = file.FileName,
                fileSize = file.Length,
                contentType = file.ContentType,
                transcription = transcription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while processing the file" });
        }
    }
    
}