using System.ComponentModel.DataAnnotations;

namespace Models.Shared.Requests;

public class CreateVoiceDetectorRequest
{
    [Required]
    public string Name { get; set; }
    
    [Required]
    public required string Voice { get; set; }
    
    /// <summary>
    /// Gets the audio file as a byte array by decoding the base64 string
    /// </summary>
    public byte[] GetAudioBytes() => Convert.FromBase64String(Voice);
}