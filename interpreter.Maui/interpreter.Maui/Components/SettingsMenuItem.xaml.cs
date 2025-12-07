namespace interpreter.Maui.Components;

/// <summary>
/// A menu item for settings flyout with icon and text.
/// </summary>
public partial class SettingsMenuItem : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(SettingsMenuItem), "⚙️");

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(SettingsMenuItem), "Menu Item");

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(SettingsMenuItem), Colors.White);

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

    /// <summary>
    /// Gets the icon label for external styling
    /// </summary>
    public Label IconLabelElement => IconLabel;

    /// <summary>
    /// Gets the text label for external styling
    /// </summary>
    public Label TextLabelElement => TextLabel;

    public event EventHandler<EventArgs>? Clicked;

    public SettingsMenuItem()
    {
        InitializeComponent();
    }

    private void OnTapped(object? sender, EventArgs e)
    {
        Clicked?.Invoke(this, e);
    }
}

