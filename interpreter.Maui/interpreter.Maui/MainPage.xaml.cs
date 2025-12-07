using interpreter.Maui.ViewModels;
using interpreter.Maui.Services;
using interpreter.Maui.Components;

namespace interpreter.Maui
{
    /// <summary>
    /// Main page refactored following MVVM pattern and SOLID principles
    /// View layer is responsible only for UI interactions and animations
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private readonly IAnimationService _animationService;
        private readonly IButtonStateService _buttonStateService;
        private readonly IAudioRecordingService? _audioRecordingService;
        private readonly IModalService _modalService;
        private readonly IAdjustmentService? _adjustmentService;

        // Constructor with dependency injection (Dependency Inversion Principle)
        public MainPage(
            MainViewModel viewModel,
            IAnimationService animationService,
            IButtonStateService buttonStateService,
            IModalService modalService,
            IAdjustmentService? adjustmentService = null,
            IAudioRecordingService? audioRecordingService = null)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
            _buttonStateService = buttonStateService ?? throw new ArgumentNullException(nameof(buttonStateService));
            _modalService = modalService ?? throw new ArgumentNullException(nameof(modalService));
            _adjustmentService = adjustmentService;
            _audioRecordingService = audioRecordingService;

            BindingContext = _viewModel;

            // Initialize modal service
            if (_modalService is ModalService concreteModalService)
            {
                concreteModalService.Initialize(ModalContainer, ModalContentBorder);
            }

            SubscribeToViewModelEvents();
            Loaded += OnPageLoaded;
        }

        /// <summary>
        /// Subscribe to ViewModel property changes (Observer pattern)
        /// </summary>
        private void SubscribeToViewModelEvents()
        {
            _viewModel.PropertyChanged += async (sender, args) =>
            {
                if (args.PropertyName == nameof(_viewModel.IsRecording))
                {
                    await HandleRecordingStateChange();
                }
                else if (args.PropertyName == nameof(_viewModel.IsMenuVisible))
                {
                    await HandleMenuVisibilityChange();
                }
            };
        }

        #region Lifecycle Events

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            await _animationService.AnimatePageLoadAsync(
                LanguagePickerBorder,
                ModePickerBorder,
                VoiceTuneButton,
                NoiseButton);
        }

        #endregion

        #region ViewModel Event Handlers

        private async Task HandleRecordingStateChange()
        {
            if (_viewModel.IsRecording)
            {
                if (_audioRecordingService != null)
                {
                    var granted = await _audioRecordingService.RequestPermissionsAsync();
                    if (granted)
                    {
                        await _audioRecordingService.StartAsync();
                    }
                }

                await _animationService.AnimateToRecordingStateAsync(
                    InitialStateLayout,
                    RecordingStateLayout,
                    ActionButton,
                    TranscriptBorder,
                    ChartBorder);

                _buttonStateService.UpdateToStopState(ActionButton, ActionIcon, ActionText);

                // Start pulse animation
                _ = _animationService.AnimatePulseAsync(
                    ActionButton,
                    () => _viewModel.IsRecording && RecordingStateLayout.IsVisible);
            }
            else
            {
                await _animationService.AnimateButtonPressAsync(ActionButton, 0.9);

                await _animationService.AnimateToInitialStateAsync(
                    RecordingStateLayout,
                    InitialStateLayout,
                    LanguagePickerBorder,
                    ModePickerBorder,
                    VoiceTuneButton,
                    NoiseButton);

                _buttonStateService.UpdateToStartState(ActionButton, ActionIcon, ActionText);
                if (_audioRecordingService != null)
                {
                    await _audioRecordingService.StopAsync();
                }
            }
        }


        private async Task HandleMenuVisibilityChange()
        {
            await _animationService.AnimateFadeToggleAsync(MenuFlyout, _viewModel.IsMenuVisible);
        }

        #endregion

        #region UI Event Handlers

        private void OnActionButtonClicked(object? sender, EventArgs e)
        {
            _viewModel.ActionButtonCommand.Execute(null);
        }

        private async void OnVoiceTuneClicked(object? sender, EventArgs e)
        {
            await _animationService.AnimateButtonPressAsync(VoiceTuneButton);
            _viewModel.VoiceTuneCommand.Execute(null);
        }

        private async void OnNoiseAdjustClicked(object? sender, EventArgs e)
        {
            await _animationService.AnimateButtonPressAsync(NoiseButton);

            // Create simple text modal content
            var modalContent = CreateNoiseAdjustModalContent();

            // Configure modal options with theme-aware colors
            var options = new ModalOptions
            {
                ShowCloseButton = true, // Show close button
                AutoCloseDurationSeconds = 5, // No auto-close
                CloseOnBackgroundTap = true, // Close when clicking outside
                ContentBackgroundColor = Color.FromArgb("#121821"), // Dark surface
                CloseButtonColor = Colors.White
            };

            // Show the modal
            await _modalService.ShowModalAsync(modalContent, options);

            if (_adjustmentService != null)
            {
                await _adjustmentService.AdjustEnvironmentalNoise();
            }

            _viewModel.NoiseAdjustCommand.Execute(null);
        }

        private async void OnVoiceDetectorClicked(object? sender, EventArgs e)
        {
            // Close the menu flyout first
            _viewModel.IsMenuVisible = false;

            // Show Voice Detector Layout
            await ShowVoiceDetectorLayout();
        }

        private async void OnVoiceDetectorBackClicked(object? sender, EventArgs e)
        {
            // Hide Voice Detector Layout and show Initial Layout
            await HideVoiceDetectorLayout();
        }

        private async Task ShowVoiceDetectorLayout()
        {
            // Hide initial state
            await InitialStateLayout.FadeToAsync(0, 200);
            InitialStateLayout.IsVisible = false;
            
            // Hide action button
            await ActionButton.FadeToAsync(0, 200);
            ActionButton.IsVisible = false;

            // Show voice detector layout
            VoiceDetectorLayout.IsVisible = true;
            await VoiceDetectorLayout.FadeToAsync(1, 300);
            
            // Load voice profiles
            LoadVoiceProfiles();
        }

        private async Task HideVoiceDetectorLayout()
        {
            // Hide voice detector layout
            await VoiceDetectorLayout.FadeToAsync(0, 200);
            VoiceDetectorLayout.IsVisible = false;

            // Show initial state
            InitialStateLayout.IsVisible = true;
            await InitialStateLayout.FadeToAsync(1, 300);
            
            // Show action button
            ActionButton.IsVisible = true;
            await ActionButton.FadeToAsync(1, 300);
        }

        #endregion

        #region Voice Detector Events

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

        #region Helper Methods

        /// <summary>
        /// Creates the content for the noise adjust modal
        /// </summary>
        private View CreateNoiseAdjustModalContent()
        {
            // Simple text label with theme-aware color
            var label = new Label
            {
                Text = "🔇 Noise Control Settings",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Colors.White,
                Padding = new Thickness(20)
            };

            return label;
        }

        /// <summary>
        /// Handle tap on modal background
        /// </summary>
        private async void OnModalBackgroundTapped(object? sender, EventArgs e)
        {
            // Only close if tapped on the background grid itself, not on the content
            await _modalService.CloseModalAsync();
        }

        #endregion

        #region Navigation

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            try
            {
                var location = Shell.Current?.CurrentState?.Location;
                if (location == null)
                    return;

                // Parse query manually from the Uri (e.g. //MainPage?showVoiceDetector=true)
                var queryString = location.OriginalString;
                if (string.IsNullOrEmpty(queryString))
                    return;

                // Check if the query contains showVoiceDetector=true
                if (queryString.Contains("showVoiceDetector=true", StringComparison.OrdinalIgnoreCase))
                {
                    if (!VoiceDetectorLayout.IsVisible)
                    {
                        await ShowVoiceDetectorLayout();
                    }
                }
            }
            catch
            {
                // Silently ignore any parsing errors on startup
            }
        }

        #endregion
    }
}


