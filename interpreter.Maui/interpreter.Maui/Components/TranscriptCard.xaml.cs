namespace interpreter.Maui.Components;

/// <summary>
/// A card component for displaying transcript/transcription text.
/// </summary>
public partial class TranscriptCard : ContentView
{
    public static readonly BindableProperty TranscriptTextProperty =
        BindableProperty.Create(nameof(TranscriptText), typeof(string), typeof(TranscriptCard), 
            "Your transcription will appear here...");

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(TranscriptCard), Colors.White);

    public static readonly BindableProperty CardBackgroundColorProperty =
        BindableProperty.Create(nameof(CardBackgroundColor), typeof(Color), typeof(TranscriptCard), 
            Color.FromArgb("#18202B"));

    public static readonly BindableProperty CardStrokeProperty =
        BindableProperty.Create(nameof(CardStroke), typeof(Color), typeof(TranscriptCard), 
            Color.FromArgb("#40FFFFFF"));

    public string TranscriptText
    {
        get => (string)GetValue(TranscriptTextProperty);
        set => SetValue(TranscriptTextProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Color CardBackgroundColor
    {
        get => (Color)GetValue(CardBackgroundColorProperty);
        set => SetValue(CardBackgroundColorProperty, value);
    }

    public Color CardStroke
    {
        get => (Color)GetValue(CardStrokeProperty);
        set => SetValue(CardStrokeProperty, value);
    }

    /// <summary>
    /// Gets the inner label for external styling
    /// </summary>
    public Label TranscriptLabelElement => TranscriptLabel;

    public TranscriptCard()
    {
        InitializeComponent();
    }
}

