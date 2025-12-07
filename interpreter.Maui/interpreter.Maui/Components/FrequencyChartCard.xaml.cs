namespace interpreter.Maui.Components;

/// <summary>
/// A card component for displaying frequency chart visualization.
/// </summary>
public partial class FrequencyChartCard : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(FrequencyChartCard), "📊 Voice Frequency");

    public static readonly BindableProperty TitleColorProperty =
        BindableProperty.Create(nameof(TitleColor), typeof(Color), typeof(FrequencyChartCard), Color.FromArgb("#7cc8a5"));

    public static readonly BindableProperty CardBackgroundColorProperty =
        BindableProperty.Create(nameof(CardBackgroundColor), typeof(Color), typeof(FrequencyChartCard), Color.FromArgb("#18202B"));

    public static readonly BindableProperty CardStrokeProperty =
        BindableProperty.Create(nameof(CardStroke), typeof(Color), typeof(FrequencyChartCard), Color.FromArgb("#40FFFFFF"));

    public static readonly BindableProperty ChartDrawableProperty =
        BindableProperty.Create(nameof(ChartDrawable), typeof(IDrawable), typeof(FrequencyChartCard), null);

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

    public IDrawable ChartDrawable
    {
        get => (IDrawable)GetValue(ChartDrawableProperty);
        set => SetValue(ChartDrawableProperty, value);
    }

    /// <summary>
    /// Gets the chart view for external access
    /// </summary>
    public GraphicsView ChartViewElement => ChartView;

    public FrequencyChartCard()
    {
        InitializeComponent();
    }
}

