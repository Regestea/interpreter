using Android.Content;
using Android.Media;

namespace interpreter.Maui.Services;

/// <summary>
/// Service for managing microphone devices on Android.
/// Uses MicrophoneSettings singleton for storing the selected microphone.
/// </summary>
public class MicrophoneManagerService : IMicrophoneManagerService
{
    private readonly AudioManager? _audioManager;
    private readonly List<MicrophoneInfo> _availableMicrophones = new();

    /// <summary>
    /// Creates a new MicrophoneManagerService with a Context.
    /// </summary>
    /// <param name="context">Android context to access AudioManager.</param>
    public MicrophoneManagerService(Context context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        _audioManager = context.GetSystemService(Context.AudioService) as AudioManager;
        Initialize();
    }

    /// <summary>
    /// Creates a new MicrophoneManagerService with an AudioManager.
    /// </summary>
    /// <param name="audioManager">The AudioManager instance.</param>
    public MicrophoneManagerService(AudioManager? audioManager)
    {
        _audioManager = audioManager;
        Initialize();
    }

    private void Initialize()
    {
        RefreshMicrophones();
        
        // Initialize MicrophoneSettings with default microphone if not already set
        if (MicrophoneSettings.Instance.SelectedMicrophone == null && _audioManager != null)
        {
            MicrophoneSettings.Instance.InitializeWithAudioManager(_audioManager);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IMicrophoneInfo> GetAvailableMicrophones()
    {
        return _availableMicrophones.AsReadOnly();
    }

    /// <inheritdoc />
    public IMicrophoneInfo? GetSelectedMicrophone()
    {
        return MicrophoneSettings.Instance.SelectedMicrophone;
    }

    /// <inheritdoc />
    public bool SetSelectedMicrophone(int microphoneId)
    {
        var microphone = _availableMicrophones.FirstOrDefault(m => m.Id == microphoneId);
        if (microphone == null)
            return false;

        MicrophoneSettings.Instance.SelectedMicrophone = microphone;
        return true;
    }

    /// <inheritdoc />
    public void SetSelectedMicrophone(IMicrophoneInfo microphone)
    {
        if (microphone == null)
            throw new ArgumentNullException(nameof(microphone));

        if (microphone is MicrophoneInfo micInfo)
        {
            MicrophoneSettings.Instance.SelectedMicrophone = micInfo;
        }
        else
        {
            // Find matching microphone by ID
            var existingMic = _availableMicrophones.FirstOrDefault(m => m.Id == microphone.Id);
            if (existingMic != null)
            {
                MicrophoneSettings.Instance.SelectedMicrophone = existingMic;
            }
        }
    }

    /// <inheritdoc />
    public void ResetToDefault()
    {
        if (_audioManager != null)
        {
            MicrophoneSettings.Instance.InitializeWithAudioManager(_audioManager);
        }
    }

    /// <inheritdoc />
    public void RefreshMicrophones()
    {
        _availableMicrophones.Clear();

#pragma warning disable CA1416
        if (_audioManager == null)
            return;

        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
        {
            // API level below 23, add a default placeholder
            _availableMicrophones.Add(new MicrophoneInfo
            {
                Id = 0,
                Name = "Default Microphone",
                DeviceType = AudioDeviceType.BuiltinMic,
                IsDefault = true,
                AndroidDeviceInfo = null
            });
            return;
        }

        var devices = _audioManager.GetDevices(GetDevicesTargets.Inputs);
        if (devices == null || devices.Length == 0)
            return;

        // Find the built-in microphone to mark as default
        var builtInMicId = -1;
        foreach (var device in devices)
        {
            if (device.Type == AudioDeviceType.BuiltinMic)
            {
                builtInMicId = device.Id;
                break;
            }
        }

        // If no built-in mic found, use the first device as default
        if (builtInMicId == -1 && devices.Length > 0)
        {
            builtInMicId = devices[0].Id;
        }

        foreach (var device in devices)
        {
            _availableMicrophones.Add(new MicrophoneInfo
            {
                Id = device.Id,
                Name = device.ProductName ?? GetDefaultNameForType(device.Type),
                DeviceType = device.Type,
                IsDefault = device.Id == builtInMicId,
                AndroidDeviceInfo = device
            });
        }
#pragma warning restore CA1416
    }

#pragma warning disable CA1416
    private static string GetDefaultNameForType(AudioDeviceType type)
    {
        return type switch
        {
            AudioDeviceType.BuiltinMic => "Built-in Microphone",
            AudioDeviceType.WiredHeadset => "Wired Headset Microphone",
            AudioDeviceType.BluetoothSco => "Bluetooth Microphone",
            AudioDeviceType.BluetoothA2dp => "Bluetooth A2DP Microphone",
            AudioDeviceType.UsbDevice => "USB Microphone",
            AudioDeviceType.UsbAccessory => "USB Accessory Microphone",
            AudioDeviceType.UsbHeadset => "USB Headset Microphone",
            _ => $"Microphone ({type})"
        };
    }
#pragma warning restore CA1416
}

