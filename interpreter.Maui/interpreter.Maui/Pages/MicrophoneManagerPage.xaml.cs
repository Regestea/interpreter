using interpreter.Maui.ViewModels;

namespace interpreter.Maui.Pages;

/// <summary>
/// Page for managing microphone selection.
/// </summary>
public partial class MicrophoneManagerPage : ContentPage
{
    private readonly MicrophoneManagerViewModel _viewModel;

    public MicrophoneManagerPage(MicrophoneManagerViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        // Load microphones on a background thread to prevent UI freeze
        await Task.Run(() => _viewModel.LoadMicrophones());
    }
}

