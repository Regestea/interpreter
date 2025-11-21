namespace interpreter.Maui.Services;

/// <summary>
/// Handles theme switching logic following Single Responsibility Principle
/// </summary>
public class ThemeService : IThemeService
{
    public void ApplyTheme(bool isDarkTheme, ContentPage page, ThemeElements elements)
    {
        if (isDarkTheme)
        {
            ApplyDarkTheme(page, elements);
        }
        else
        {
            ApplyLightTheme(page, elements);
        }
    }

    private void ApplyDarkTheme(ContentPage page, ThemeElements elements)
    {
        Application.Current.UserAppTheme = AppTheme.Dark;
        
        elements.ThemeIcon.Text = "☀️";
        elements.ThemeLabel.Text = "Light Theme";
        
        page.Background = CreateDarkGradient();
        
        var darkGlassBrush = GetResource<LinearGradientBrush>("GlassSurfaceBrushDark");
        elements.MainBorder.Background = darkGlassBrush;
        elements.MainBorder.Stroke = Color.FromArgb("#50FFFFFF");
        
        elements.MenuFlyout.BackgroundColor = Color.FromArgb("#40FFFFFF");
        
        UpdateCardBackgrounds(elements, Color.FromArgb("#30FFFFFF"));
        UpdateTextColors(elements, Color.FromArgb("#E0E0E0"));
    }

    private void ApplyLightTheme(ContentPage page, ThemeElements elements)
    {
        Application.Current.UserAppTheme = AppTheme.Light;
        
        elements.ThemeIcon.Text = "🌙";
        elements.ThemeLabel.Text = "Dark Theme";
        
        page.Background = CreateLightGradient();
        
        var lightGlassBrush = GetResource<LinearGradientBrush>("GlassSurfaceBrush");
        elements.MainBorder.Background = lightGlassBrush;
        elements.MainBorder.Stroke = Color.FromArgb("#30FFFFFF");
        
        var glassWhite = GetResource<Color>("GlassWhite") ?? Color.FromArgb("#D0FFFFFF");
        elements.MenuFlyout.BackgroundColor = glassWhite;
        
        UpdateCardBackgrounds(elements, glassWhite);
        
        var textDark = GetResource<Color>("AppTextDark") ?? Color.FromArgb("#2C3E50");
        UpdateTextColors(elements, textDark);
    }

    private LinearGradientBrush CreateDarkGradient()
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#1a1a2e"), Offset = 0.0f },
                new GradientStop { Color = Color.FromArgb("#16213e"), Offset = 0.33f },
                new GradientStop { Color = Color.FromArgb("#0f3460"), Offset = 0.66f },
                new GradientStop { Color = Color.FromArgb("#1a1a2e"), Offset = 1.0f }
            }
        };
    }

    private LinearGradientBrush CreateLightGradient()
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#C8B5F0"), Offset = 0.0f },
                new GradientStop { Color = Color.FromArgb("#B5E8D9"), Offset = 0.33f },
                new GradientStop { Color = Color.FromArgb("#FFCFB5"), Offset = 0.66f },
                new GradientStop { Color = Color.FromArgb("#B5DBF0"), Offset = 1.0f }
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

