namespace SpeechBrain;

/// <summary>
/// Interface for SpeechBrain speaker recognition functionality
/// </summary>
public interface ISpeechBrainRecognition : IDisposable
{
    /// <summary>
    /// Initializes the SpeechBrain model
    /// </summary>
    void Initialize();

    /// <summary>
    /// Gets the audio embedding from the provided audio file
    /// </summary>
    /// <param name="audioBytes">Audio file as byte array</param>
    /// <returns>Audio embedding as a list of floats</returns>
    List<float> GetAudioEmbedding(byte[] audioBytes);

    /// <summary>
    /// Compares an audio byte array with the provided main audio embedding
    /// </summary>
    /// <param name="audioBytes">Audio file as byte array to compare</param>
    /// <param name="mainEmbedding">Main audio embedding obtained from GetAudioEmbedding</param>
    /// <returns>Comparison result containing score and match status</returns>
    ComparisonResult CompareAudio(byte[] audioBytes, List<float> mainEmbedding);
}

