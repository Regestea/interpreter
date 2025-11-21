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
    private const int NotificationId = 1001;
    private const string ChannelId = "recording_channel";

    // Switched from MediaRecorder to AudioRecord (raw PCM -> WAV)
    private AudioRecord? _audioRecord;
    private Thread? _recordingThread;
    private FileStream? _outputStream;
    private volatile bool _isRecordingInternal;
    private int _sampleRate = 44100;
    private int _channelCount = 1; // Mono
    private int _bitsPerSample = 16;
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
        _filePath = Path.Combine(CacheDir.AbsolutePath, $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

#pragma warning disable CA1416
        // Configure AudioRecord
        var channelConfig = ChannelIn.Mono;
        var audioFormat = Encoding.Pcm16bit;
        int minBufferSize = AudioRecord.GetMinBufferSize(_sampleRate, channelConfig, audioFormat);
        if (minBufferSize <= 0)
            minBufferSize = _sampleRate * _channelCount * (_bitsPerSample / 8); // 1 second fallback

        _audioRecord = new AudioRecord(
            AudioSource.Mic,
            _sampleRate,
            channelConfig,
            audioFormat,
            minBufferSize
        );

        // Open output stream and write WAV header placeholder
        _outputStream = new FileStream(_filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        WriteWavHeader(_outputStream, _sampleRate, _channelCount, _bitsPerSample);

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
                try { UpdateWavHeader(_outputStream); } catch { }
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

    // WAV helpers
    private static void WriteWavHeader(System.IO.Stream stream, int sampleRate, int channels, int bitsPerSample)
    {
        // RIFF header with placeholder sizes
        using var bw = new System.IO.BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);

        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(0); // ChunkSize placeholder
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // Subchunk1Size for PCM
        bw.Write((short)1); // PCM format
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write(blockAlign);
        bw.Write((short)bitsPerSample);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(0); // Subchunk2Size placeholder
    }

    private static void UpdateWavHeader(System.IO.Stream stream)
    {
        long fileSize = stream.Length;
        if (fileSize < 44) return;
        using var bw = new System.IO.BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        long dataSize = fileSize - 44;
        // ChunkSize = 36 + Subchunk2Size
        bw.Seek(4, SeekOrigin.Begin);
        bw.Write((int)(36 + dataSize));
        // Subchunk2Size
        bw.Seek(40, SeekOrigin.Begin);
        bw.Write((int)dataSize);
        bw.Flush();
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
