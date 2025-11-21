namespace interpreter.Maui.Services;

/// <summary>
/// Interface for theme operations following Interface Segregation Principle
/// </summary>
public interface IThemeService
{
    void ApplyTheme(bool isDarkTheme, ContentPage page, ThemeElements elements);
}

/// <summary>
/// Data Transfer Object for theme elements
/// </summary>
public class ThemeElements
{
    public Border MainBorder { get; set; }
    public Border MenuFlyout { get; set; }
    public Label ThemeIcon { get; set; }
    public Label ThemeLabel { get; set; }
    public Border LanguagePickerBorder { get; set; }
    public Border ModePickerBorder { get; set; }
    public Border VoiceTuneButton { get; set; }
    public Border NoiseButton { get; set; }
    public Border TranscriptBorder { get; set; }
    public Border ChartBorder { get; set; }
    public Label TranscriptLabel { get; set; }
}

