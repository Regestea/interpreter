namespace interpreter.Maui.Services;

/// <summary>
/// Configuration for audio recording settings.
/// </summary>
public class AudioRecordingConfiguration
{
    public int SampleRate { get; set; } = 44100;
    public int ChannelCount { get; set; } = 1; // Mono
    public int BitsPerSample { get; set; } = 16;
    
    /// <summary>
    /// Gain multiplier for amplifying recorded audio (1.0 = no change, 2.0 = double volume, etc.)
    /// Recommended range: 1.0 to 4.0 for distant audio
    /// </summary>
    public float GainMultiplier { get; set; } = 2.5f;
    
    /// <summary>
    /// Enable Automatic Gain Control to boost quieter sounds
    /// </summary>
    public bool EnableAutomaticGainControl { get; set; } = true;
    
    /// <summary>
    /// Use voice recognition audio source (optimized for distant voice pickup)
    /// </summary>
    public bool UseVoiceRecognitionSource { get; set; } = true;
}

