using System.Linq;
using interpreter.Maui.Components;
using interpreter.Maui.ViewModels;
using interpreter.Maui.Services;
using interpreter.Maui.Models;
using interpreter.Maui.DTOs;

namespace interpreter.Maui.Pages;

/// <summary>
/// Page for managing voice profiles used in speaker recognition
/// </summary>
public partial class VoiceProfilesPage : ContentPage
{
    private readonly VoiceProfilesViewModel _viewModel;
    private readonly ILocalStorageService _localStorageService;
    private readonly IVoiceProfileService _voiceProfileService;
    private readonly IAndroidAudioRecordingService _audioRecordingService;
    
    private RecordingModel _recordingModel = new();
    private bool _isInitializing;

    private bool _isRecording;
    private byte[]? _recordedAudioBytes;
    private int _selectedDurationSeconds = 30;
    private CancellationTokenSource? _recordingCancellationTokenSource;
    private int _recordingElapsedSeconds;
    private int _countdownSeconds;

    public VoiceProfilesPage(
        VoiceProfilesViewModel viewModel,
        ILocalStorageService localStorageService,
        IVoiceProfileService voiceProfileService,
        IAndroidAudioRecordingService audioRecordingService)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _localStorageService = localStorageService ?? throw new ArgumentNullException(nameof(localStorageService));
        _voiceProfileService = voiceProfileService ?? throw new ArgumentNullException(nameof(voiceProfileService));
        _audioRecordingService = audioRecordingService ?? throw new ArgumentNullException(nameof(audioRecordingService));
        BindingContext = _viewModel;

        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        _isInitializing = true;
        try
        {
            LoadRecordingModel();
            InitializeVoiceProfilePicker();
            LoadVoiceProfiles();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    #region RecordingModel Management

    private void LoadRecordingModel()
    {
        var defaultModel = new RecordingModel
        {
            VoiceProfileModels = new List<VoiceProfileModel>(),
            SelectedVoiceProfileId = null
        };

        _recordingModel = _localStorageService.Get(defaultModel);
        
        // Ensure VoiceProfileModels list is initialized
        if (_recordingModel.VoiceProfileModels == null)
        {
            _recordingModel.VoiceProfileModels = new List<VoiceProfileModel>();
        }
    }

    private void SaveRecordingModel()
    {
        _localStorageService.Set(_recordingModel);
    }

    #endregion

    #region Voice Profile Picker

    private void InitializeVoiceProfilePicker()
    {
        ActiveVoiceProfilePicker.ItemsSource = new List<string>();
        ActiveVoiceProfilePicker.SelectionChanged += OnActiveVoiceProfileChanged;
        UpdateVoiceProfilePicker();
    }

    private void UpdateVoiceProfilePicker()
    {
        var profileNames = _recordingModel.VoiceProfileModels
            .Select(p => string.IsNullOrWhiteSpace(p.Name) ? "Unnamed Profile" : p.Name)
            .ToList();
        
        ActiveVoiceProfilePicker.ItemsSource = profileNames;

        // Set selected index based on SelectedVoiceProfileId
        if (_recordingModel.SelectedVoiceProfileId.HasValue)
        {
            var selectedIndex = _recordingModel.VoiceProfileModels
                .FindIndex(p => p.Id == _recordingModel.SelectedVoiceProfileId.Value);
            
            if (selectedIndex >= 0 && selectedIndex < profileNames.Count)
            {
                ActiveVoiceProfilePicker.SelectedIndex = selectedIndex;
            }
            else
            {
                ActiveVoiceProfilePicker.SelectedIndex = -1;
            }
        }
        else
        {
            ActiveVoiceProfilePicker.SelectedIndex = -1;
        }
    }

    private void OnActiveVoiceProfileChanged(object? sender, EventArgs e)
    {
        if (_isInitializing || ActiveVoiceProfilePicker.SelectedIndex < 0) return;

        var profiles = _recordingModel.VoiceProfileModels;
        if (profiles == null || profiles.Count == 0) return;

        if (ActiveVoiceProfilePicker.SelectedIndex < profiles.Count)
        {
            var selectedProfile = profiles[ActiveVoiceProfilePicker.SelectedIndex];
            _recordingModel.SelectedVoiceProfileId = selectedProfile.Id;
            SaveRecordingModel();
        }
    }

    #endregion

    #region Voice Profile Form Events

    private async void OnProfileFormSaveClicked(VoiceProfileSaveEventArgs e)
    {
        try
        {
            if (_recordedAudioBytes == null || _recordedAudioBytes.Length == 0)
            {
                await DisplayAlertAsync("Error", "No audio data recorded.", "OK");
                return;
            }

            using var audioStream = new MemoryStream(_recordedAudioBytes);
            var createRequest = new CreateVoiceProfileDto
            {
                Name = e.Name,
                Voice = audioStream
            };

            var response = await _voiceProfileService.CreateAsync(createRequest);
            if (response != null)
            {
                var newProfile = new VoiceProfileModel
                {
                    Id = response.Id,
                    Name = e.Name
                };
                _recordingModel.VoiceProfileModels.Add(newProfile);
                SaveRecordingModel();
                _resetFormFields();
                LoadVoiceProfiles();

                await DisplayAlertAsync("Success", $"Profile '{e.Name}' saved successfully.", "OK");
            }
            else
            {
                await DisplayAlertAsync("Error", "Failed to create voice profile.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private async void OnProfileFormRecordingStarted()
    {
        try
        {
            bool hasPermission = await _audioRecordingService.RequestPermissionsAsync();
            if (!hasPermission)
            {
                await DisplayAlertAsync("Permission Denied", "Microphone permission is required to record audio.", "OK");
                _resetFormFields();
                return;
            }

            await _audioRecordingService.StartAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to start recording: {ex.Message}", "OK");
            _resetFormFields();
        }
    }

    private async void OnProfileFormRecordingStopped()
    {
        try
        {
            var audioFilePath = await _audioRecordingService.StopAsync();
            if (!string.IsNullOrEmpty(audioFilePath) && File.Exists(audioFilePath))
            {
                var audioBytes = await File.ReadAllBytesAsync(audioFilePath);
                _recordedAudioBytes = audioBytes;
                _updateRecordingStatus("Audio recorded successfully", true);
            }
            else
            {
                _updateRecordingStatus("Failed to record audio", false);
            }
        }
        catch (Exception ex)
        {
            _updateRecordingStatus($"Recording error: {ex.Message}", false);
        }
    }

    private async void OnProfileFormValidationFailed(object? sender, string message)
    {
        await DisplayAlertAsync("Validation Error", message, "OK");
    }

    #endregion

    #region Voice Profile List Events

    private void OnProfileListRefreshClicked(object? sender, EventArgs e)
    {
        LoadVoiceProfiles();
    }

    private async void OnProfileListDeleteClicked(object? sender, Guid id)
    {
        var confirm = await DisplayAlertAsync("Delete", "Are you sure you want to delete this voice profile?", "Yes", "No");
        if (confirm)
        {
            try
            {
                // Call the service to delete the voice profile
                await _voiceProfileService.DeleteAsync(id);
                
                _recordingModel.VoiceProfileModels.RemoveAll(p => p.Id == id);
                // Clear selection if deleted profile was selected
                if (_recordingModel.SelectedVoiceProfileId == id)
                {
                    _recordingModel.SelectedVoiceProfileId = null;
                }
                SaveRecordingModel();
                LoadVoiceProfiles();
                
                await DisplayAlertAsync("Success", "Voice profile deleted successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to delete profile: {ex.Message}", "OK");
            }
        }
    }

    private void LoadVoiceProfiles()
    {
        // TODO: Implement HTTP client request to get voice profiles
        // var response = await _voiceProfileService.GetListAsync();
        // var profileItems = response.Select(r => new VoiceProfileItem { Id = r.Id, Name = r.Name }).ToList();
        // ProfileList.UpdateItems(profileItems);
        
        // For now, load from RecordingModel
        var profileItems = _recordingModel.VoiceProfileModels
            .Select(p => new VoiceProfileItem { Id = p.Id, Name = p.Name })
            .ToList();
        
        ProfileList.UpdateItems(profileItems);
        
        // Update the picker after loading profiles
        UpdateVoiceProfilePicker();
    }

    #endregion

    private void OnDurationChanged(object? sender, EventArgs e)
    {
        if (DurationPicker.SelectedIndex >= 0)
        {
            _selectedDurationSeconds = DurationPicker.SelectedIndex == 0 ? 30 : 60;
        }
    }

    private void OnNameTextChanged(object? sender, TextChangedEventArgs e)
    {
        NameEntryBorder.Stroke = Color.FromArgb("#374151");
    }

    private async void OnRecordClicked(object? sender, EventArgs e)
    {
        if (!_isRecording)
        {
            _isRecording = true;
            _recordingElapsedSeconds = 0;
            _countdownSeconds = _selectedDurationSeconds;
            _recordingCancellationTokenSource = new CancellationTokenSource();
            
            RecordButton.BackgroundColor = Color.FromArgb("#22C55E");
            RecordIcon.Text = "⏹️";
            RecordingStatusLabel.Text = "Recording...";
            RecordingStatusLabel.TextColor = Color.FromArgb("#EF4444");
            DurationPicker.IsEnabled = false;

            await RecordButton.ScaleToAsync(1.1, 200);
            await RecordButton.ScaleToAsync(1.0, 200);

            StartCountdownTimer(_recordingCancellationTokenSource.Token);
        }
        else
        {
            _isRecording = false;
            _recordingCancellationTokenSource?.Cancel();
            
            RecordButton.BackgroundColor = Color.FromArgb("#EF4444");
            RecordIcon.Text = "🎙️";
            RecordingStatusLabel.Text = "Recording saved ✓";
            RecordingStatusLabel.TextColor = Color.FromArgb("#22C55E");
            RecordingCountdownLabel.Text = string.Empty;
            DurationPicker.IsEnabled = true;
        }
    }

    private void StartCountdownTimer(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (_countdownSeconds > 0 && !cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RecordingCountdownLabel.Text = $"{_countdownSeconds}s remaining";
                    double progress = (double)_recordingElapsedSeconds / _selectedDurationSeconds;
                    RecordingProgressBar.Progress = progress;
                    RecordingLengthLabel.Text = $"{_recordingElapsedSeconds}s";
                });

                await Task.Delay(1000, cancellationToken);
                _recordingElapsedSeconds++;
                _countdownSeconds--;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _isRecording = false;
                    RecordButton.BackgroundColor = Color.FromArgb("#EF4444");
                    RecordIcon.Text = "🎙️";
                    RecordingStatusLabel.Text = "Recording complete!";
                    RecordingCountdownLabel.Text = string.Empty;
                    RecordingStatusLabel.TextColor = Color.FromArgb("#22C55E");
                    DurationPicker.IsEnabled = true;
                    RecordingProgressBar.Progress = 1.0;
                    RecordingLengthLabel.Text = $"{_selectedDurationSeconds}s";
                });
            }
        }, cancellationToken);
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text;

        if (string.IsNullOrWhiteSpace(name))
        {
            NameEntryBorder.Stroke = Color.FromArgb("#EF4444");
            return;
        }

        if (_recordedAudioBytes == null || _recordedAudioBytes.Length == 0)
        {
            return;
        }

        // Save logic here
    }

    private void _resetFormFields()
    {
        NameEntry.Text = string.Empty;
        RecordingStatusLabel.Text = "";
        RecordingCountdownLabel.Text = "";
        RecordingProgressBar.Progress = 0;
        RecordingLengthLabel.Text = "";
        _recordedAudioBytes = null;
        _isRecording = false;
    }

    private void _updateRecordingStatus(string message, bool isSuccess)
    {
        RecordingStatusLabel.Text = message;
        RecordingStatusLabel.TextColor = isSuccess ? Color.FromArgb("#22C55E") : Color.FromArgb("#EF4444");
    }

    // Updated DisplayAlert logic
    private new async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        var mainPage = Application.Current?.Windows[0]?.Page;
        if (mainPage != null)
        {
            await mainPage.DisplayAlertAsync(title, message, cancel);
        }
    }

    private new async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        var mainPage = Application.Current?.Windows[0]?.Page;
        if (mainPage != null)
        {
            return await mainPage.DisplayAlertAsync(title, message, accept, cancel);
        }
        return false;
    }

    // Added definition for VoiceProfileSaveEventArgs
    public class VoiceProfileSaveEventArgs : EventArgs
    {
        public string Name { get; set; } = string.Empty;
    }
}
