namespace interpreter.Api.Services;

/// <summary>
/// Interface for audio transcription service using Whisper.
/// </summary>
public interface IWhisperService
{
    /// <summary>
    /// Transcribes an audio file.
    /// </summary>
    /// <param name="audioPath">The path to the audio file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The full transcription text.</returns>
    Task<string> TranscribeAsync(string audioPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes an audio stream.
    /// </summary>
    /// <param name="audioStream">The audio stream to transcribe.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The full transcription text.</returns>
    Task<string> TranscribeStreamAsync(Stream audioStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes an audio stream and yields results as they become available.
    /// </summary>
    /// <param name="audioStream">The audio stream to transcribe.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of transcription segments.</returns>
    IAsyncEnumerable<string> TranscribeStreamingAsync(Stream audioStream, CancellationToken cancellationToken = default);
}


