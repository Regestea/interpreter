namespace interpreter.Maui.Services;

/// <summary>
/// Manages button state changes following Single Responsibility Principle
/// </summary>
public class ButtonStateService : IButtonStateService
{
    private const string StopIcon = "⏹";
    private const string StartIcon = "▶";
    private const string StopText = "STOP";
    private const string StartText = "START";

    public void UpdateToStopState(Border actionButton, Label actionIcon, Label actionText)
    {
        actionButton.Background = CreateStopGradient();
        actionButton.Shadow = CreateShadow("#ff6b6b");
        actionIcon.Text = StopIcon;
        actionText.Text = StopText;
    }

    public void UpdateToStartState(Border actionButton, Label actionIcon, Label actionText)
    {
        actionButton.Background = CreateStartGradient();
        actionButton.Shadow = CreateShadow("#7cc8a5");
        actionIcon.Text = StartIcon;
        actionIcon.Margin = new Thickness(8, 0, 0, 0);
        actionText.Text = StartText;
    }

    private RadialGradientBrush CreateStopGradient()
    {
        return new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            Radius = 1.0,
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#ff6b6b"), Offset = 0.0f },
                new GradientStop { Color = Color.FromArgb("#ee5a52"), Offset = 1.0f }
            }
        };
    }

    private RadialGradientBrush CreateStartGradient()
    {
        return new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            Radius = 1.0,
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#7cc8a5"), Offset = 0.0f },
                new GradientStop { Color = Color.FromArgb("#5cb88d"), Offset = 1.0f }
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
}

