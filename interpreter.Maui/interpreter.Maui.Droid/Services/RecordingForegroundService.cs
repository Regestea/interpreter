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
    private AudioCalibration? _calibration;
    private VoiceActivityDetector? _vad;
    private AudioChunkBuffer? _chunkBuffer;
    private int _chunkCounter;
    private bool _hasDetectedSpeech;
    

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // Initialize dependencies
        var app = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Application as IPlatformApplication;
        var sp = app?.Services;
        
        if (_config == null)
        {
            // Try to get configuration from DI, fallback to default
            try
            {
               
                _config = sp?.GetService<AudioRecordingConfiguration>() ?? new AudioRecordingConfiguration();
                _calibration = sp?.GetService<AudioCalibration>() ?? new AudioCalibration();
            }
            catch
            {
                _config = new AudioRecordingConfiguration();
                _calibration = new AudioCalibration();
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
            
            // Finalize any remaining audio chunk
            FinalizeRemainingChunk();
            
            StopForeground(true);
            StopSelf();
        }
        return StartCommandResult.Sticky;
    }

    private async Task StartRecording()
    {
        Directory.CreateDirectory(CacheDir.AbsolutePath);
        _filePath = Path.Combine(CacheDir.AbsolutePath, $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

#pragma warning disable CA1416
        // Create AudioRecord using the recorder
        _audioRecord = _audioRecorder!.CreateAudioRecord(out int minBufferSize);

        // Initialize Voice Activity Detector (VAD) with configuration
        _calibration ??= new AudioCalibration();
        
        // Configure VAD based on audio configuration and calibration
        var vadConfig = new VadConfiguration
        {
            SampleRate = _config!.SampleRate,
            FrameMs = 30,  // 30ms frames for good balance
            AttackFrames = 4,  // ~120ms to trigger speech (prevents false positives)
            ReleaseFrames = 25,  // ~750ms silence to end speech (smoother than 2s)
            PreRollMs = 200,  // 200ms pre-roll to capture speech onset
            PostRollMs = 300,  // 300ms post-roll to capture speech tail
            AbsMinRaw = 300f,  // Minimum absolute threshold
            StartFactor = 3.0f,  // Start threshold = 3x ambient noise
            EndFactor = 2.0f,   // End threshold = 2x ambient noise
            AmbientCalibrationSeconds = 1.0f,  // 1 second initial calibration
            MaxMemoryStorageBytes = 50 * 1024 * 1024  // 50MB before file fallback
        };
        
        _vad = new VoiceActivityDetector(vadConfig);
        
        // Subscribe to VAD events
        _vad.OnSegmentStarted += OnSpeechSegmentStarted;
        _vad.OnSegmentEnded += OnSpeechSegmentEnded;
        
        _chunkBuffer = new AudioChunkBuffer(_config.SampleRate, _config.ChannelCount, _config.BitsPerSample);
        _chunkCounter = 0;
        _hasDetectedSpeech = false;

        // Open output stream and write WAV header placeholder (for backup/full recording)
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
                        // Apply software gain amplification for distant audio
                        if (Math.Abs(_config.GainMultiplier - 1.0f) > 0.001f)
                        {
                            ApplyGain(buffer, read, _config.GainMultiplier);
                        }
                        
                        // Write to backup file
                        _outputStream?.Write(buffer, 0, read);
                        
                        // Process audio for silence detection and chunking
                        ProcessAudioChunk(buffer, 0, read);
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

    /// <summary>
    /// Applies gain amplification to PCM16 audio data.
    /// </summary>
    private void ApplyGain(byte[] buffer, int length, float gain)
    {
        for (int i = 0; i < length; i += 2)
        {
            // Read 16-bit PCM sample (little-endian)
            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
            
            // Apply gain and clamp to prevent clipping
            int amplified = (int)(sample * gain);
            if (amplified > short.MaxValue)
                amplified = short.MaxValue;
            else if (amplified < short.MinValue)
                amplified = short.MinValue;
            
            // Write back amplified sample
            buffer[i] = (byte)(amplified & 0xFF);
            buffer[i + 1] = (byte)((amplified >> 8) & 0xFF);
        }
    }

    /// <summary>
    /// Processes audio chunk for VAD and streaming.
    /// </summary>
    private void ProcessAudioChunk(byte[] buffer, int offset, int length)
    {
        if (_vad == null || _chunkBuffer == null)
            return;

        // Always append data to the current chunk
        _chunkBuffer.AppendData(buffer, offset, length);

        // Feed audio to VAD for speech detection
        try
        {
            _vad.ProcessAudioChunk(buffer, offset, length, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("RecordingService", $"VAD processing error: {ex.Message}");
        }

        // Calculate audio level for logging
        double rms = CalculateRms(buffer, offset, length);
        double dbfs = RmsToDbfs(rms);

        // Log VAD state every ~1 second (assuming ~100ms buffers, log every 10th call)
        _chunkCounter++;
        if (_chunkCounter % 10 == 0)
        {
            Android.Util.Log.Debug("RecordingService", 
                $"Audio: {dbfs:F1} dBFS, InSpeech: {_vad.IsInSpeech}, Calibrated: {_vad.IsCalibrated}, " +
                $"Ambient: {_vad.AmbientNoiseRms:F0}, StartThresh: {_vad.StartThreshold:F0}, EndThresh: {_vad.EndThreshold:F0}");
        }
    }

    /// <summary>
    /// Calculates RMS for logging purposes.
    /// </summary>
    private static double CalculateRms(byte[] buffer, int offset, int length)
    {
        if (buffer == null || length <= 0)
            return 0.0;

        long sumOfSquares = 0;
        int sampleCount = 0;

        for (int i = offset; i < offset + length && i < buffer.Length - 1; i += 2)
        {
            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
            sumOfSquares += (long)sample * sample;
            sampleCount++;
        }

        if (sampleCount == 0)
            return 0.0;

        double meanSquare = (double)sumOfSquares / sampleCount;
        return Math.Sqrt(meanSquare);
    }

    /// <summary>
    /// Converts RMS to dBFS for logging.
    /// </summary>
    private static double RmsToDbfs(double rms)
    {
        if (rms <= 0)
            return -100;

        const double fullScale = 32767.0;
        double dbfs = 20 * Math.Log10(rms / fullScale);
        return Math.Max(-100, Math.Min(0, dbfs));
    }

    /// <summary>
    /// Called when VAD detects speech segment start.
    /// </summary>
    private void OnSpeechSegmentStarted(TimeSpan start)
    {
        _hasDetectedSpeech = true;
        Android.Util.Log.Info("RecordingService", 
            $"🎤 SPEECH STARTED at {start.TotalSeconds:F2}s");
    }

    /// <summary>
    /// Called when VAD detects speech segment end - finalize the chunk.
    /// </summary>
    private void OnSpeechSegmentEnded(TimeSpan start, TimeSpan end)
    {
        var duration = end - start;
        Android.Util.Log.Info("RecordingService", 
            $"⚡ SPEECH ENDED: {start.TotalSeconds:F2}s - {end.TotalSeconds:F2}s (duration: {duration.TotalSeconds:F2}s)");
        
        // Finalize the current chunk when speech ends
        FinalizeCurrentChunk();
        
        // Reset for next chunk
        _hasDetectedSpeech = false;
    }

    /// <summary>
    /// Finalizes the current audio chunk and enqueues it for processing.
    /// </summary>
    private void FinalizeCurrentChunk()
    {
        if (_chunkBuffer == null || !_chunkBuffer.HasData)
            return;

        var chunkStream = _chunkBuffer.FinalizeChunk();
        if (chunkStream != null && chunkStream.Length > 0)
        {
            var chunkName = $"chunk_{DateTime.Now:yyyyMMdd_HHmmss}_{_chunkCounter}.wav";
            
            Android.Util.Log.Info("RecordingForegroundService", 
                $"✅ Finalizing audio chunk: {chunkName}, Size: {chunkStream.Length} bytes");

            // Enqueue the chunk for processing
            AudioProcessQueue.Instance.Enqueue(new AudioProcess
            {
                Name = chunkName,
                AudioStream = chunkStream,
                Timestamp = DateTime.Now
            });
        }
    }

    /// <summary>
    /// Finalizes any remaining audio chunk when recording stops.
    /// </summary>
    private void FinalizeRemainingChunk()
    {
        // Force end any ongoing speech detection
        _vad?.ForceEndSpeech();
        
        // Finalize any remaining buffered audio
        if (_chunkBuffer != null && _chunkBuffer.HasData)
        {
            FinalizeCurrentChunk();
        }

        // Cleanup
        _vad?.Dispose();
        _vad = null;
        _chunkBuffer?.Dispose();
        _chunkBuffer = null;
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
