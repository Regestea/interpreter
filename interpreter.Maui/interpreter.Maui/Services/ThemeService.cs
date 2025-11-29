namespace interpreter.Maui.Services;

/// <summary>
/// Handles theme switching logic following Single Responsibility Principle
/// </summary>
public class ThemeService : IThemeService
{
    public void ApplyTheme(bool isDarkTheme, ContentPage page, ThemeElements elements)
    {
        // Force modern dark theme regardless of input flag
        ApplyDarkTheme(page, elements);
    }

    private void ApplyDarkTheme(ContentPage page, ThemeElements elements)
    {
        Application.Current.UserAppTheme = AppTheme.Dark;
        
        elements.ThemeIcon.Text = "🌙";
        elements.ThemeLabel.Text = "Dark Mode";
        
        page.Background = CreateDarkGradient();
        
        var darkGlassBrush = GetResource<LinearGradientBrush>("GlassSurfaceBrushDark");
        elements.MainBorder.Background = darkGlassBrush;
        elements.MainBorder.Stroke = Color.FromArgb("#50FFFFFF");
        
        var darkSurface = GetResource<Color>("DarkSurface") ?? Color.FromArgb("#121821");
        elements.MenuFlyout.BackgroundColor = darkSurface;
        
        var darkCard = GetResource<Color>("DarkCardBackground") ?? Color.FromArgb("#18202B");
        UpdateCardBackgrounds(elements, darkCard);
        var textPrimary = GetResource<Color>("DarkTextPrimary") ?? Color.FromArgb("#E6EDF3");
        UpdateTextColors(elements, textPrimary);
    }

    private LinearGradientBrush CreateDarkGradient()
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#0B0F14"), Offset = 0.0f },
                new GradientStop { Color = Color.FromArgb("#0C1220"), Offset = 0.5f },
                new GradientStop { Color = Color.FromArgb("#0B0F14"), Offset = 1.0f }
            }
        };
    }

    private void UpdateCardBackgrounds(ThemeElements elements, Color backgroundColor)
    {
        elements.LanguagePickerBorder.BackgroundColor = backgroundColor;
        elements.ModePickerBorder.BackgroundColor = backgroundColor;
        elements.VoiceTuneButton.BackgroundColor = backgroundColor;
        elements.NoiseButton.BackgroundColor = backgroundColor;
        elements.TranscriptBorder.BackgroundColor = backgroundColor;
        elements.ChartBorder.BackgroundColor = backgroundColor;
    }

    private void UpdateTextColors(ThemeElements elements, Color textColor)
    {
        elements.TranscriptLabel.TextColor = textColor;
    }

    private T GetResource<T>(string key) where T : class
    {
        if (Application.Current.Resources.TryGetValue(key, out var resource))
        {
            return resource as T;
        }
        return null;
    }
}

