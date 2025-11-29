using Microsoft.AspNetCore.Mvc;
using interpreter.Api.Services;

namespace interpreter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoiceDetectorController : ControllerBase
{
    private readonly IVoiceFileService _voiceFileService;
    private readonly ILogger<VoiceDetectorController> _logger;

    public VoiceDetectorController(
        IVoiceFileService voiceFileService,
        ILogger<VoiceDetectorController> logger)
    {
        _voiceFileService = voiceFileService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string name, [FromBody] IFormFile file)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name is required");

            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            // Check if voice file already exists
            if (await _voiceFileService.VoiceFileExistsAsync(name))
                return Conflict($"Voice file with name '{name}' already exists");

            // Decode OPUS and save as WAV
            await using var opusStream = file.OpenReadStream();
            var filePath = await _voiceFileService.SaveVoiceFileAsync(name, opusStream);

            _logger.LogInformation("Voice file created: {Name} at {FilePath}", name, filePath);
            return Ok(new { name, filePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating voice file: {Name}", name);
            return StatusCode(500, "An error occurred while creating the voice file");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        try
        {
            var voiceFiles = await _voiceFileService.GetVoiceFileNamesAsync();
            return Ok(voiceFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voice file list");
            return StatusCode(500, "An error occurred while retrieving the voice file list");
        }
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name is required");

            var deleted = await _voiceFileService.DeleteVoiceFileAsync(name);
            
            if (!deleted)
                return NotFound($"Voice file with name '{name}' not found");

            _logger.LogInformation("Voice file deleted: {Name}", name);
            return Ok(new { message = $"Voice file '{name}' deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting voice file: {Name}", name);
            return StatusCode(500, "An error occurred while deleting the voice file");
        }
    }

    [HttpPost("set-voice-cache")]
    public IActionResult SetVoiceInCache()
    {
        // TODO: Implement set voice in cache logic
        return Ok();
    }
}