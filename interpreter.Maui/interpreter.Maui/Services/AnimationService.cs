namespace interpreter.Maui.Services;

/// <summary>
/// Handles all animation logic following Single Responsibility Principle
/// </summary>
public class AnimationService : IAnimationService
{
    private const uint ShortDuration = 100;
    private const uint MediumDuration = 400;
    private const uint LongDuration = 600;
    private const uint PulseDuration = 1000;
    
    private const double ButtonPressScale = 0.9;
    private const double NormalScale = 1.0;
    private const double EnlargedScale = 1.1;
    private const double RecordingScale = 0.8;
    private const double PulseScale = 1.05;

    public async Task AnimatePageLoadAsync(View languagePicker, View modePicker, View voiceTuneButton, View noiseButton)
    {
        await Task.WhenAll(
            Task.Delay(150).ContinueWith(async _ => await FadeInAsync(languagePicker, LongDuration)),
            Task.Delay(250).ContinueWith(async _ => await FadeInAsync(modePicker, LongDuration))
        );
        
        voiceTuneButton.Opacity = 1;
        noiseButton.Opacity = 1;
    }

    public async Task AnimateButtonPressAsync(View button, double scale = 0.95)
    {
        await ScaleSequenceAsync(button, scale, NormalScale);
    }

    public async Task AnimateToRecordingStateAsync(
        View initialStateLayout, 
        View recordingStateLayout, 
        View actionButton, 
        View transcriptBorder, 
        View chartBorder)
    {
        // Initial button press feedback
        await ScaleSequenceAsync(actionButton, ButtonPressScale, NormalScale);

        // Fade out initial state
        await Task.WhenAll(
            FadeOutAsync(initialStateLayout, MediumDuration),
            ScaleToAsync(actionButton, RecordingScale, MediumDuration, Easing.CubicIn)
        );

        initialStateLayout.IsVisible = false;
        recordingStateLayout.IsVisible = true;
        recordingStateLayout.Opacity = 0;

        // Animate recording state elements
        await Task.WhenAll(
            FadeInAsync(recordingStateLayout, MediumDuration),
            SlideAndFadeInAsync(transcriptBorder, LongDuration),
            AnimateActionButtonEntranceAsync(actionButton),
            Task.Delay(200).ContinueWith(async _ => await FadeInAsync(chartBorder, LongDuration))
        );
    }

    public async Task AnimateToInitialStateAsync(
        View recordingStateLayout, 
        View initialStateLayout, 
        View languagePicker, 
        View modePicker, 
        View voiceTuneButton, 
        View noiseButton)
    {
        // Fade out recording state
        await FadeOutAsync(recordingStateLayout, MediumDuration);

        recordingStateLayout.IsVisible = false;
        initialStateLayout.IsVisible = true;
        initialStateLayout.Opacity = 0;

        // Reset pickers
        languagePicker.Opacity = 0;
        modePicker.Opacity = 0;

        // Animate initial state back in
        await Task.WhenAll(
            FadeInAsync(initialStateLayout, MediumDuration),
            FadeInAsync(languagePicker, LongDuration),
            Task.Delay(100).ContinueWith(async _ => await FadeInAsync(modePicker, LongDuration))
        );
        
        voiceTuneButton.Opacity = 1;
        noiseButton.Opacity = 1;
    }

    public async Task AnimatePulseAsync(View view, Func<bool> shouldContinue)
    {
        while (shouldContinue())
        {
            await ScaleToAsync(view, PulseScale, PulseDuration, Easing.SinInOut);
            await ScaleToAsync(view, NormalScale, PulseDuration, Easing.SinInOut);
        }
    }

    public async Task AnimateFadeToggleAsync(View view, bool fadeIn)
    {
        if (fadeIn)
        {
            view.IsVisible = true;
            await FadeInAsync(view, 200);
        }
        else
        {
            await FadeOutAsync(view, 200);
            view.IsVisible = false;
        }
    }

    #region Private Helper Methods

    private async Task FadeInAsync(View view, uint duration)
    {
        await view.FadeTo(1, duration, Easing.CubicOut);
    }

    private async Task FadeOutAsync(View view, uint duration)
    {
        await view.FadeTo(0, duration, Easing.CubicIn);
    }

    private async Task ScaleToAsync(View view, double scale, uint duration, Easing easing)
    {
        await view.ScaleTo(scale, duration, easing);
    }

    private async Task ScaleSequenceAsync(View view, double firstScale, double secondScale)
    {
        await ScaleToAsync(view, firstScale, ShortDuration, Easing.CubicIn);
        await ScaleToAsync(view, secondScale, ShortDuration, Easing.SpringOut);
    }

    private async Task SlideAndFadeInAsync(View view, uint duration)
    {
        await Task.WhenAll(
            view.TranslateTo(0, 0, duration, Easing.SpringOut),
            FadeInAsync(view, MediumDuration)
        );
    }

    private async Task AnimateActionButtonEntranceAsync(View actionButton)
    {
        await Task.Delay(100);
        await ScaleToAsync(actionButton, EnlargedScale, ShortDuration, Easing.CubicOut);
        await ScaleToAsync(actionButton, NormalScale, LongDuration, Easing.SpringOut);
    }

    #endregion
}

