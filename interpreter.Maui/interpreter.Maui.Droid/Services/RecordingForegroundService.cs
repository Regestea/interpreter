using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;

namespace interpreter.Maui.Services;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
public class RecordingForegroundService : Service
{
    public const string ActionStart = "ACTION_START_RECORDING";
    public const string ActionStop = "ACTION_STOP_RECORDING";
    private const int NotificationId = 1001;
    private const string ChannelId = "recording_channel";

    private MediaRecorder? _recorder;
    private string? _filePath;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action;
        if (action == ActionStart)
        {
            if (!RecordingState.IsRecording)
            {
                StartInForeground();
                StartRecording();
            }
        }
        else if (action == ActionStop)
        {
            StopRecording();
            StopForeground(true);
            StopSelf();
        }
        return StartCommandResult.Sticky;
    }

    private void StartInForeground()
    {
        var mgr = (NotificationManager?)GetSystemService(NotificationService);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Recording", NotificationImportance.Low)
            {
                Description = "Voice recording in progress"
            };
            mgr?.CreateNotificationChannel(channel);
        }

        // PendingIntent to stop recording
        var stopIntent = new Intent(this, typeof(RecordingStopReceiver));
        var pendingStop = PendingIntent.GetBroadcast(this, 0, stopIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Recording audio")
            .SetContentText("Tap Stop to finish and play")
            .SetSmallIcon((int)Android.Resource.Drawable.IcMediaPlay)
            .SetOngoing(true)
            .AddAction(new NotificationCompat.Action(0, "Stop", pendingStop));

        StartForeground(NotificationId, builder.Build());
    }

    private void StartRecording()
    {
        Directory.CreateDirectory(CacheDir.AbsolutePath);
        _filePath = Path.Combine(CacheDir.AbsolutePath, $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.m4a");

        _recorder = new MediaRecorder();
#pragma warning disable CA1416
        _recorder.SetAudioSource(AudioSource.Mic);
        _recorder.SetOutputFormat(OutputFormat.Mpeg4);
        _recorder.SetAudioEncoder(AudioEncoder.Aac);
        _recorder.SetAudioSamplingRate(44100);
        _recorder.SetAudioEncodingBitRate(96000);
        _recorder.SetOutputFile(_filePath);
        _recorder.Prepare();
        _recorder.Start();
#pragma warning restore CA1416
        RecordingState.IsRecording = true;
    }

    private void StopRecording()
    {
        try
        {
            if (_recorder != null)
            {
                _recorder.Stop();
                _recorder.Release();
                _recorder.Dispose();
                _recorder = null;
            }
        }
        catch (Exception) { }
        finally
        {
            RecordingState.IsRecording = false;
            RecordingState.LastFilePath = _filePath;
        }
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public class RecordingStopReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;
        var serviceIntent = new Intent(context, typeof(RecordingForegroundService));
        serviceIntent.SetAction(RecordingForegroundService.ActionStop);
        context.StartService(serviceIntent);
    }
}
