using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using interpreter.Maui.Models;
using interpreter.Maui.Services;

namespace interpreter.Maui.ViewModels;

/// <summary>
/// ViewModel for the Microphone Manager page.
/// </summary>
public class MicrophoneManagerViewModel : INotifyPropertyChanged
{
    private readonly IMicrophoneManagerService? _microphoneManager;
    private MicrophoneDisplayModel? _selectedMicrophone;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private bool _isUpdatingSelection;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MicrophoneDisplayModel> Microphones { get; } = new();

    public MicrophoneDisplayModel? SelectedMicrophone
    {
        get => _selectedMicrophone;
        set
        {
            if (_selectedMicrophone != value && !_isUpdatingSelection)
            {
                _selectedMicrophone = value;
                OnPropertyChanged();
                
                if (value != null)
                {
                    Task.Run(() => SetSelectedMicrophone(value));
                }
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public Command RefreshCommand { get; }
    public Command ResetToDefaultCommand { get; }

    public MicrophoneManagerViewModel(IMicrophoneManagerService? microphoneManager = null)
    {
        _microphoneManager = microphoneManager;
        RefreshCommand = new Command(RefreshMicrophones);
        ResetToDefaultCommand = new Command(ResetToDefault);
    }

    /// <summary>
    /// Loads the available microphones from the service.
    /// </summary>
    public void LoadMicrophones()
    {
        if (_microphoneManager == null)
        {
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = "Microphone service not available");
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsLoading = true;
            StatusMessage = "Loading microphones...";
        });

        try
        {
            _microphoneManager.RefreshMicrophones();
            var microphones = _microphoneManager.GetAvailableMicrophones();
            var selectedMic = _microphoneManager.GetSelectedMicrophone();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isUpdatingSelection = true;
                try
                {
                    Microphones.Clear();

                    foreach (var mic in microphones)
                    {
                        var displayModel = new MicrophoneDisplayModel
                        {
                            Id = mic.Id,
                            Name = mic.Name,
                            DeviceType = mic.DeviceTypeName,
                            IsDefault = mic.IsDefault,
                            IsSelected = selectedMic != null && mic.Id == selectedMic.Id
                        };

                        Microphones.Add(displayModel);

                        if (displayModel.IsSelected)
                        {
                            _selectedMicrophone = displayModel;
                            OnPropertyChanged(nameof(SelectedMicrophone));
                        }
                    }

                    StatusMessage = $"Found {Microphones.Count} microphone(s)";
                }
                finally
                {
                    _isUpdatingSelection = false;
                    IsLoading = false;
                }
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = $"Error loading microphones: {ex.Message}";
                IsLoading = false;
            });
        }
    }

    private void RefreshMicrophones()
    {
        Task.Run(() => LoadMicrophones());
    }

    private void SetSelectedMicrophone(MicrophoneDisplayModel microphone)
    {
        if (_microphoneManager == null)
            return;

        try
        {
            if (_microphoneManager.SetSelectedMicrophone(microphone.Id))
            {
                // Update IsSelected state for all microphones
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var mic in Microphones)
                    {
                        mic.IsSelected = mic.Id == microphone.Id;
                    }
                    StatusMessage = $"Selected: {microphone.Name}";
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => 
                    StatusMessage = $"Failed to select microphone: {microphone.Name}");
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => 
                StatusMessage = $"Error selecting microphone: {ex.Message}");
        }
    }

    private void ResetToDefault()
    {
        if (_microphoneManager == null)
            return;

        Task.Run(() =>
        {
            try
            {
                _microphoneManager.ResetToDefault();
                LoadMicrophones();
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => 
                    StatusMessage = $"Error resetting to default: {ex.Message}");
            }
        });
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

