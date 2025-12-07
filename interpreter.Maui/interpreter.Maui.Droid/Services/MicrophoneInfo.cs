using Android.Media;

namespace interpreter.Maui.Services;

/// <summary>
/// Represents information about an audio input device (microphone).
/// </summary>
public class MicrophoneInfo : IMicrophoneInfo
{
    /// <summary>
    /// The unique identifier for this audio device.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The product name of the audio device.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of audio device (e.g., built-in mic, USB, Bluetooth).
    /// </summary>
    public AudioDeviceType DeviceType { get; set; }

    /// <summary>
    /// The type of audio device as a string for platform-agnostic usage.
    /// </summary>
    public string DeviceTypeName => DeviceType.ToString();

    /// <summary>
    /// Indicates if this is the default system microphone.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// The underlying Android AudioDeviceInfo object.
    /// </summary>
    public AudioDeviceInfo? AndroidDeviceInfo { get; set; }

    public override string ToString()
    {
        return $"{Name} ({DeviceType})";
    }
}

