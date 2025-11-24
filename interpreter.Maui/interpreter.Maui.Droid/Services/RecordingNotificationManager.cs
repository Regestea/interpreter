using System;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace interpreter.Maui.Services;

/// <summary>
/// Manages the creation and display of recording notifications.
/// </summary>
public class RecordingNotificationManager : IRecordingNotificationManager
{
    private const int NotificationId = 1001;
    private const string ChannelId = "recording_channel";
    private readonly Service _service;

    public RecordingNotificationManager(Service service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Creates and starts the foreground service notification.
    /// </summary>
    public void StartForegroundNotification()
    {
        var mgr = (NotificationManager?)_service.GetSystemService(Context.NotificationService);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Recording", NotificationImportance.Low)
            {
                Description = "Voice recording in progress"
            };
            mgr?.CreateNotificationChannel(channel);
        }

        // PendingIntent to stop recording
        var stopIntent = new Intent(_service, typeof(RecordingStopReceiver));
        var pendingStop = PendingIntent.GetBroadcast(_service, 0, stopIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(_service, ChannelId)
            .SetContentTitle("Recording audio")
            .SetContentText("Tap Stop to finish and play")
            .SetSmallIcon(Android.Resource.Drawable.IcMediaPlay)
            .SetOngoing(true)
            .AddAction(new NotificationCompat.Action(0, "Stop", pendingStop));

        _service.StartForeground(NotificationId, builder.Build());
    }
}

