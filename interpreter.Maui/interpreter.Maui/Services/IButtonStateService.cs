namespace interpreter.Maui.Services;

/// <summary>
/// Interface for button state management following Interface Segregation Principle
/// </summary>
public interface IButtonStateService
{
    void UpdateToStopState(Border actionButton, Label actionText);
    void UpdateToStartState(Border actionButton, Label actionText);
}

