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

    /// <summary>
    /// Upload a file stream directly with real-time transcription
    /// </summary>
    /// <returns>Result of the stream processing</returns>
    [HttpPost("upload-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadStream(CancellationToken cancellationToken)
    {
        try
        {
            if (Request.ContentLength > 0)
            {
                _logger.LogInformation("Stream received, Size: {Size} bytes", Request.ContentLength);
                
                // Use streaming transcription for real-time results
                var transcription = await _whisperService.TranscribeStreamAsync(Request.Body, cancellationToken);
                
                return Ok(new
                {
                    message = "Stream processed successfully",
                    size = Request.ContentLength,
                    transcription = transcription
                });
            }

            return BadRequest(new { error = "No stream data provided" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stream");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while processing the stream" });
        }
    }

    /// <summary>
    /// Upload audio stream and get real-time transcription segments (Server-Sent Events)
    /// </summary>
    /// <returns>Streaming transcription segments</returns>
    [HttpPost("upload-stream-realtime")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task UploadStreamRealtime(CancellationToken cancellationToken)
    {
        try
        {
            if (Request.ContentLength > 0)
            {
                _logger.LogInformation("Starting real-time stream transcription, Size: {Size} bytes", Request.ContentLength);
                
                Response.ContentType = "text/event-stream";
                Response.Headers.Append("Cache-Control", "no-cache");
                Response.Headers.Append("Connection", "keep-alive");
                
                await foreach (var segment in _whisperService.TranscribeStreamingAsync(Request.Body, cancellationToken))
                {
                    var message = $"data: {segment}\n\n";
                    await Response.WriteAsync(message, cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                
                await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            else
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsJsonAsync(new { error = "No stream data provided" }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing real-time stream");
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Response.WriteAsJsonAsync(new { error = "An error occurred while processing the stream" }, cancellationToken);
        }
    }
}