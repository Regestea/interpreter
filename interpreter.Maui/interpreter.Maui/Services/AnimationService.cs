namespace interpreter.Maui.Services;

/// <summary>
/// Handles all animation logic following Single Responsibility Principle
/// </summary>
public class AnimationService : IAnimationService
{
    // All animations are disabled for a snappy modern dark UI. Methods apply final states immediately.

    public Task AnimatePageLoadAsync(params View[] views)
    {
        foreach (var view in views)
        {
            view.Opacity = 1;
        }
        return Task.CompletedTask;
    }

    public Task AnimateButtonPressAsync(View button, double scale = 0.95)
    {
        // No visual scaling; immediate return
        return Task.CompletedTask;
    }

    public Task AnimateToRecordingStateAsync(
        View initialStateLayout,
        View recordingStateLayout,
        View actionButton,
        View transcriptBorder,
        View chartBorder)
    {
        initialStateLayout.Opacity = 0;
        initialStateLayout.IsVisible = false;
        recordingStateLayout.IsVisible = true;
        recordingStateLayout.Opacity = 1;
        transcriptBorder.Opacity = 1;
        chartBorder.Opacity = 1;
        return Task.CompletedTask;
    }

    public Task AnimateToInitialStateAsync(
        View recordingStateLayout,
        View initialStateLayout,
        params View[] viewsToShow)
    {
        recordingStateLayout.Opacity = 0;
        recordingStateLayout.IsVisible = false;
        initialStateLayout.IsVisible = true;
        initialStateLayout.Opacity = 1;
        foreach (var view in viewsToShow)
        {
            view.Opacity = 1;
        }
        return Task.CompletedTask;
    }

    public Task AnimatePulseAsync(View view, Func<bool> shouldContinue)
    {
        // Pulse disabled
        return Task.CompletedTask;
    }

    public Task AnimateFadeToggleAsync(View view, bool fadeIn)
    {
        view.IsVisible = fadeIn;
        view.Opacity = fadeIn ? 1 : 0;
        return Task.CompletedTask;
    }
}

