using Microsoft.AspNetCore.Mvc;

namespace interpreter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterpreterController : ControllerBase
{
    private readonly ILogger<InterpreterController> _logger;

    public InterpreterController(ILogger<InterpreterController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Upload a file for interpretation
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <returns>Result of the file processing</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided or file is empty" });
        }

        try
        {
            _logger.LogInformation("Receiving file: {FileName}, Size: {FileSize} bytes", 
                file.FileName, file.Length);

            // Process the file stream
            using (var stream = file.OpenReadStream())
            {
                // Example: Read the stream content
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    
                    // TODO: Add your file processing logic here
                    _logger.LogInformation("File processed successfully: {FileName}", file.FileName);
                }
            }

            return Ok(new
            {
                message = "File uploaded successfully",
                fileName = file.FileName,
                fileSize = file.Length,
                contentType = file.ContentType
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
    /// Upload a file stream directly
    /// </summary>
    /// <returns>Result of the stream processing</returns>
    [HttpPost("upload-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadStream()
    {
        try
        {
            if (!Request.HasFormContentType && Request.ContentLength > 0)
            {
                // Handle raw stream upload
                using (var memoryStream = new MemoryStream())
                {
                    await Request.Body.CopyToAsync(memoryStream);
                    var streamBytes = memoryStream.ToArray();
                    
                    _logger.LogInformation("Stream received, Size: {Size} bytes", streamBytes.Length);
                    
                    // TODO: Add your stream processing logic here
                    
                    return Ok(new
                    {
                        message = "Stream processed successfully",
                        size = streamBytes.Length
                    });
                }
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
}