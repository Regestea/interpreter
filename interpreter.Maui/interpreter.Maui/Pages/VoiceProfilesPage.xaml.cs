using System.Linq;
using interpreter.Maui.Components;
using interpreter.Maui.ViewModels;
using interpreter.Maui.Services;
using interpreter.Maui.Models;
using interpreter.Maui.DTOs;
using Models.Shared.Responses;

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
    private Stream? _recordedAudioStream;
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
        _audioRecordingService =
            audioRecordingService ?? throw new ArgumentNullException(nameof(audioRecordingService));
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

    private async Task InitializeVoiceProfilePicker()
    {
        ActiveVoiceProfilePicker.ItemsSource = new List<string>();
        ActiveVoiceProfilePicker.SelectionChanged += OnActiveVoiceProfileChanged;
        var result = await _voiceProfileService.GetListAsync();
        UpdateVoiceProfilePicker(result);
    }

    private void UpdateVoiceProfilePicker(List<VoiceEmbeddingResponse>? profiles = null)
    {
        // Retrieve the current recording model from local storage
        var localSaved = _localStorageService.Get<RecordingModel>() ?? new RecordingModel();

        // Update the VoiceProfileModels with the provided profiles
        if (profiles != null)
        {
            localSaved.VoiceProfileModels = profiles.Select(p => new VoiceProfileModel
            {
                Id = p.Id,
                Name = p.Name
            }).ToList();

            // Save the updated recording model back to local storage
            _localStorageService.Set(localSaved);
        }

        // Update the picker items
        var profileNames = localSaved.VoiceProfileModels.Select(x => x.Name).ToList();
        ActiveVoiceProfilePicker.ItemsSource = profileNames;

        // Set selected index based on SelectedVoiceProfileId
        if (localSaved.SelectedVoiceProfileId.HasValue)
        {
            var selectedIndex = localSaved.VoiceProfileModels
                .FindIndex(p => p.Id == localSaved.SelectedVoiceProfileId.Value);

            ActiveVoiceProfilePicker.SelectedIndex = selectedIndex >= 0 && selectedIndex < profileNames.Count
                ? selectedIndex
                : -1;
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

            // Retrieve the current recording model from local storage
            var localSaved = _localStorageService.Get<RecordingModel>() ?? new RecordingModel();

            // Update the selected voice profile ID
            localSaved.SelectedVoiceProfileId = selectedProfile.Id;

            // Save the updated recording model back to local storage
            _localStorageService.Set(localSaved);

            // Update the in-memory recording model
            _recordingModel.SelectedVoiceProfileId = selectedProfile.Id;

            // Persist the changes
            SaveRecordingModel();
        }
    }

    #endregion


    #region Voice Profile List Events

    private void OnProfileListRefreshClicked(object? sender, EventArgs e)
    {
        LoadVoiceProfiles();
    }

    private async void OnProfileListDeleteClicked(object? sender, Guid id)
    {
        var confirm =
            await DisplayAlertAsync("Delete", "Are you sure you want to delete this voice profile?", "Yes", "No");
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

    private async Task LoadVoiceProfiles()
    {
        var response = await _voiceProfileService.GetListAsync();
        var profileItems = response.Select(r => new VoiceProfileItem { Id = r.Id, Name = r.Name }).ToList();
        ProfileList.UpdateItems(profileItems);

        // Update the picker after loading profiles
        UpdateVoiceProfilePicker(response);
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

            // Start recording
            try
            {
                _recordedAudioStream =
                    await _audioRecordingService.RecordAudioTrack(durationSeconds: _selectedDurationSeconds);
            }
            catch (Exception ex)
            {
                _updateRecordingStatus($"Recording error: {ex.Message}", false);
                _isRecording = false;
                return;
            }
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
                MainThread.BeginInvokeOnMainThread(async () =>
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

                    // Stop recording and process the audio
                    try
                    {
                        if (_recordedAudioStream != null)
                        {
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
                });
            }
        }, cancellationToken);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text;

        if (string.IsNullOrWhiteSpace(name))
        {
            NameEntryBorder.Stroke = Color.FromArgb("#EF4444");
            return;
        }

        if (_recordedAudioStream == null)
        {
            return;
        }

        await _voiceProfileService.CreateAsync(new CreateVoiceProfileDto()
        {
            Name = name,
            Voice = _recordedAudioStream
        });
        NameEntry.Text= string.Empty;
        _recordedAudioStream = null;
        _updateRecordingStatus("",true);
        _countdownSeconds = 0;
        await LoadVoiceProfiles();
        // Save logic here
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
}