namespace interpreter.Maui.Services;

/// <summary>
/// Interface for animation operations following Interface Segregation Principle
/// </summary>
public interface IAnimationService
{
    Task AnimatePageLoadAsync(View languagePicker, View modePicker, View voiceTuneButton, View noiseButton);
    Task AnimateButtonPressAsync(View button, double scale = 0.95);
    Task AnimateToRecordingStateAsync(View initialStateLayout, View recordingStateLayout, View actionButton, View transcriptBorder, View chartBorder);
    Task AnimateToInitialStateAsync(View recordingStateLayout, View initialStateLayout, View languagePicker, View modePicker, View voiceTuneButton, View noiseButton);
    Task AnimatePulseAsync(View view, Func<bool> shouldContinue);
    Task AnimateFadeToggleAsync(View view, bool fadeIn);
}

