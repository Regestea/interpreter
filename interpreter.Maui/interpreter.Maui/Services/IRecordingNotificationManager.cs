namespace interpreter.Maui.Services;

/// <summary>
/// Interface for managing the creation and display of recording notifications.
/// </summary>
public interface IRecordingNotificationManager
{
    /// <summary>
    /// Creates and starts the foreground service notification.
    /// </summary>
    void StartForegroundNotification();
}

