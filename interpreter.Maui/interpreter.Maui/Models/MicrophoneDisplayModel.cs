using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace interpreter.Maui.Models;

/// <summary>
/// Platform-agnostic model for displaying microphone information in the UI.
/// </summary>
public class MicrophoneDisplayModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The unique identifier for this audio device.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The display name of the audio device.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of audio device as a string (e.g., "Built-in Microphone", "USB", "Bluetooth").
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is the default system microphone.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Indicates if this microphone is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public override string ToString()
    {
        return $"{Name} ({DeviceType})";
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

