namespace interpreter.Maui.Services;

/// <summary>
/// Configuration for audio recording settings.
/// </summary>
public class AudioRecordingConfiguration
{
    public int SampleRate { get; set; } = 44100;
    public int ChannelCount { get; set; } = 1; // Mono
    public int BitsPerSample { get; set; } = 16;
}

