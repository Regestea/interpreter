using interpreter.Maui.Components;
using interpreter.Maui.ViewModels;

namespace interpreter.Maui.Pages;

/// <summary>
/// Page for managing voice profiles used in speaker recognition
/// </summary>
public partial class VoiceProfilesPage : ContentPage
{
    private readonly VoiceProfilesViewModel _viewModel;

    public VoiceProfilesPage(VoiceProfilesViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        LoadVoiceProfiles();
    }

    #region Voice Profile Form Events

    private async void OnProfileFormSaveClicked(object? sender, VoiceProfileSaveEventArgs e)
    {
        // TODO: Implement HTTP client request to VoiceDetectorController
        // var request = new CreateVoiceDetectorRequest { Name = e.Name, Voice = e.VoiceData };
        // var response = await _httpClient.PostAsJsonAsync("api/VoiceDetector", request);
        // if (response.IsSuccessStatusCode)
        // {
        //     ProfileForm.Reset();
        //     LoadVoiceProfiles();
        // }

        await DisplayAlert("Info", $"Save profile '{e.Name}' - HTTP client not implemented yet.", "OK");
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
            //     LoadVoiceProfiles();
            // }

            await DisplayAlert("Info", $"Delete profile ID: {id} - HTTP client not implemented yet.", "OK");
        }
    }

    private void LoadVoiceProfiles()
    {
        // TODO: Implement HTTP client request to get voice profiles
        // var response = await _httpClient.GetFromJsonAsync<List<VoiceEmbeddingResponse>>("api/VoiceDetector");
        // ProfileList.UpdateItems(response.Select(r => new VoiceProfileItem { Id = r.Id, Name = r.Name }));

        // For now, show empty list
        ProfileList.UpdateItems(new List<VoiceProfileItem>());
    }

    #endregion
}

