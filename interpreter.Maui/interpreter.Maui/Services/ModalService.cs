using System.Timers;

namespace interpreter.Maui.Services
{
    /// <summary>
    /// Implementation of modal service with auto-close and animation support
    /// </summary>
    public class ModalService : IModalService
    {
        private Grid? _modalContainer;
        private Border? _modalContentBorder;
        private System.Timers.Timer? _autoCloseTimer;
        private TaskCompletionSource<bool>? _closeTaskCompletionSource;

        public bool IsModalVisible { get; private set; }

        /// <summary>
        /// Initialize the modal service with a container grid
        /// </summary>
        public void Initialize(Grid modalContainer, Border modalContentBorder)
        {
            _modalContainer = modalContainer ?? throw new ArgumentNullException(nameof(modalContainer));
            _modalContentBorder = modalContentBorder ?? throw new ArgumentNullException(nameof(modalContentBorder));
        }

        public async Task ShowModalAsync(View content, ModalOptions? options = null)
        {
            if (_modalContainer == null || _modalContentBorder == null)
            {
                throw new InvalidOperationException("ModalService must be initialized before use");
            }

            if (IsModalVisible)
            {
                await CloseModalAsync();
            }

            options ??= new ModalOptions();

            // Setup modal content
            var modalContent = CreateModalContent(content, options);
            _modalContentBorder.Content = modalContent;

            // Apply background colors if specified
            if (options.OverlayColor != null)
            {
                _modalContainer.BackgroundColor = options.OverlayColor;
            }

            if (options.ContentBackgroundColor != null)
            {
                _modalContentBorder.BackgroundColor = options.ContentBackgroundColor;
            }

            // Show modal with animation
            _modalContainer.IsVisible = true;
            IsModalVisible = true;

            await Task.WhenAll(
                _modalContainer.FadeTo(1, 250, Easing.CubicOut),
                _modalContentBorder.ScaleTo(1, 300, Easing.SpringOut)
            );

            // Setup auto-close timer if specified
            if (options.AutoCloseDurationSeconds.HasValue && options.AutoCloseDurationSeconds.Value > 0)
            {
                StartAutoCloseTimer(options.AutoCloseDurationSeconds.Value);
            }
        }

        public async Task CloseModalAsync()
        {
            if (_modalContainer == null || _modalContentBorder == null || !IsModalVisible)
            {
                return;
            }

            // Stop auto-close timer if running
            StopAutoCloseTimer();

            // Animate out
            await Task.WhenAll(
                _modalContainer.FadeTo(0, 200, Easing.CubicIn),
                _modalContentBorder.ScaleTo(0.9, 200, Easing.CubicIn)
            );

            // Hide and reset
            _modalContainer.IsVisible = false;
            _modalContentBorder.Content = null;
            _modalContentBorder.Scale = 0.9;
            IsModalVisible = false;

            _closeTaskCompletionSource?.TrySetResult(true);
            _closeTaskCompletionSource = null;
        }

        private View CreateModalContent(View content, ModalOptions options)
        {
            var container = new VerticalStackLayout
            {
                Spacing = 0
            };

            // Add close button if needed
            if (options.ShowCloseButton)
            {
                var closeButton = new Border
                {
                    BackgroundColor = Colors.Transparent,
                    StrokeThickness = 0,
                    Padding = new Thickness(12),
                    HorizontalOptions = LayoutOptions.End,
                    GestureRecognizers =
                    {
                        new TapGestureRecognizer
                        {
                            Command = new Command(async () => await CloseModalAsync())
                        }
                    },
                    Content = new Label
                    {
                        Text = "✕",
                        FontSize = 24,
                        TextColor = options.CloseButtonColor ?? Color.FromArgb("#666666"),
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };

                container.Add(closeButton);
            }

            // Add the actual content
            container.Add(content);

            return container;
        }

        private void StartAutoCloseTimer(int durationSeconds)
        {
            StopAutoCloseTimer();

            _autoCloseTimer = new System.Timers.Timer(durationSeconds * 1000);
            _autoCloseTimer.Elapsed += async (sender, e) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await CloseModalAsync();
                });
            };
            _autoCloseTimer.AutoReset = false;
            _autoCloseTimer.Start();
        }

        private void StopAutoCloseTimer()
        {
            if (_autoCloseTimer != null)
            {
                _autoCloseTimer.Stop();
                _autoCloseTimer.Dispose();
                _autoCloseTimer = null;
            }
        }
    }
}

