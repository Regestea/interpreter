namespace interpreter.Maui.Components;

/// <summary>
/// A card-style button with icon and text.
/// Used for Voice Tune and Noise Control buttons.
/// </summary>
public partial class IconButton : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(IconButton), "🎵");

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(IconButton), "Button");

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(IconButton), Color.FromArgb("#FF6B6B"));

    public static readonly BindableProperty CardBackgroundColorProperty =
        BindableProperty.Create(nameof(CardBackgroundColor), typeof(Color), typeof(IconButton), Color.FromArgb("#18202B"));

    public static readonly BindableProperty CardStrokeProperty =
        BindableProperty.Create(nameof(CardStroke), typeof(Color), typeof(IconButton), Color.FromArgb("#40FFFFFF"));

    public static readonly BindableProperty ShadowColorProperty =
        BindableProperty.Create(nameof(ShadowColor), typeof(Color), typeof(IconButton), Color.FromArgb("#FF6B6B"));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
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

    public Color ShadowColor
    {
        get => (Color)GetValue(ShadowColorProperty);
        set => SetValue(ShadowColorProperty, value);
    }

    public event EventHandler<EventArgs>? Clicked;

    public IconButton()
    {
        InitializeComponent();
    }

    private void OnTapped(object? sender, EventArgs e)
    {
        Clicked?.Invoke(this, e);
    }
}

