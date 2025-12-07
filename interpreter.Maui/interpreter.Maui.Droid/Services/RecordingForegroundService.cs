using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using System.Threading;
using System.Collections.Generic;

namespace interpreter.Maui.Services;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
public class RecordingForegroundService : Service
{
    public const string ActionStart = "ACTION_START_RECORDING";
    public const string ActionStop = "ACTION_STOP_RECORDING";

    private AudioRecord? _audioRecord;
    private Thread? _recordingThread;
    private MemoryStream? _audioBuffer; // Buffer all audio in memory for segment extraction
    private volatile bool _isRecordingInternal;
    private string? _filePath;
    
    private AudioRecordingConfiguration? _config;
    private IRecordingNotificationManager? _notificationManager;
    private IAudioRecorderService? _audioRecorder;
    private SlidingWindowVad? _slidingWindowVad;
    private int _logCounter;
    private int _segmentCounter;
    private readonly object _bufferLock = new();
    

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
            }
            catch
            {
                _config = new AudioRecordingConfiguration();
            }
        }
        
        _notificationManager ??= new RecordingNotificationManager(this);
        _audioRecorder ??= new AudioRecorderService(_config);
        
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

        // Initialize SlidingWindowVad with configuration optimized for dynamic environments
        // The first 2 seconds are used for calibration to detect ambient noise level
        var vadConfig = new SlidingWindowVadConfiguration
        {
            SampleRate = _config!.SampleRate,
            
            // Sliding window configuration
            WindowSizeMs = 1500,           // 1.5 second window (15 samples)
            SampleIntervalMs = 100,        // Sample every 100ms
            
            // Speech detection thresholds (ratio of window that must be speech)
            SpeechStartThreshold = 0.70f,  // 70% of window must be speech to START
            SpeechEndThreshold = 0.50f,    // Below 50% to END speech
            
            // Dynamic calibration - CRITICAL for different environments
            // First 2 seconds: measure ambient noise (market vs quiet home)
            CalibrationDurationMs = 2000,
            
            // RMS multiplier: speech must be this many times louder than ambient
            // Lower = more sensitive (good for far/quiet), Higher = less false positives
            SpeechRmsMultiplier = 1.8f,    // Start at 1.8x ambient noise
            MinSpeechRmsMultiplier = 1.3f, // Can go down to 1.3x in quiet environments
            MaxSpeechRmsMultiplier = 2.5f, // Can go up to 2.5x in noisy environments
            
            // Absolute RMS thresholds (regardless of calibration)
            MinRmsThreshold = 80f,         // Minimum threshold (very quiet room)
            MaxRmsThreshold = 10000f,      // Maximum threshold (very noisy market)
            
            // Pre/Post roll to capture full speech
            PreRollMs = 200,               // 200ms before detected speech
            PostRollMs = 200,              // 200ms after detected speech
            
            // Segment validation
            MinSegmentDurationMs = 500,    // Ignore segments shorter than 500ms
            
            // Adaptive ambient noise tracking (after calibration)
            AdaptiveAlpha = 0.02f,         // Very slow adaptation during silence
            AdaptiveAlphaFast = 0.10f,     // Faster adaptation when noise rises
            
            // Adaptive sensitivity
            RecentRmsHistorySize = 30,     // Track more samples for better adaptation
            LowVolumeAdaptationThreshold = 0.6f // Increase sensitivity if volume drops to 60% of calibration
        };
        
        _slidingWindowVad = new SlidingWindowVad(vadConfig);
        _logCounter = 0;
        _segmentCounter = 0;
        
        // Subscribe to debug events for logging
        _slidingWindowVad.OnDebugInfo += OnVadDebugInfo;
        _slidingWindowVad.OnSpeechStarted += (start) => 
            Android.Util.Log.Info("Recording", $"🎤 Speech started at {start:mm\\:ss\\.ff}");
        _slidingWindowVad.OnSpeechEnded += OnSpeechSegmentDetected;

        // Initialize memory buffer for all audio data
        _audioBuffer = new MemoryStream();

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
                        
                        // Store all audio in memory buffer (thread-safe)
                        lock (_bufferLock)
                        {
                            _audioBuffer?.Write(buffer, 0, read);
                        }
                        
                        // Process audio through VAD - will trigger OnSpeechSegmentDetected when speech ends
                        _slidingWindowVad?.ProcessAudio(buffer, 0, read);
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

            // Force end any ongoing speech detection (will trigger OnSpeechSegmentDetected if speech was active)
            _slidingWindowVad?.ForceEndSpeech();
            
            // Cleanup VAD and audio buffer resources
            CleanupVad();
        }
        catch (Exception) { }
        finally
        {
            RecordingState.IsRecording = false;
            RecordingState.LastFilePath = _filePath;
        }
    }
    
    /// <summary>
    /// VAD debug info callback for logging.
    /// </summary>
    private void OnVadDebugInfo(float rms, float threshold, bool isSpeech, float windowRatio)
    {
        _logCounter++;
        // Log every ~1 second (approximately every 3rd call at 300ms intervals)
        if (_logCounter % 3 == 0)
        {
            Android.Util.Log.Debug("Recording", 
                $"VAD: RMS={rms:F0} Thresh={threshold:F0} Speech={isSpeech} Window={windowRatio:P0}");
        }
    }

    /// <summary>
    /// Called in real-time when a speech segment is detected.
    /// Extracts the segment from the audio buffer and enqueues it for processing.
    /// </summary>
    private void OnSpeechSegmentDetected(SpeechTimeSegment segment)
    {
        try
        {
            Android.Util.Log.Info("Recording", $"🔇 Speech ended: {segment}");
            
            byte[] audioData;
            lock (_bufferLock)
            {
                if (_audioBuffer == null || _audioBuffer.Length == 0)
                {
                    Android.Util.Log.Warn("Recording", "Audio buffer is empty, cannot extract segment");
                    return;
                }
                
                // Get a snapshot of current audio data
                audioData = _audioBuffer.ToArray();
            }
            
            // Extract this single segment from the audio buffer
            var segmentList = new List<SpeechTimeSegment> { segment };
            var extractedSegments = SpeechSegmentExtractor.ExtractIndividualSegments(
                audioData,
                _config!.SampleRate,
                _config.ChannelCount,
                _config.BitsPerSample,
                segmentList);
            
            foreach (var (seg, wavStream) in extractedSegments)
            {
                var chunkName = $"speech_{DateTime.Now:yyyyMMdd_HHmmss}_{_segmentCounter++}.wav";
                
                Android.Util.Log.Info("Recording", 
                    $"✅ Real-time extracted segment: {seg}, Size: {wavStream.Length} bytes");
                
                // Enqueue for processing immediately while still recording
                AudioProcessQueue.Instance.Enqueue(new AudioProcess
                {
                    Name = chunkName,
                    AudioStream = wavStream,
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("Recording", $"Error extracting speech segment: {ex.Message}");
        }
    }

    /// <summary>
    /// Save full audio as WAV file (for debugging purposes).
    /// </summary>
    private void SaveFullAudioWithMarkers(byte[] audioData, List<SpeechTimeSegment> segments)
    {
        try
        {
            using var fileStream = new FileStream(_filePath!, FileMode.Create, FileAccess.Write);
            WavFileHandler.WriteWavHeader(fileStream, _config!.SampleRate, _config.ChannelCount, _config.BitsPerSample);
            fileStream.Write(audioData, 0, audioData.Length);
            WavFileHandler.UpdateWavHeader(fileStream);
            
            // Log segment info for reference
            Android.Util.Log.Info("Recording", $"Saved full audio: {_filePath}");
            Android.Util.Log.Info("Recording", $"Speech segments:");
            foreach (var s in segments)
            {
                Android.Util.Log.Info("Recording", $"  - {s}");
            }
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("Recording", $"Error saving full audio: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Cleanup VAD resources.
    /// </summary>
    private void CleanupVad()
    {
        _slidingWindowVad?.Dispose();
        _slidingWindowVad = null;
        
        _audioBuffer?.Dispose();
        _audioBuffer = null;
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
