using interpreter.Maui.DTOs;
using Models.Shared.Requests;
using Models.Shared.Responses;
using System.Text.Json;
using Opus.Services;

namespace interpreter.Maui.Services;

public class VoiceProfileService : IVoiceProfileService
{
    private readonly IApiClient _apiClient;
    private readonly IOpusCodecService _opusCodecService;

    public VoiceProfileService(IApiClient apiClient)
    {
        _opusCodecService = new OpusCodecService();
        _apiClient = apiClient;
    }

    /// <summary>
    /// Creates a new voice profile with the provided voice data.
    /// </summary>
    public async Task<CreateVoiceDetectorResponse> CreateAsync(CreateVoiceProfileDto createVoiceProfile)
    {
        if (createVoiceProfile == null)
            throw new ArgumentNullException(nameof(createVoiceProfile));

        if (string.IsNullOrWhiteSpace(createVoiceProfile.Name))
            throw new ArgumentException("Profile name is required.", nameof(createVoiceProfile.Name));

        if (createVoiceProfile.Voice == null || createVoiceProfile.Voice.Length == 0)
            throw new ArgumentException("Voice data is required.", nameof(createVoiceProfile.Voice));

        try
        {
            // Create multipart form data request
            var encodedAudio= await _opusCodecService.EncodeAsync(createVoiceProfile.Voice);
            var memoryStream=new MemoryStream();
            await encodedAudio.CopyToAsync(memoryStream);

            var request = new CreateVoiceDetectorRequest()
            {
                Name =  createVoiceProfile.Name,
                Voice =  memoryStream.ToArray()
            };
            var result=await _apiClient.SendAsync("api/VoiceDetector", HttpMethod.Post,request,false);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var response = JsonSerializer.Deserialize<CreateVoiceDetectorResponse>(result.Content, jsonOptions);
            return  response;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to create voice profile.", ex);
        }
    }

    /// <summary>
    /// Retrieves a list of all voice profiles.
    /// </summary>
    public async Task<List<VoiceEmbeddingResponse>> GetListAsync()
    {
        try
        {
            var result = await _apiClient.SendAsync("api/VoiceDetector", HttpMethod.Get, null, false);
        
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var profiles = JsonSerializer.Deserialize<List<VoiceEmbeddingResponse>>(result.Content, jsonOptions);
            return profiles ?? new List<VoiceEmbeddingResponse>();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to retrieve voice profiles.", ex);
        }
    }

    /// <summary>
    /// Deletes a voice profile by ID.
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Profile ID cannot be empty.", nameof(id));

        try
        {
            await _apiClient.SendAsync($"api/VoiceDetector/{id}", HttpMethod.Delete, null, false);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to delete voice profile with ID {id}.", ex);
        }
    }

}
