using System.Windows.Input;

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

    // New: Command for navigation / actions when used in a drawer
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SettingsMenuItem));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(SettingsMenuItem));

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

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
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
        // First, raise the Clicked event for backward compatibility
        Clicked?.Invoke(this, e);

        // Then execute the bound command if present
        if (Command is { } cmd && cmd.CanExecute(CommandParameter))
        {
            cmd.Execute(CommandParameter);
        }
    }
}