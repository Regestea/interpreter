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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The full transcription text.</returns>
    Task<string> TranscribeStreamAsync(Stream audioStream, CancellationToken cancellationToken = default);
}


