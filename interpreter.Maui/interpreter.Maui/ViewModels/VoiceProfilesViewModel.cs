using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace interpreter.Maui.ViewModels;

/// <summary>
/// ViewModel for VoiceProfilesPage following MVVM pattern
/// </summary>
public class VoiceProfilesViewModel : INotifyPropertyChanged
{
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public VoiceProfilesViewModel()
    {
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

