using Android.Content;
using Android.Media;

namespace interpreter.Maui.Services;

/// <summary>
/// Singleton class that stores the selected/default microphone settings.
/// Initializes with the default phone microphone.
/// </summary>
public sealed class MicrophoneSettings
{
    private static readonly Lazy<MicrophoneSettings> s_instance = new(() => new MicrophoneSettings());
    private MicrophoneInfo? _selectedMicrophone;
    private readonly object _lock = new();

    /// <summary>
    /// Gets the singleton instance of MicrophoneSettings.
    /// </summary>
    public static MicrophoneSettings Instance => s_instance.Value;

    /// <summary>
    /// Private constructor to enforce singleton pattern.
    /// </summary>
    private MicrophoneSettings()
    {
    }

    /// <summary>
    /// Gets or sets the currently selected microphone.
    /// Thread-safe property.
    /// </summary>
    public MicrophoneInfo? SelectedMicrophone
    {
        get
        {
            lock (_lock)
            {
                return _selectedMicrophone;
            }
        }
        set
        {
            lock (_lock)
            {
                _selectedMicrophone = value;
            }
            // Invoke event outside lock to prevent deadlocks
            OnMicrophoneChanged?.Invoke(this, value);
        }
    }

    /// <summary>
    /// Event raised when the selected microphone changes.
    /// </summary>
    public event EventHandler<MicrophoneInfo?>? OnMicrophoneChanged;

    /// <summary>
    /// Initializes the microphone settings with available devices.
    /// Sets the default microphone to the built-in phone microphone.
    /// </summary>
    /// <param name="context">Android context to access AudioManager.</param>
    public void Initialize(Context context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var audioManager = context.GetSystemService(Context.AudioService) as AudioManager;
        if (audioManager == null)
            return;

        InitializeWithAudioManager(audioManager);
    }

    /// <summary>
    /// Initializes the microphone settings with the provided AudioManager.
    /// Sets the default microphone to the built-in phone microphone.
    /// </summary>
    /// <param name="audioManager">The AudioManager instance.</param>
    public void InitializeWithAudioManager(AudioManager audioManager)
    {
        if (audioManager == null)
            throw new ArgumentNullException(nameof(audioManager));

#pragma warning disable CA1416
        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
        {
            // API level below 23, cannot enumerate devices
            // Set a default placeholder
            SelectedMicrophone = new MicrophoneInfo
            {
                Id = 0,
                Name = "Default Microphone",
                DeviceType = AudioDeviceType.BuiltinMic,
                IsDefault = true,
                AndroidDeviceInfo = null
            };
            return;
        }

        var devices = audioManager.GetDevices(GetDevicesTargets.Inputs);
        if (devices == null || devices.Length == 0)
        {
            return;
        }

        // Find the built-in microphone as default
        AudioDeviceInfo? defaultDevice = null;
        foreach (var device in devices)
        {
            if (device.Type == AudioDeviceType.BuiltinMic)
            {
                defaultDevice = device;
                break;
            }
        }

        // If no built-in mic found, use the first available input device
        defaultDevice ??= devices[0];

        SelectedMicrophone = new MicrophoneInfo
        {
            Id = defaultDevice.Id,
            Name = defaultDevice.ProductName ?? "Default Microphone",
            DeviceType = defaultDevice.Type,
            IsDefault = true,
            AndroidDeviceInfo = defaultDevice
        };
#pragma warning restore CA1416
    }

    /// <summary>
    /// Resets the selected microphone to null.
    /// </summary>
    public void Reset()
    {
        SelectedMicrophone = null;
    }
}

