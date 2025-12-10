namespace interpreter.Maui.Services;

/// <summary>
/// Manages button state changes following Single Responsibility Principle
/// </summary>
public class ButtonStateService : IButtonStateService
{
    private const string StopText = "STOP";
    private const string StartText = "START";

    public void UpdateToStopState(Border actionButton, Label actionText)
    {
        // Match the start styling but with a red palette for stop state
        actionButton.Background = CreateStopGradient();
        actionButton.Shadow = CreateStopShadow();
        actionText.Text = StopText;
    }

    public void UpdateToStartState(Border actionButton, Label actionText)
    {
        // Restore the original start styling so the button matches the XAML design
        actionButton.Background = CreateOriginalStartGradient();
        actionButton.Shadow = CreateOriginalShadow();
        actionText.Text = StartText;
    }

    private RadialGradientBrush CreateStopGradient()
    {
        return new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            Radius = 0.9,
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#F97373"), Offset = 0.0f },
                new GradientStop { Color = Color.FromArgb("#EF4444"), Offset = 0.35f },
                new GradientStop { Color = Color.FromArgb("#7F1D1D"), Offset = 1.0f }
            }
        };
    }

    private RadialGradientBrush CreateOriginalStartGradient()
    {
        return new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            Radius = 0.9,
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#22C7B8"), Offset = 0.0f },
                new GradientStop { Color = Color.FromArgb("#0F766E"), Offset = 0.4f },
                new GradientStop { Color = Color.FromArgb("#022C22"), Offset = 1.0f }
            }
        };
    }

    private Shadow CreateShadow(string color)
    {
        return new Shadow
        {
            Brush = Color.FromArgb(color),
            Offset = new Point(0, 12),
            Radius = 35,
            Opacity = 0.5f
        };
    }

    private Shadow CreateStopShadow()
    {
        return new Shadow
        {
            Brush = Color.FromArgb("#66EF4444"),
            Offset = new Point(0, 14),
            Radius = 26,
            Opacity = 0.55f
        };
    }

    private Shadow CreateOriginalShadow()
    {
        return new Shadow
        {
            Brush = Color.FromArgb("#66000000"),
            Offset = new Point(0, 14),
            Radius = 26,
            Opacity = 0.55f
        };
    }
}

