using PiperSharp;
using PiperSharp.Models;

namespace interpreter.Api.Services
{
    public interface IPiperService
    {
        /// <summary>
        /// Converts text to speech audio
        /// </summary>
        /// <param name="text">The text to convert to speech</param>
        /// <param name="outputType">The audio output format (Wav, Mp3, Raw)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Audio data as byte array</returns>
        Task<byte[]> TextToSpeechAsync(string text, AudioOutputType outputType = AudioOutputType.Wav, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the list of available voice models
        /// </summary>
        Dictionary<string, VoiceModel> GetAvailableModels();
        
        /// <summary>
        /// Sets the active voice model by key
        /// </summary>
        void SetModel(string modelKey);
    }
}
