using Android.Media;
using System;

namespace interpreter.Maui.Services;

/// <summary>
/// Interface for audio recording operations using Android AudioRecord.
/// </summary>
public interface IAudioRecorderService : IDisposable
{
    /// <summary>
    /// Gets available audio input devices (requires AudioManager to be provided in constructor).
    /// Returns null if AudioManager is not available or API level is below 23.
    /// </summary>
    AudioDeviceInfo[]? GetAvailableInputDevices();

    /// <summary>
    /// Sets the preferred audio input device for recording.
    /// Requires Android API 23+ (Marshmallow).
    /// </summary>
    bool SetPreferredDevice(AudioRecord audioRecord, AudioDeviceInfo device);

    /// <summary>
    /// Records audio for a specified duration and returns a stream containing the WAV data.
    /// </summary>
    System.IO.Stream RecordForDuration(TimeSpan duration);

    /// <summary>
    /// Creates and configures an AudioRecord instance for continuous recording with audio enhancements.
    /// </summary>
    AudioRecord CreateAudioRecord(out int bufferSize);
}

