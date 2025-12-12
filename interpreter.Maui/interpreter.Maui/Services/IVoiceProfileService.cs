using interpreter.Maui.DTOs;
using Models.Shared.Requests;
using Models.Shared.Responses;

namespace interpreter.Maui.Services;

public interface IVoiceProfileService
{
    /// <summary>
    /// Creates a new voice profile with the provided voice data.
    /// </summary>
    /// <param name="request">The voice profile creation request containing name and audio data.</param>
    /// <returns>A response containing the created voice profile ID and name.</returns>
    Task<CreateVoiceDetectorResponse> CreateAsync(CreateVoiceProfileDto createVoiceProfile);

    /// <summary>
    /// Retrieves a list of all voice profiles.
    /// </summary>
    /// <returns>A list of voice profile responses with ID and name.</returns>
    Task<List<VoiceEmbeddingResponse>> GetListAsync();

    /// <summary>
    /// Deletes a voice profile by ID.
    /// </summary>
    /// <param name="id">The ID of the voice profile to delete.</param>
    /// <returns>A message indicating successful deletion.</returns>
    Task DeleteAsync(Guid id);
}
