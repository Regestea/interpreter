namespace interpreter.Maui
{
    public partial class MainPage : ContentPage
    {
        private bool isDarkTheme = false;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, EventArgs e)
        {
            // Animate initial state elements on page load with staggered timing
            await Task.WhenAll(
                Task.Delay(150).ContinueWith(async _ => await LanguagePickerBorder.FadeTo(1, 600, Easing.CubicOut)),
                Task.Delay(250).ContinueWith(async _ => await ModePickerBorder.FadeTo(1, 600, Easing.CubicOut)),
                Task.Delay(450).ContinueWith(async _ =>
                {
                    await Task.WhenAll(
                        VoiceTuneButton.FadeTo(1, 600, Easing.CubicOut),
                        VoiceTuneButton.TranslateTo(0, 0, 600, Easing.CubicOut)
                    );
                }),
                Task.Delay(500).ContinueWith(async _ =>
                {
                    await Task.WhenAll(
                        NoiseButton.FadeTo(1, 600, Easing.CubicOut),
                        NoiseButton.TranslateTo(0, 0, 600, Easing.CubicOut)
                    );
                })
            );
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            // Add button press animation
            await StartButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await StartButton.ScaleTo(1, 100, Easing.CubicOut);

            // Fade out initial state with animations
            await Task.WhenAll(
                InitialStateLayout.FadeTo(0, 400, Easing.CubicIn),
                StartButton.ScaleTo(0.8, 400, Easing.CubicIn)
            );

            InitialStateLayout.IsVisible = false;
            RecordingStateLayout.IsVisible = true;
            RecordingStateLayout.Opacity = 0;

            // Animate recording state elements
            await Task.WhenAll(
                RecordingStateLayout.FadeTo(1, 400, Easing.CubicOut),
                TranscriptBorder.TranslateTo(0, 0, 600, Easing.SpringOut).ContinueWith(_ =>
                    TranscriptBorder.FadeTo(1, 400, Easing.CubicOut)),
                Task.Delay(100).ContinueWith(async _ =>
                {
                    await Task.WhenAll(
                        StopButton.ScaleTo(1.1, 100, Easing.CubicOut),
                        StopButton.ScaleTo(1, 600, Easing.SpringOut)
                    );
                    // Pulse animation for stop button
                    _ = PulseStopButton();
                }),
                Task.Delay(200).ContinueWith(async _ =>
                    await ChartBorder.FadeTo(1, 600, Easing.CubicOut))
            );
        }

        private async Task PulseStopButton()
        {
            while (RecordingStateLayout.IsVisible)
            {
                await StopButton.ScaleTo(1.05, 1000, Easing.SinInOut);
                await StopButton.ScaleTo(1.0, 1000, Easing.SinInOut);
            }
        }

        private async void OnStopClicked(object sender, EventArgs e)
        {
            // Add button press animation
            await StopButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await StopButton.ScaleTo(1, 100, Easing.CubicOut);

            // Fade out recording state
            await RecordingStateLayout.FadeTo(0, 400, Easing.CubicIn);

            RecordingStateLayout.IsVisible = false;
            InitialStateLayout.IsVisible = true;
            InitialStateLayout.Opacity = 0;

            // Reset all elements
            VoiceTuneButton.Opacity = 0;
            VoiceTuneButton.TranslationY = 20;
            NoiseButton.Opacity = 0;
            NoiseButton.TranslationY = 20;
            LanguagePickerBorder.Opacity = 0;
            ModePickerBorder.Opacity = 0;

            // Animate initial state back in
            await Task.WhenAll(
                InitialStateLayout.FadeTo(1, 400, Easing.CubicOut),
                LanguagePickerBorder.FadeTo(1, 600, Easing.CubicOut),
                Task.Delay(100).ContinueWith(async _ => await ModePickerBorder.FadeTo(1, 600, Easing.CubicOut)),
                Task.Delay(300).ContinueWith(async _ =>
                {
                    await Task.WhenAll(
                        VoiceTuneButton.FadeTo(1, 600, Easing.CubicOut),
                        VoiceTuneButton.TranslateTo(0, 0, 600, Easing.CubicOut)
                    );
                }),
                Task.Delay(350).ContinueWith(async _ =>
                {
                    await Task.WhenAll(
                        NoiseButton.FadeTo(1, 600, Easing.CubicOut),
                        NoiseButton.TranslateTo(0, 0, 600, Easing.CubicOut)
                    );
                })
            );
        }

        private async void OnVoiceTuneClicked(object sender, EventArgs e)
        {
            // Add button press animation
            await VoiceTuneButton.ScaleTo(0.95, 100, Easing.CubicIn);
            await VoiceTuneButton.ScaleTo(1, 100, Easing.SpringOut);
            // Add your voice tune detection logic here
        }

        private async void OnNoiseAdjustClicked(object sender, EventArgs e)
        {
            // Add button press animation
            await NoiseButton.ScaleTo(0.95, 100, Easing.CubicIn);
            await NoiseButton.ScaleTo(1, 100, Easing.SpringOut);
            // Add your noise adjustment logic here
        }

        public async void ToggleMenu()
        {
            // Toggle menu flyout visibility
            if (MenuFlyout.IsVisible)
            {
                await MenuFlyout.FadeTo(0, 200, Easing.CubicIn);
                MenuFlyout.IsVisible = false;
            }
            else
            {
                MenuFlyout.IsVisible = true;
                await MenuFlyout.FadeTo(1, 200, Easing.CubicOut);
            }
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            ToggleMenu();
        }

        private async void OnThemeToggleClicked(object sender, EventArgs e)
        {
            // Animate button press
            var border = sender as Border;
            if (border != null)
            {
                await border.ScaleTo(0.95, 50, Easing.CubicIn);
                await border.ScaleTo(1, 50, Easing.CubicOut);
            }

            isDarkTheme = !isDarkTheme;
            ApplyTheme(isDarkTheme);
        }

        private void ApplyTheme(bool useDarkTheme)
        {
            if (useDarkTheme)
            {
                // Apply dark theme to entire app
                Application.Current.UserAppTheme = AppTheme.Dark;
                
                ThemeIcon.Text = "☀️";
                ThemeLabel.Text = "Light Theme";
                
                // Update background gradient
                var darkGradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                darkGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb("#1a1a2e"), Offset = 0.0f });
                darkGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb("#16213e"), Offset = 0.33f });
                darkGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb("#0f3460"), Offset = 0.66f });
                darkGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb("#1a1a2e"), Offset = 1.0f });
                this.Background = darkGradient;

                // Update main border to dark glass effect
                var darkGlassBrush = Application.Current.Resources["GlassSurfaceBrushDark"] as LinearGradientBrush;
                MainBorder.Background = darkGlassBrush;
                MainBorder.Stroke = Color.FromArgb("#50FFFFFF");

                // Update menu flyout
                MenuFlyout.BackgroundColor = Color.FromArgb("#40FFFFFF");

                // Update all card backgrounds
                UpdateCardBackgrounds(Color.FromArgb("#30FFFFFF"));
                
                // Update text colors
                UpdateTextColors(Color.FromArgb("#E0E0E0"));
            }
            else
            {
                // Apply light theme to entire app
                Application.Current.UserAppTheme = AppTheme.Light;
                
                ThemeIcon.Text = "🌙";
                ThemeLabel.Text = "Dark Theme";
                
                // Restore original background gradient
                var lightGradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                lightGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb("#C8B5F0"), Offset = 0.0f });
                lightGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb("#B5E8D9"), Offset = 0.33f });
                lightGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb("#FFCFB5"), Offset = 0.66f });
                lightGradient.GradientStops.Add(new GradientStop { Color = Color.FromArgb("#B5DBF0"), Offset = 1.0f });
                this.Background = lightGradient;

                // Restore main border to light glass effect
                var lightGlassBrush = Application.Current.Resources["GlassSurfaceBrush"] as LinearGradientBrush;
                MainBorder.Background = lightGlassBrush;
                MainBorder.Stroke = Color.FromArgb("#30FFFFFF");

                // Restore menu flyout
                var glassWhite = Application.Current.Resources["GlassWhite"] as Color;
                MenuFlyout.BackgroundColor = glassWhite;

                // Restore card backgrounds
                UpdateCardBackgrounds(glassWhite ?? Color.FromArgb("#D0FFFFFF"));
                
                // Restore text colors
                var textDark = Application.Current.Resources["AppTextDark"] as Color;
                UpdateTextColors(textDark ?? Color.FromArgb("#2C3E50"));
            }
        }

        private void UpdateCardBackgrounds(Color backgroundColor)
        {
            // Update all border card backgrounds
            LanguagePickerBorder.BackgroundColor = backgroundColor;
            ModePickerBorder.BackgroundColor = backgroundColor;
            VoiceTuneButton.BackgroundColor = backgroundColor;
            NoiseButton.BackgroundColor = backgroundColor;
            TranscriptBorder.BackgroundColor = backgroundColor;
            ChartBorder.BackgroundColor = backgroundColor;
        }

        private void UpdateTextColors(Color textColor)
        {
            // Update primary text elements
            TranscriptLabel.TextColor = textColor;
        }
    }

    public class FrequencyChartDrawable : IDrawable
    {
        // Default data points for two lines
        private readonly float[] orangeLineData = { 0.3f, 0.5f, 0.7f, 0.6f, 0.8f, 0.7f, 0.5f, 0.6f, 0.8f, 0.9f, 0.7f, 0.6f, 0.8f, 0.7f, 0.5f, 0.6f, 0.7f, 0.5f, 0.4f, 0.3f };
        private readonly float[] greenLineData = { 0.4f, 0.3f, 0.5f, 0.7f, 0.6f, 0.8f, 0.9f, 0.7f, 0.6f, 0.5f, 0.6f, 0.8f, 0.7f, 0.6f, 0.7f, 0.8f, 0.6f, 0.5f, 0.3f, 0.2f };

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width == 0 || dirtyRect.Height == 0)
                return;

            float width = dirtyRect.Width;
            float height = dirtyRect.Height;
            float padding = 10;

            // Draw cyan line
            DrawLine(canvas, orangeLineData, width, height, padding, Color.FromArgb("#72c9e4"));

            // Draw mint green line
            DrawLine(canvas, greenLineData, width, height, padding, Color.FromArgb("#7cc8a5"));
        }

        private void DrawLine(ICanvas canvas, float[] data, float width, float height, float padding, Color color)
        {
            if (data.Length < 2)
                return;

            float availableWidth = width - (2 * padding);
            float availableHeight = height - (2 * padding);
            float xStep = availableWidth / (data.Length - 1);

            PathF path = new PathF();

            // Start at first point
            float x = padding;
            float y = height - padding - (data[0] * availableHeight);
            path.MoveTo(x, y);

            // Draw line through all points
            for (int i = 1; i < data.Length; i++)
            {
                x = padding + (i * xStep);
                y = height - padding - (data[i] * availableHeight);
                path.LineTo(x, y);
            }

            canvas.StrokeColor = color;
            canvas.StrokeSize = 3;
            canvas.DrawPath(path);

            // Draw points on the line
            for (int i = 0; i < data.Length; i++)
            {
                x = padding + (i * xStep);
                y = height - padding - (data[i] * availableHeight);
                canvas.FillColor = color;
                canvas.FillCircle(x, y, 3);
            }
        }
    }

}
