using System.Text.Json;
using IdempotentAPI.Filters;
using interpreter.Api.Data;
using interpreter.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Shared.Requests;
using Models.Shared.Responses;
using Opus.Services;
using SpeechBrain;

namespace interpreter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoiceDetectorController : ControllerBase
{
    private readonly ILogger<VoiceDetectorController> _logger;
    private readonly InterpreterDbContext _dbContext;
    private readonly ISpeechBrainRecognition _speechBrainRecognition;
    private readonly IOpusCodecService _opusCodecService;

    public VoiceDetectorController(
        ILogger<VoiceDetectorController> logger, InterpreterDbContext dbContext,
        ISpeechBrainRecognition speechBrainRecognition, IOpusCodecService opusCodecService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _speechBrainRecognition = speechBrainRecognition;
        _opusCodecService = opusCodecService;
    }

    [HttpPost]
    [Idempotent(ExpiresInMilliseconds = 90000)]
    public async Task<IActionResult> Create([FromBody] CreateVoiceDetectorRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required");

            if (request.Voice.Length == 0)
                return BadRequest("File is required");
            var audioBytes = request.GetAudioBytes();
            await using var audioStream = new MemoryStream(audioBytes);
            await using var decodedMemoryStream=new MemoryStream();
            try
            {
                var decodedVoice=await _opusCodecService.DecodeAsync(audioStream);
                
                await decodedVoice.CopyToAsync(decodedMemoryStream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var embedding = _speechBrainRecognition.GetAudioEmbedding(decodedMemoryStream.ToArray());

            if (embedding.Count == 0)
            {
                return BadRequest("Failed to generate audio embedding.");
            }
            
            var voiceEmbedding = new VoiceEmbedding()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                EmbeddingJson = JsonSerializer.Serialize(embedding)
            };

            _dbContext.VoiceEmbeddings.Add(voiceEmbedding);
            await _dbContext.SaveChangesAsync();
            await Task.Delay(20000);
            return Ok(new CreateVoiceDetectorResponse
            {
                Id = voiceEmbedding.Id,
                Name = request.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating voice file: {Name}", request.Name);
            return StatusCode(500, "An error occurred while creating the voice file");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        try
        {
            var result = await _dbContext.VoiceEmbeddings
                .Select(x => new VoiceEmbeddingResponse() { Id = x.Id, Name = x.Name }).ToListAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voice file list");
            return StatusCode(500, "An error occurred while retrieving the voice file list");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var voiceEmbedding = await _dbContext.VoiceEmbeddings.FindAsync(id);

            if (voiceEmbedding == null)
                return NotFound($"Voice embedding with id '{id}' not found");

            _dbContext.VoiceEmbeddings.Remove(voiceEmbedding);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Voice file deleted: {Id}, {Name}", id, voiceEmbedding.Name);
            return Ok(new { message = $"Voice file '{voiceEmbedding.Name}' deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting voice file: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the voice file");
        }
    }
    
}