using System.ComponentModel.DataAnnotations;
using Models.Shared.Enums;

namespace Models.Shared.Requests;

public class InterpreterRequest
{
    [Required]
    public required string AudioFile { get; set; }

    public CurrentAudioLanguages CurrentAudioLanguages { get; set; }

    [Required] 
    public string UserVoiceDetectorName { get; set; } = null!;

    public EnglishVoiceModels EnglishVoiceModels { get; set; }

    public OutputLanguages OutputLanguages { get; set; }
    
    public Modes Modes { get; set; }
    
    /// <summary>
    /// Gets the audio file as a byte array by decoding the base64 string
    /// </summary>
    public byte[] GetAudioBytes() => Convert.FromBase64String(AudioFile);
}