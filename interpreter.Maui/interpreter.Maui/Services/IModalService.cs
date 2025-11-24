namespace interpreter.Maui.Services
{
    /// <summary>
    /// Service for displaying modal dialogs with customizable options
    /// </summary>
    public interface IModalService
    {
        /// <summary>
        /// Shows a modal with the specified content and options
        /// </summary>
        /// <param name="content">The content view to display in the modal</param>
        /// <param name="options">Modal display options</param>
        Task ShowModalAsync(View content, ModalOptions? options = null);

        /// <summary>
        /// Closes the currently displayed modal
        /// </summary>
        Task CloseModalAsync();

        /// <summary>
        /// Gets whether a modal is currently displayed
        /// </summary>
        bool IsModalVisible { get; }
    }

    /// <summary>
    /// Configuration options for modal display
    /// </summary>
    public class ModalOptions
    {
        /// <summary>
        /// Whether to show a close button
        /// </summary>
        public bool ShowCloseButton { get; set; } = true;

        /// <summary>
        /// Auto-close duration in seconds (null = no auto-close)
        /// </summary>
        public int? AutoCloseDurationSeconds { get; set; } = null;

        /// <summary>
        /// Whether clicking outside the modal closes it
        /// </summary>
        public bool CloseOnBackgroundTap { get; set; } = true;

        /// <summary>
        /// Custom background color for the modal overlay
        /// </summary>
        public Color? OverlayColor { get; set; } = null;

        /// <summary>
        /// Custom background color for the modal content
        /// </summary>
        public Color? ContentBackgroundColor { get; set; } = null;

        /// <summary>
        /// Custom color for the close button (X)
        /// </summary>
        public Color? CloseButtonColor { get; set; } = null;
    }
}
