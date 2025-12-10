using System.Linq;
using interpreter.Maui.Components;
using interpreter.Maui.ViewModels;
using interpreter.Maui.Services;
using interpreter.Maui.Models;

namespace interpreter.Maui.Pages;

/// <summary>
/// Page for managing voice profiles used in speaker recognition
/// </summary>
public partial class VoiceProfilesPage : ContentPage
{
    private readonly VoiceProfilesViewModel _viewModel;
    private readonly ILocalStorageService _localStorageService;
    
    private RecordingModel _recordingModel = new();
    private bool _isInitializing = false;

    public VoiceProfilesPage(
        VoiceProfilesViewModel viewModel,
        ILocalStorageService localStorageService)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _localStorageService = localStorageService ?? throw new ArgumentNullException(nameof(localStorageService));
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
        // TODO: Implement HTTP client request to VoiceDetectorController
        // var request = new CreateVoiceDetectorRequest { Name = e.Name, Voice = e.VoiceData };
        // var response = await _httpClient.PostAsJsonAsync("api/VoiceDetector", request);
        // if (response.IsSuccessStatusCode)
        // {
        //     var newProfile = new VoiceProfileModel 
        //     { 
        //         Id = response.Id, 
        //         Name = e.Name 
        //     };
        //     _recordingModel.VoiceProfileModels.Add(newProfile);
        //     SaveRecordingModel();
        //     ProfileForm.Reset();
        //     LoadVoiceProfiles();
        // }

        // For now, add to local model
        var newProfile = new VoiceProfileModel
        {
            Id = Guid.NewGuid(),
            Name = e.Name
        };
        _recordingModel.VoiceProfileModels.Add(newProfile);
        SaveRecordingModel();
        ProfileForm.Reset();
        LoadVoiceProfiles();
        
        await DisplayAlert("Success", $"Profile '{e.Name}' saved successfully.", "OK");
    }

    private void OnProfileFormRecordingStarted(object? sender, EventArgs e)
    {
        // TODO: Start actual audio recording
        // var bytes = await _audioRecordingService.StartRecordingAsync();
    }

    private void OnProfileFormRecordingStopped(object? sender, EventArgs e)
    {
        // TODO: Stop actual audio recording and set bytes
        // var bytes = await _audioRecordingService.StopRecordingAsync();
        // ProfileForm.RecordedAudioBytes = bytes;
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
            // TODO: Implement HTTP client request to delete
            // var response = await _httpClient.DeleteAsync($"api/VoiceDetector/{id}");
            // if (response.IsSuccessStatusCode)
            // {
            //     _recordingModel.VoiceProfileModels.RemoveAll(p => p.Id == id);
            //     // Clear selection if deleted profile was selected
            //     if (_recordingModel.SelectedVoiceProfileId == id)
            //     {
            //         _recordingModel.SelectedVoiceProfileId = null;
            //     }
            //     SaveRecordingModel();
            //     LoadVoiceProfiles();
            // }

            // For now, remove from local model
            _recordingModel.VoiceProfileModels.RemoveAll(p => p.Id == id);
            // Clear selection if deleted profile was selected
            if (_recordingModel.SelectedVoiceProfileId == id)
            {
                _recordingModel.SelectedVoiceProfileId = null;
            }
            SaveRecordingModel();
            LoadVoiceProfiles();
        }
    }

    private void LoadVoiceProfiles()
    {
        // TODO: Implement HTTP client request to get voice profiles
        // var response = await _httpClient.GetFromJsonAsync<List<VoiceEmbeddingResponse>>("api/VoiceDetector");
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

