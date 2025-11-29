namespace interpreter.Api.Services;

/// <summary>
/// Service for managing voice files (WAV format only)
/// </summary>
public interface IVoiceFileService
{
    /// <summary>
    /// Saves a voice file after decoding from OPUS format
    /// </summary>
    /// <param name="name">Name of the voice file (without extension)</param>
    /// <param name="opusStream">OPUS encoded audio stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Full path of the saved file</returns>
    Task<string> SaveVoiceFileAsync(string name, Stream opusStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all voice file names (without extension)
    /// </summary>
    /// <returns>List of voice file names</returns>
    Task<List<string>> GetVoiceFileNamesAsync();

    /// <summary>
    /// Gets the stream of a specific voice file
    /// </summary>
    /// <param name="name">Name of the voice file (without extension)</param>
    /// <returns>WAV file stream</returns>
    Task<Stream?> GetVoiceFileStreamAsync(string name);

    /// <summary>
    /// Deletes a voice file by name
    /// </summary>
    /// <param name="name">Name of the voice file (without extension)</param>
    /// <returns>True if deleted successfully, false if file not found</returns>
    Task<bool> DeleteVoiceFileAsync(string name);

    /// <summary>
    /// Checks if a voice file exists
    /// </summary>
    /// <param name="name">Name of the voice file (without extension)</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> VoiceFileExistsAsync(string name);
}

