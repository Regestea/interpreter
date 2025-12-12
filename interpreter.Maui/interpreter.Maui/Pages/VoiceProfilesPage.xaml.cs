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
    private bool _isInitializing = false;

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

    private async void OnProfileFormSaveClicked(object? sender, VoiceProfileSaveEventArgs e)
    {
        try
        {
            if (ProfileForm.RecordedAudioBytes == null || ProfileForm.RecordedAudioBytes.Length == 0)
            {
                await DisplayAlert("Error", "No audio data recorded.", "OK");
                return;
            }

            // Create a MemoryStream from the recorded audio bytes
            using var audioStream = new MemoryStream(ProfileForm.RecordedAudioBytes);
            
            // Create the DTO with the voice data
            var createRequest = new CreateVoiceProfileDto
            {
                Name = e.Name,
                Voice = audioStream
            };

            // Call the service to create the voice profile
            var response = await _voiceProfileService.CreateAsync(createRequest);
            
            if (response != null)
            {
                // Add to local model
                var newProfile = new VoiceProfileModel
                {
                    Id = response.Id,
                    Name = e.Name
                };
                _recordingModel.VoiceProfileModels.Add(newProfile);
                SaveRecordingModel();
                ProfileForm.Reset();
                LoadVoiceProfiles();
                
                await DisplayAlert("Success", $"Profile '{e.Name}' saved successfully.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to create voice profile.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private async void OnProfileFormRecordingStarted(object? sender, EventArgs e)
    {
        try
        {
            // Request permissions if needed
            bool hasPermission = await _audioRecordingService.RequestPermissionsAsync();
            if (!hasPermission)
            {
                await DisplayAlert("Permission Denied", "Microphone permission is required to record audio.", "OK");
                ProfileForm.Reset();
                return;
            }

            // Get the selected duration from the form
            int durationSeconds = ProfileForm.IsRecording ? 30 : 60;
            
            // Start recording
            await _audioRecordingService.StartAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to start recording: {ex.Message}", "OK");
            ProfileForm.Reset();
        }
    }

    private async void OnProfileFormRecordingStopped(object? sender, EventArgs e)
    {
        try
        {
            // Stop recording and get the audio file path
            var audioFilePath = await _audioRecordingService.StopAsync();
            
            if (!string.IsNullOrEmpty(audioFilePath) && File.Exists(audioFilePath))
            {
                // Read the audio file into bytes
                var audioBytes = await File.ReadAllBytesAsync(audioFilePath);
                ProfileForm.RecordedAudioBytes = audioBytes;
                
                ProfileForm.SetRecordingStatus("Audio recorded successfully", isSuccess: true);
            }
            else
            {
                ProfileForm.SetRecordingStatus("Failed to record audio", isSuccess: false);
            }
        }
        catch (Exception ex)
        {
            ProfileForm.SetRecordingStatus($"Recording error: {ex.Message}", isSuccess: false);
        }
    }

    private async void OnProfileFormValidationFailed(object? sender, string message)
    {
        await DisplayAlert("Validation Error", message, "OK");
    }

    #endregion

    #region Voice Profile List Events

    private void OnProfileListRefreshClicked(object? sender, EventArgs e)
    {
        LoadVoiceProfiles();
    }

    private async void OnProfileListDeleteClicked(object? sender, Guid id)
    {
        var confirm = await DisplayAlert("Delete", "Are you sure you want to delete this voice profile?", "Yes", "No");
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
                
                await DisplayAlert("Success", "Voice profile deleted successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete profile: {ex.Message}", "OK");
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
}

