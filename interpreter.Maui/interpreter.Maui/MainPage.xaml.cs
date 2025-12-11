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

        // Constructor with dependency injection (Dependency Inversion Principle)
        public MainPage(
            MainViewModel viewModel,
            IAnimationService animationService,
            IButtonStateService buttonStateService,
            IModalService modalService,
            IAudioRecordingService? audioRecordingService = null)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
            _buttonStateService = buttonStateService ?? throw new ArgumentNullException(nameof(buttonStateService));
            _modalService = modalService ?? throw new ArgumentNullException(nameof(modalService));
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
                ModePickerBorder);
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

                _buttonStateService.UpdateToStopState(ActionButton, ActionText);

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
                    ModePickerBorder);

                _buttonStateService.UpdateToStartState(ActionButton, ActionText);
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



        #endregion

        #region Helper Methods

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

        #endregion
    }
}


