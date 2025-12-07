using System.Collections;

namespace interpreter.Maui.Components;

/// <summary>
/// A card component with a title label and picker.
/// Used for Language and Mode selection.
/// </summary>
public partial class PickerCard : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(PickerCard), "🌍 Title");

    public static readonly BindableProperty TitleColorProperty =
        BindableProperty.Create(nameof(TitleColor), typeof(Color), typeof(PickerCard), Color.FromArgb("#BB86FC"));

    public static readonly BindableProperty PickerTitleProperty =
        BindableProperty.Create(nameof(PickerTitle), typeof(string), typeof(PickerCard), "Select");

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(PickerCard), null);

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(PickerCard), -1, BindingMode.TwoWay);

    public static readonly BindableProperty CardBackgroundColorProperty =
        BindableProperty.Create(nameof(CardBackgroundColor), typeof(Color), typeof(PickerCard), Color.FromArgb("#18202B"));

    public static readonly BindableProperty CardStrokeProperty =
        BindableProperty.Create(nameof(CardStroke), typeof(Color), typeof(PickerCard), Color.FromArgb("#40FFFFFF"));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    public string PickerTitle
    {
        get => (string)GetValue(PickerTitleProperty);
        set => SetValue(PickerTitleProperty, value);
    }

    public IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
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

    public event EventHandler<EventArgs>? SelectionChanged;

    public PickerCard()
    {
        InitializeComponent();
    }

    private void OnSelectedIndexChanged(object? sender, EventArgs e)
    {
        SelectionChanged?.Invoke(this, e);
    }
}

