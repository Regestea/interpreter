using System.Linq;
using interpreter.Maui.ViewModels;
using interpreter.Maui.Services;
using interpreter.Maui.Components;
using interpreter.Maui.Models;
using Models.Shared.Enums;
using Models.Shared.Extensions;

namespace interpreter.Maui.Pages;

/// <summary>
/// Recording page for voice interpretation and transcription
/// </summary>
public partial class RecordingPage : ContentPage
{
    private readonly RecordingViewModel _viewModel;
    private readonly IAnimationService _animationService;
    private readonly IButtonStateService _buttonStateService;
    private readonly IAndroidAudioRecordingService? _audioRecordingService;
    private readonly IModalService _modalService;
    private readonly IAdjustmentService? _adjustmentService;
    private readonly ILocalStorageService _localStorageService;
    
    private RecordingModel _recordingModel = new();
    private bool _isInitializing = false;

    public RecordingPage(
        RecordingViewModel viewModel,
        IAnimationService animationService,
        IButtonStateService buttonStateService,
        IModalService modalService,
        ILocalStorageService localStorageService,
        IAdjustmentService? adjustmentService = null,
        IAndroidAudioRecordingService? audioRecordingService = null)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        _buttonStateService = buttonStateService ?? throw new ArgumentNullException(nameof(buttonStateService));
        _modalService = modalService ?? throw new ArgumentNullException(nameof(modalService));
        _localStorageService = localStorageService ?? throw new ArgumentNullException(nameof(localStorageService));
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

    #region Page Lifecycle

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        _isInitializing = true;
        try
        {
            // Initialize pickers first so they have ItemsSource before we try to set SelectedIndex
            InitializePickers();
            // Then load and apply the model
            LoadRecordingModel();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    #endregion

    #region RecordingModel Management

    private void LoadRecordingModel()
    {
        // Load from local storage or use defaults
        var defaultModel = new RecordingModel
        {
            InputAudioLanguages = InputAudioLanguages.AutoDetect,
            OutputLanguages = OutputLanguages.English,
            EnglishVoiceModels = EnglishVoiceModels.EnUsRyanHigh,
            Modes = Modes.TranslateEverything,
            UseAndroidTts = false,
            WithTts = true,
            VoiceProfileModels = new List<VoiceProfileModel>()
        };

        _recordingModel = _localStorageService.Get(defaultModel);
        
        // Ensure VoiceProfileModels list is initialized
        if (_recordingModel.VoiceProfileModels == null)
        {
            _recordingModel.VoiceProfileModels = new List<VoiceProfileModel>();
        }
        
        ApplyModelToUI(_recordingModel);
    }

    private void SaveRecordingModel()
    {
        _localStorageService.Set(_recordingModel);
    }

    private void ApplyModelToUI(RecordingModel model)
    {
        // Set picker indices based on enum values
        SetPickerSelectedIndexByEnumName(InputAudioLanguagePicker, model.InputAudioLanguages);
        SetPickerSelectedIndexByEnumName(OutputLanguagePicker, model.OutputLanguages);
        SetPickerSelectedIndexByEnumName(EnglishVoiceModelPicker, model.EnglishVoiceModels);
        SetPickerSelectedIndexForMode(ModePicker, model.Modes);

        // Set switches
        WithTtsSwitch.IsToggled = model.WithTts;
        UseAndroidTtsSwitch.IsToggled = model.UseAndroidTts;

        UpdateOutputLanguageDependentUI(model.OutputLanguages);
    }

    #endregion

    #region Picker Initialization

    private void InitializePickers()
    {
        // Initialize Input Audio Language Picker - display enum names (English, Persian, etc.)
        var inputLanguages = Enum.GetValues<InputAudioLanguages>()
            .Select(e => e.ToString())
            .ToList();
        InputAudioLanguagePicker.ItemsSource = inputLanguages;
        InputAudioLanguagePicker.SelectionChanged += OnInputAudioLanguageChanged;

        // Initialize Output Language Picker - display enum names (English, Persian, etc.)
        var outputLanguages = Enum.GetValues<OutputLanguages>()
            .Select(e => e.ToString())
            .ToList();
        OutputLanguagePicker.ItemsSource = outputLanguages;
        OutputLanguagePicker.SelectionChanged += OnOutputLanguageChanged;

        // Initialize English Voice Model Picker - display enum names
        var voiceModels = Enum.GetValues<EnglishVoiceModels>()
            .Select(e => e.ToString())
            .ToList();
        EnglishVoiceModelPicker.ItemsSource = voiceModels;
        EnglishVoiceModelPicker.SelectionChanged += OnEnglishVoiceModelChanged;

        // Initialize Mode Picker
        var modes = Enum.GetValues<Modes>()
            .Select(m => m.ToString())
            .ToList();
        ModePicker.ItemsSource = modes;
        ModePicker.SelectionChanged += OnModeChanged;
    }

    private void SetPickerSelectedIndex(PickerCard picker, Enum enumValue)
    {
        var displayValues = picker.ItemsSource?.Cast<string>().ToList();
        if (displayValues == null || displayValues.Count == 0) return;

        var enumDisplayValue = enumValue.ToValue();
        var index = displayValues.IndexOf(enumDisplayValue);
        if (index >= 0)
        {
            picker.SelectedIndex = index;
        }
    }

    private void SetPickerSelectedIndexByEnumName(PickerCard picker, Enum enumValue)
    {
        var displayValues = picker.ItemsSource?.Cast<string>().ToList();
        if (displayValues == null || displayValues.Count == 0) return;

        var enumName = enumValue.ToString();
        var index = displayValues.IndexOf(enumName);
        if (index >= 0)
        {
            picker.SelectedIndex = index;
        }
    }

    private void SetPickerSelectedIndexForMode(PickerCard picker, Modes mode)
    {
        var displayValues = picker.ItemsSource?.Cast<string>().ToList();
        if (displayValues == null || displayValues.Count == 0) return;

        var modeString = mode.ToString();
        var index = displayValues.IndexOf(modeString);
        if (index >= 0)
        {
            picker.SelectedIndex = index;
        }
    }

    #endregion

    #region Picker Event Handlers

    private void OnInputAudioLanguageChanged(object? sender, EventArgs e)
    {
        if (_isInitializing || InputAudioLanguagePicker.SelectedIndex < 0) return;

        var selectedValue = InputAudioLanguagePicker.ItemsSource?.Cast<string>()
            .ElementAt(InputAudioLanguagePicker.SelectedIndex);
        
        if (selectedValue != null && Enum.TryParse<InputAudioLanguages>(selectedValue, out var enumValue))
        {
            _recordingModel.InputAudioLanguages = enumValue;
            SaveRecordingModel();
        }
    }

    private void OnOutputLanguageChanged(object? sender, EventArgs e)
    {
        if (_isInitializing || OutputLanguagePicker.SelectedIndex < 0) return;

        var selectedValue = OutputLanguagePicker.ItemsSource?.Cast<string>()
            .ElementAt(OutputLanguagePicker.SelectedIndex);
        
        if (selectedValue != null && Enum.TryParse<OutputLanguages>(selectedValue, out var enumValue))
        {
            _recordingModel.OutputLanguages = enumValue;
            UpdateOutputLanguageDependentUI(enumValue);
            SaveRecordingModel();
        }
    }

    private void OnEnglishVoiceModelChanged(object? sender, EventArgs e)
    {
        if (_isInitializing || EnglishVoiceModelPicker.SelectedIndex < 0) return;

        var selectedValue = EnglishVoiceModelPicker.ItemsSource?.Cast<string>()
            .ElementAt(EnglishVoiceModelPicker.SelectedIndex);
        
        if (selectedValue != null && Enum.TryParse<EnglishVoiceModels>(selectedValue, out var enumValue))
        {
            _recordingModel.EnglishVoiceModels = enumValue;
            SaveRecordingModel();
        }
    }

    private void OnModeChanged(object? sender, EventArgs e)
    {
        if (_isInitializing || ModePicker.SelectedIndex < 0) return;

        var selectedValue = ModePicker.ItemsSource?.Cast<string>()
            .ElementAt(ModePicker.SelectedIndex);
        
        if (selectedValue != null && Enum.TryParse<Modes>(selectedValue, out var enumValue))
        {
            _recordingModel.Modes = enumValue;
            SaveRecordingModel();
        }
    }

    #endregion

    #region Switch Event Handlers

    private void OnWithTtsToggled(object? sender, ToggledEventArgs e)
    {
        if (_isInitializing) return;
        _recordingModel.WithTts = e.Value;
        SaveRecordingModel();
    }

    private void OnUseAndroidTtsToggled(object? sender, ToggledEventArgs e)
    {
        if (_isInitializing) return;
        _recordingModel.UseAndroidTts = e.Value;
        SaveRecordingModel();
    }

    #endregion

    #region Output Language Helpers

    private void UpdateOutputLanguageDependentUI(OutputLanguages selectedLanguage)
    {
        var isPersian = selectedLanguage == OutputLanguages.Persian;

        EnglishVoiceModelPicker.IsEnabled = !isPersian;
        UseAndroidTtsRow.IsVisible = !isPersian;

        if (!isPersian)
        {
            return;
        }

        var modelChanged = false;

        if (UseAndroidTtsSwitch.IsToggled)
        {
            UseAndroidTtsSwitch.IsToggled = false;
        }

        if (_recordingModel.UseAndroidTts)
        {
            _recordingModel.UseAndroidTts = false;
            modelChanged = true;
        }

        if (modelChanged && _isInitializing)
        {
            SaveRecordingModel();
        }
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
                InitialStateLayout);

            _buttonStateService.UpdateToStartState(ActionButton, ActionText);
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

