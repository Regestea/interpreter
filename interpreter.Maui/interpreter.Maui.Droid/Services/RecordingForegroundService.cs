using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using System.Threading;

namespace interpreter.Maui.Services;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
public class RecordingForegroundService : Service
{
    public const string ActionStart = "ACTION_START_RECORDING";
    public const string ActionStop = "ACTION_STOP_RECORDING";

    private AudioRecord? _audioRecord;
    private Thread? _recordingThread;
    private FileStream? _outputStream;
    private volatile bool _isRecordingInternal;
    private string? _filePath;
    
    private AudioRecordingConfiguration? _config;
    private IRecordingNotificationManager? _notificationManager;
    private IAudioRecorder? _audioRecorder;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // Initialize dependencies
        if (_config == null)
        {
            // Try to get configuration from DI, fallback to default
            try
            {
                var app = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Application as IPlatformApplication;
                var sp = app?.Services;
                _config = sp?.GetService<AudioRecordingConfiguration>() ?? new AudioRecordingConfiguration();
            }
            catch
            {
                _config = new AudioRecordingConfiguration();
            }
        }
        
        _notificationManager ??= new RecordingNotificationManager(this);
        _audioRecorder ??= new AudioRecorder(_config);
        
        var action = intent?.Action;
        if (action == ActionStart)
        {
            if (!RecordingState.IsRecording)
            {
                _notificationManager.StartForegroundNotification();
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

    private void StartRecording()
    {
        Directory.CreateDirectory(CacheDir.AbsolutePath);
        _filePath = Path.Combine(CacheDir.AbsolutePath, $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

#pragma warning disable CA1416
        // Create AudioRecord using the recorder
        _audioRecord = _audioRecorder!.CreateAudioRecord(out int minBufferSize);

        // Open output stream and write WAV header placeholder
        _outputStream = new FileStream(_filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        WavFileHandler.WriteWavHeader(_outputStream, _config.SampleRate, _config.ChannelCount, _config.BitsPerSample);

        _audioRecord.StartRecording();
        _isRecordingInternal = true;

        _recordingThread = new Thread(() =>
        {
            try
            {
                var buffer = new byte[minBufferSize];
                while (_isRecordingInternal)
                {
                    if (_audioRecord == null) break;
                    int read = _audioRecord.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        _outputStream?.Write(buffer, 0, read);
                    }
                }
            }
            catch (Exception)
            {
                // swallow; we'll finalize below
            }
        })
        {
            IsBackground = true,
            Name = "AudioRecordThread"
        };
        _recordingThread.Start();
#pragma warning restore CA1416
        RecordingState.IsRecording = true;
    }

    private void StopRecording()
    {
        try
        {
            _isRecordingInternal = false;
            // Join recording thread
            if (_recordingThread != null)
            {
                try { _recordingThread.Join(500); } catch { }
                _recordingThread = null;
            }

            if (_audioRecord != null)
            {
                try { _audioRecord.Stop(); } catch { }
                _audioRecord.Release();
                _audioRecord.Dispose();
                _audioRecord = null;
            }

            if (_outputStream != null)
            {
                // Finalize WAV header sizes
                try { WavFileHandler.UpdateWavHeader(_outputStream); } catch { }
                _outputStream.Flush();
                _outputStream.Dispose();
                _outputStream = null;
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
