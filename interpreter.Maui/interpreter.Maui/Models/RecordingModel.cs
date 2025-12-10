using Models.Shared.Enums;

namespace interpreter.Maui.Models;

public class RecordingModel
{
    public InputAudioLanguages InputAudioLanguages { get; set; }
    public OutputLanguages OutputLanguages { get; set; }
    public EnglishVoiceModels EnglishVoiceModels { get; set; }
    public Modes Modes { get; set; }
    public bool UseAndroidTts { get; set; } = false;
    public bool WithTts { get; set; }= true;
    public List<VoiceProfileModel> VoiceProfileModels { get; set; } = new();
    public Guid? SelectedVoiceProfileId { get; set; }
}