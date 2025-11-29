using Android.Util;
using interpreter.Maui.ViewModels;
using interpreter.Maui.Services;

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
        private readonly IThemeService _themeService;
        private readonly IButtonStateService _buttonStateService;
        private readonly IAudioRecordingService? _audioRecordingService;
        private readonly IAudioPlaybackService? _audioPlaybackService;
        private readonly IModalService _modalService;
        private readonly IAdjustmentService _adjustmentService;

        // Constructor with dependency injection (Dependency Inversion Principle)
        public MainPage(
            MainViewModel viewModel,
            IAnimationService animationService,
            IThemeService themeService,
            IButtonStateService buttonStateService,
            IModalService modalService, IAdjustmentService adjustmentService, IAudioRecordingService? audioRecordingService = null,
            IAudioPlaybackService? audioPlaybackService = null)
        {
            InitializeComponent();
            
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _buttonStateService = buttonStateService ?? throw new ArgumentNullException(nameof(buttonStateService));
            _modalService = modalService ?? throw new ArgumentNullException(nameof(modalService));
            _adjustmentService = adjustmentService;
            _audioRecordingService = audioRecordingService;
            _audioPlaybackService = audioPlaybackService;

            BindingContext = _viewModel;
            
            // Initialize modal service
            if (_modalService is ModalService concreteModalService)
            {
                concreteModalService.Initialize(ModalContainer, ModalContentBorder);
            }

            // Apply dark theme immediately on page creation
            _themeService.ApplyTheme(true, this, CreateThemeElements());

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
                else if (args.PropertyName == nameof(_viewModel.IsDarkTheme))
                {
                    HandleThemeChange();
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
                    var file = await _audioRecordingService.StopAsync();
                    if (!string.IsNullOrWhiteSpace(file) && _audioPlaybackService != null)
                    {
                        await _audioPlaybackService.PlayAsync(file);
                    }
                }
            }
        }

        private void HandleThemeChange()
        {
            _themeService.ApplyTheme(_viewModel.IsDarkTheme, this, CreateThemeElements());
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
                ShowCloseButton = true,              // Show close button
                AutoCloseDurationSeconds = 5,     // No auto-close
                CloseOnBackgroundTap = true,         // Close when clicking outside
                ContentBackgroundColor = Color.FromArgb("#121821"), // Dark surface
                CloseButtonColor = Colors.White
            };
            
            // Show the modal
            await _modalService.ShowModalAsync(modalContent, options);

            await _adjustmentService.AdjustEnvironmentalNoise();
            
            _viewModel.NoiseAdjustCommand.Execute(null);
        }

        private void OnMenuClicked(object? sender, EventArgs e)
        {
            _viewModel.MenuToggleCommand.Execute(null);
        }

        private async void OnThemeToggleClicked(object? sender, EventArgs e)
        {
            if (sender is Border border)
            {
                await _animationService.AnimateButtonPressAsync(border, 0.95);
            }

            _viewModel.ThemeToggleCommand.Execute(null);
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

        /// <summary>
        /// Factory method to create theme elements DTO
        /// </summary>
        private ThemeElements CreateThemeElements()
        {
            return new ThemeElements
            {
                MainBorder = MainBorder,
                MenuFlyout = MenuFlyout,
                ThemeIcon = ThemeIcon,
                ThemeLabel = ThemeLabel,
                LanguagePickerBorder = LanguagePickerBorder,
                ModePickerBorder = ModePickerBorder,
                VoiceTuneButton = VoiceTuneButton,
                NoiseButton = NoiseButton,
                TranscriptBorder = TranscriptBorder,
                ChartBorder = ChartBorder,
                TranscriptLabel = TranscriptLabel
            };
        }

        #endregion
    }
}

