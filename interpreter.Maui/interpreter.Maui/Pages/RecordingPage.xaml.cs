using interpreter.Maui.ViewModels;
using interpreter.Maui.Services;
using interpreter.Maui.Components;

namespace interpreter.Maui.Pages;

/// <summary>
/// Recording page for voice interpretation and transcription
/// </summary>
public partial class RecordingPage : ContentPage
{
    private readonly RecordingViewModel _viewModel;
    private readonly IAnimationService _animationService;
    private readonly IButtonStateService _buttonStateService;
    private readonly IAudioRecordingService? _audioRecordingService;
    private readonly IModalService _modalService;
    private readonly IAdjustmentService? _adjustmentService;

    public RecordingPage(
        RecordingViewModel viewModel,
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

    private void SubscribeToViewModelEvents()
    {
        _viewModel.PropertyChanged += async (sender, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.IsRecording))
            {
                await HandleRecordingStateChange();
            }
        };
    }

    #region Lifecycle Events

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        // Fire-and-forget animation to prevent blocking
        _ = _animationService.AnimatePageLoadAsync(
            LanguagePickerBorder,
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
                ModePickerBorder);

            _buttonStateService.UpdateToStartState(ActionButton, ActionIcon, ActionText);
            if (_audioRecordingService != null)
            {
                await _audioRecordingService.StopAsync();
            }
        }
    }

    #endregion

    #region UI Event Handlers

    private void OnActionButtonClicked(object? sender, EventArgs e)
    {
        _viewModel.ActionButtonCommand.Execute(null);
    }


    private async void OnModalBackgroundTapped(object? sender, EventArgs e)
    {
        await _modalService.CloseModalAsync();
    }

    #endregion
}

