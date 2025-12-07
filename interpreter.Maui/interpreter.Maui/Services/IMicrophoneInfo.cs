namespace interpreter.Maui.Services;

/// <summary>
/// Platform-agnostic interface for microphone information.
/// </summary>
public interface IMicrophoneInfo
{
    /// <summary>
    /// The unique identifier for this audio device.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// The product name of the audio device.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The type of audio device as a string (e.g., "BuiltinMic", "USB", "Bluetooth").
    /// </summary>
    string DeviceTypeName { get; }

    /// <summary>
    /// Indicates if this is the default system microphone.
    /// </summary>
    bool IsDefault { get; }
}

