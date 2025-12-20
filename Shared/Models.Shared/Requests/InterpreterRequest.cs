using System.ComponentModel.DataAnnotations;
using Models.Shared.Enums;

namespace Models.Shared.Requests;

public class InterpreterRequest
{
    [Required]
    public required string AudioFile { get; set; }

    public InputAudioLanguages InputAudioLanguages { get; set; }
    
    public Guid? VoiceProfileId { get; set; } 

    public bool WithTts { get; set; } = true;

    public EnglishVoiceModels EnglishVoiceModels { get; set; }

    public OutputLanguages OutputLanguages { get; set; }
    
    public Modes Modes { get; set; }
    
    /// <summary>
    /// Gets the audio file as a byte array by decoding the base64 string
    /// </summary>
    public byte[] GetAudioBytes() => Convert.FromBase64String(AudioFile);
}