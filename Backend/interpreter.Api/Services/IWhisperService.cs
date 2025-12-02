namespace interpreter.Api.Services;

/// <summary>
/// Interface for audio transcription service using Whisper.
/// </summary>
public interface IWhisperService
{
    /// <summary>
    /// Transcribes an audio stream.
    /// </summary>
    /// <param name="audioStream">The audio stream to transcribe.</param>
    /// <param name="language">The language code for transcription.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The full transcription text.</returns>
    Task<string> TranscribeStreamAsync(Stream audioStream, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the language from an audio stream.
    /// </summary>
    /// <param name="audioStream">The audio stream to analyze.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The detected or configured language code.</returns>
    Task<string> GetLanguageAsync(Stream audioStream, CancellationToken cancellationToken = default);
}


