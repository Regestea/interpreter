using System;
using System.Collections.Generic;

namespace interpreter.Maui.Services;

/// <summary>
/// Represents a detected speech segment with start and end times.
/// </summary>
public readonly struct SpeechTimeSegment
{
    public TimeSpan Start { get; init; }
    public TimeSpan End { get; init; }
    public TimeSpan Duration => End - Start;
    
    public override string ToString() => $"{Start:mm\\:ss\\.ff} - {End:mm\\:ss\\.ff} ({Duration.TotalSeconds:F2}s)";
}

/// <summary>
/// Configuration for sliding window voice activity detection.
/// </summary>
public class SlidingWindowVadConfiguration
{
    /// <summary>
    /// Audio sample rate in Hz.
    /// </summary>
    public int SampleRate { get; set; } = 44100;
    
    /// <summary>
    /// Size of the sliding window in milliseconds (default: 3000ms = 3 seconds).
    /// </summary>
    public int WindowSizeMs { get; set; } = 3000;
    
    /// <summary>
    /// Interval between samples in milliseconds (default: 300ms).
    /// Each sample is classified as speech (1) or silence (0).
    /// </summary>
    public int SampleIntervalMs { get; set; } = 300;
    
    /// <summary>
    /// Percentage of window that must be speech to START detecting speech (0.0 to 1.0).
    /// Default: 0.8 (80%)
    /// </summary>
    public float SpeechStartThreshold { get; set; } = 0.8f;
    
    /// <summary>
    /// Percentage of window that must be speech to CONTINUE speech (0.0 to 1.0).
    /// Default: 0.5 (50%)
    /// </summary>
    public float SpeechEndThreshold { get; set; } = 0.5f;
    
    /// <summary>
    /// Initial ambient noise calibration duration in milliseconds.
    /// </summary>
    public int CalibrationDurationMs { get; set; } = 2000;
    
    /// <summary>
    /// Base multiplier applied to ambient noise RMS to determine speech threshold.
    /// Speech is detected when RMS > AmbientRms * SpeechRmsMultiplier
    /// Reduced from 2.5 to 1.5 for better far-field detection.
    /// </summary>
    public float SpeechRmsMultiplier { get; set; } = 1.5f;
    
    /// <summary>
    /// Minimum speech-to-noise ratio multiplier (adaptive sensitivity floor).
    /// The system will reduce multiplier down to this value in quiet environments.
    /// </summary>
    public float MinSpeechRmsMultiplier { get; set; } = 1.2f;
    
    /// <summary>
    /// Maximum speech-to-noise ratio multiplier (adaptive sensitivity ceiling).
    /// </summary>
    public float MaxSpeechRmsMultiplier { get; set; } = 3.0f;
    
    /// <summary>
    /// Minimum RMS threshold regardless of ambient noise (prevents false triggers in very quiet environments).
    /// Reduced from 200 to 100 for better sensitivity.
    /// </summary>
    public float MinRmsThreshold { get; set; } = 100f;
    
    /// <summary>
    /// Maximum RMS threshold (to adapt when speaker is far from microphone).
    /// </summary>
    public float MaxRmsThreshold { get; set; } = 8000f;
    
    /// <summary>
    /// How much time to add before detected speech start to capture onset (ms).
    /// </summary>
    public int PreRollMs { get; set; } = 500;
    
    /// <summary>
    /// How much time to add after detected speech end to capture trailing sounds (ms).
    /// </summary>
    public int PostRollMs { get; set; } = 500;
    
    /// <summary>
    /// Minimum duration for a valid speech segment (ms). Shorter segments are ignored.
    /// </summary>
    public int MinSegmentDurationMs { get; set; } = 500;
    
    /// <summary>
    /// Time constant for adaptive ambient noise tracking (slower = more stable).
    /// Increased from 0.05 to 0.03 for more stable tracking.
    /// </summary>
    public float AdaptiveAlpha { get; set; } = 0.03f;
    
    /// <summary>
    /// Time constant for fast ambient noise tracking (for rising noise).
    /// Allows faster adaptation when noise increases.
    /// </summary>
    public float AdaptiveAlphaFast { get; set; } = 0.15f;
    
    /// <summary>
    /// Number of recent RMS values to track for adaptive sensitivity.
    /// </summary>
    public int RecentRmsHistorySize { get; set; } = 20;
    
    /// <summary>
    /// If the max recent RMS is below this percentile of calibration, increase sensitivity.
    /// </summary>
    public float LowVolumeAdaptationThreshold { get; set; } = 0.7f;
    
    /// <summary>
    /// Gets the number of samples in the sliding window.
    /// </summary>
    public int GetWindowSampleCount() => WindowSizeMs / SampleIntervalMs;
}

/// <summary>
/// Voice Activity Detector using sliding window algorithm.
/// <para>
/// Algorithm: Audio is analyzed every SampleIntervalMs (e.g., 300ms).
/// Each interval is classified as speech (1) or silence (0) based on RMS.
/// A sliding window of WindowSizeMs (e.g., 3s) tracks recent classifications.
/// Speech STARTS when at least SpeechStartThreshold (e.g., 80%) of window is speech.
/// Speech ENDS when less than SpeechEndThreshold (e.g., 50%) of window is speech.
/// </para>
/// <para>
/// Benefits: Resistant to short impulse noises (taps on microphone).
/// Provides time ranges, not real-time streaming. Simple and predictable behavior.
/// </para>
/// </summary>
public sealed class SlidingWindowVad : IDisposable
{
    private readonly SlidingWindowVadConfiguration _config;
    private readonly object _lock = new();
    
    // Sliding window state
    private readonly Queue<bool> _windowSamples; // true = speech, false = silence
    private readonly int _windowSize;
    private int _speechSamplesInWindow;
    
    // Audio accumulation for current sample interval
    private readonly List<short> _currentIntervalSamples;
    private int _samplesPerInterval;
    private long _totalSamplesProcessed;
    
    // Calibration state
    private readonly List<float> _calibrationRmsValues;
    private int _calibrationSamplesNeeded;
    private bool _isCalibrated;
    private float _ambientNoiseRms;
    private float _currentThreshold;
    
    // Adaptive sensitivity state
    private float _currentMultiplier;
    private readonly Queue<float> _recentRmsHistory;
    private float _calibrationMaxRms;
    
    // Speech detection state
    private bool _inSpeech;
    private TimeSpan _speechStartTime;
    
    // Results
    private readonly List<SpeechTimeSegment> _detectedSegments;
    
    private bool _disposed;

    // Events
    public event Action<TimeSpan>? OnSpeechStarted;
    public event Action<SpeechTimeSegment>? OnSpeechEnded;
    public event Action<float, float, bool, float>? OnDebugInfo; // rms, threshold, isSpeech, windowRatio

    // Public properties
    public bool IsCalibrated => _isCalibrated;
    public bool IsInSpeech => _inSpeech;
    public float AmbientNoiseRms => _ambientNoiseRms;
    public float CurrentThreshold => _currentThreshold;
    public float CurrentMultiplier => _currentMultiplier;
    public IReadOnlyList<SpeechTimeSegment> DetectedSegments => _detectedSegments;
    public float WindowSpeechRatio => _windowSize > 0 ? (float)_speechSamplesInWindow / _windowSize : 0f;

    public SlidingWindowVad(SlidingWindowVadConfiguration? config = null)
    {
        _config = config ?? new SlidingWindowVadConfiguration();
        
        _windowSize = _config.GetWindowSampleCount();
        _windowSamples = new Queue<bool>(_windowSize);
        _speechSamplesInWindow = 0;
        
        _samplesPerInterval = (_config.SampleRate * _config.SampleIntervalMs) / 1000;
        _currentIntervalSamples = new List<short>(_samplesPerInterval);
        _totalSamplesProcessed = 0;
        
        // Calculate how many interval samples needed for calibration
        _calibrationSamplesNeeded = _config.CalibrationDurationMs / _config.SampleIntervalMs;
        _calibrationRmsValues = new List<float>(_calibrationSamplesNeeded);
        _isCalibrated = false;
        _ambientNoiseRms = _config.MinRmsThreshold;
        _currentMultiplier = _config.SpeechRmsMultiplier;
        _currentThreshold = _config.MinRmsThreshold * _currentMultiplier;
        
        // Adaptive sensitivity
        _recentRmsHistory = new Queue<float>(_config.RecentRmsHistorySize);
        _calibrationMaxRms = 0f;
        
        _inSpeech = false;
        _detectedSegments = new List<SpeechTimeSegment>();
    }

    /// <summary>
    /// Process audio data in byte[] format (PCM16 little-endian).
    /// </summary>
    public void ProcessAudio(byte[] buffer, int offset, int length)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SlidingWindowVad));
        if (buffer == null || length <= 0) return;

        // Convert bytes to shorts
        int sampleCount = length / 2;
        short[] samples = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            int byteOffset = offset + (i * 2);
            samples[i] = (short)(buffer[byteOffset] | (buffer[byteOffset + 1] << 8));
        }
        
        ProcessAudio(samples, 0, sampleCount);
    }

    /// <summary>
    /// Process audio data in short[] format.
    /// </summary>
    public void ProcessAudio(short[] samples, int offset, int count)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SlidingWindowVad));
        if (samples == null || count <= 0) return;

        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                _currentIntervalSamples.Add(samples[offset + i]);
                _totalSamplesProcessed++;
                
                // When we have enough samples for one interval, process it
                if (_currentIntervalSamples.Count >= _samplesPerInterval)
                {
                    ProcessInterval();
                    _currentIntervalSamples.Clear();
                }
            }
        }
    }

    /// <summary>
    /// Process one complete sample interval.
    /// </summary>
    private void ProcessInterval()
    {
        // Calculate RMS for this interval
        float rms = CalculateRms(_currentIntervalSamples);
        
        // Track recent RMS values for adaptive sensitivity
        TrackRecentRms(rms);
        
        // Handle calibration
        if (!_isCalibrated)
        {
            _calibrationRmsValues.Add(rms);
            if (_calibrationRmsValues.Count >= _calibrationSamplesNeeded)
            {
                CompleteCalibration();
            }
            // During calibration, treat everything as silence
            AddSampleToWindow(false);
            return;
        }
        
        // Classify this interval as speech or silence
        bool isSpeech = rms >= _currentThreshold;
        
        // Add to sliding window
        AddSampleToWindow(isSpeech);
        
        // Adaptive threshold adjustment when not in speech
        if (!_inSpeech)
        {
            if (!isSpeech)
            {
                // Slowly adapt ambient noise estimate downward
                _ambientNoiseRms = _ambientNoiseRms * (1 - _config.AdaptiveAlpha) + rms * _config.AdaptiveAlpha;
            }
            else if (rms > _ambientNoiseRms)
            {
                // Fast adapt upward when noise increases (but only up to current ambient level)
                float noisyAmbient = _ambientNoiseRms * 1.2f; // Only consider it noise if close to ambient
                if (rms < noisyAmbient)
                {
                    _ambientNoiseRms = _ambientNoiseRms * (1 - _config.AdaptiveAlphaFast) + rms * _config.AdaptiveAlphaFast;
                }
            }
            
            // Adaptive sensitivity - reduce multiplier if audio levels are consistently low
            UpdateAdaptiveSensitivity();
            UpdateThreshold();
        }
        
        // Check for speech state changes based on window ratio
        float windowRatio = WindowSpeechRatio;
        
        OnDebugInfo?.Invoke(rms, _currentThreshold, isSpeech, windowRatio);
        
        if (!_inSpeech)
        {
            // Check if we should START speech detection
            if (windowRatio >= _config.SpeechStartThreshold)
            {
                StartSpeech();
            }
        }
        else
        {
            // Check if we should END speech detection
            if (windowRatio < _config.SpeechEndThreshold)
            {
                EndSpeech();
            }
        }
    }
    
    private void TrackRecentRms(float rms)
    {
        _recentRmsHistory.Enqueue(rms);
        while (_recentRmsHistory.Count > _config.RecentRmsHistorySize)
        {
            _recentRmsHistory.Dequeue();
        }
    }
    
    private void UpdateAdaptiveSensitivity()
    {
        if (_recentRmsHistory.Count < _config.RecentRmsHistorySize / 2)
            return;
        
        // Find the max and average RMS in recent history
        float maxRecentRms = 0f;
        float sumRms = 0f;
        foreach (var r in _recentRmsHistory)
        {
            if (r > maxRecentRms) maxRecentRms = r;
            sumRms += r;
        }
        float avgRecentRms = sumRms / _recentRmsHistory.Count;
        
        // Calculate the ratio of max recent to ambient
        float signalToNoise = _ambientNoiseRms > 0 ? maxRecentRms / _ambientNoiseRms : 1f;
        
        // Dynamic adjustment based on multiple factors:
        
        // 1. If signal levels are low compared to calibration, increase sensitivity
        if (_calibrationMaxRms > 0 && maxRecentRms < _calibrationMaxRms * _config.LowVolumeAdaptationThreshold)
        {
            // User seems to be speaking quietly or far from mic - reduce multiplier
            _currentMultiplier = Math.Max(
                _currentMultiplier * 0.98f, // Slowly reduce
                _config.MinSpeechRmsMultiplier
            );
        }
        // 2. If average noise level has increased significantly, increase multiplier
        else if (avgRecentRms > _ambientNoiseRms * 1.5f && signalToNoise < 2.0f)
        {
            // Background noise increased but no clear speech signal
            _currentMultiplier = Math.Min(
                _currentMultiplier * 1.02f, // Slowly increase
                _config.MaxSpeechRmsMultiplier
            );
        }
        // 3. Strong clear signal - can use standard multiplier
        else if (signalToNoise > 3.0f)
        {
            // Strong signal - can use higher multiplier for noise rejection
            _currentMultiplier = Math.Min(
                _currentMultiplier * 1.01f,
                _config.MaxSpeechRmsMultiplier
            );
        }
    }

    private void AddSampleToWindow(bool isSpeech)
    {
        // Add new sample
        _windowSamples.Enqueue(isSpeech);
        if (isSpeech) _speechSamplesInWindow++;
        
        // Remove oldest if window is full
        while (_windowSamples.Count > _windowSize)
        {
            bool removed = _windowSamples.Dequeue();
            if (removed) _speechSamplesInWindow--;
        }
    }

    private void CompleteCalibration()
    {
        if (_calibrationRmsValues.Count == 0)
        {
            _ambientNoiseRms = _config.MinRmsThreshold;
            _calibrationMaxRms = _config.MinRmsThreshold * 2;
            _currentMultiplier = _config.SpeechRmsMultiplier;
        }
        else
        {
            // Sort values to analyze the distribution
            var sorted = new List<float>(_calibrationRmsValues);
            sorted.Sort();
            
            // Use median for ambient noise (ignores outliers like coughs)
            int mid = sorted.Count / 2;
            float median = sorted.Count % 2 == 0 
                ? (sorted[mid - 1] + sorted[mid]) / 2f 
                : sorted[mid];
            
            // Also calculate percentiles for better understanding of environment
            int p10Index = (int)(sorted.Count * 0.1f);
            int p90Index = (int)(sorted.Count * 0.9f);
            float p10Rms = sorted[Math.Max(0, p10Index)];
            float p90Rms = sorted[Math.Min(p90Index, sorted.Count - 1)];
            
            // Calculate ambient noise variance
            float variance = p90Rms - p10Rms;
            float varianceRatio = median > 0 ? variance / median : 0;
            
            // Use median as ambient noise baseline
            _ambientNoiseRms = median;
            _calibrationMaxRms = sorted[sorted.Count - 1];
            
            // Dynamically adjust multiplier based on environment:
            // - High variance (noisy market): use higher multiplier to avoid false triggers
            // - Low variance (quiet home): use lower multiplier for better sensitivity
            if (varianceRatio > 0.5f)
            {
                // Noisy environment - increase multiplier for noise rejection
                _currentMultiplier = Math.Min(_config.SpeechRmsMultiplier * 1.2f, _config.MaxSpeechRmsMultiplier);
                Android.Util.Log.Info("SlidingWindowVad", $"🔊 Noisy environment detected (variance ratio: {varianceRatio:F2})");
            }
            else if (varianceRatio < 0.15f && p90Rms < _config.MinRmsThreshold * 3)
            {
                // Very quiet environment - decrease multiplier for better sensitivity
                _currentMultiplier = Math.Max(_config.SpeechRmsMultiplier * 0.8f, _config.MinSpeechRmsMultiplier);
                Android.Util.Log.Info("SlidingWindowVad", $"🤫 Quiet environment detected (variance ratio: {varianceRatio:F2})");
            }
            else
            {
                // Normal environment
                _currentMultiplier = _config.SpeechRmsMultiplier;
            }
        }
        
        // Ensure minimum threshold
        _ambientNoiseRms = Math.Max(_ambientNoiseRms, _config.MinRmsThreshold);
        
        UpdateThreshold();
        _isCalibrated = true;
        
        Android.Util.Log.Info("SlidingWindowVad", 
            $"✅ Calibration complete: Ambient={_ambientNoiseRms:F0}, MaxCal={_calibrationMaxRms:F0}, Multiplier={_currentMultiplier:F2}, Threshold={_currentThreshold:F0}");
    }

    private void UpdateThreshold()
    {
        _currentThreshold = Math.Clamp(
            _ambientNoiseRms * _currentMultiplier,
            _config.MinRmsThreshold,
            _config.MaxRmsThreshold);
    }

    private void StartSpeech()
    {
        // Calculate start time, considering the window lead time
        // Speech actually started earlier than current time
        var currentTime = GetCurrentTime();
        var windowDuration = TimeSpan.FromMilliseconds(_config.WindowSizeMs);
        var preRoll = TimeSpan.FromMilliseconds(_config.PreRollMs);
        
        // Find when speech actually began in the window
        // Use the first speech sample time, minus some buffer
        _speechStartTime = currentTime - windowDuration - preRoll;
        if (_speechStartTime < TimeSpan.Zero) _speechStartTime = TimeSpan.Zero;
        
        _inSpeech = true;
        
        OnSpeechStarted?.Invoke(_speechStartTime);
        
        Android.Util.Log.Info("SlidingWindowVad", 
            $"🎤 Speech STARTED at {_speechStartTime:mm\\:ss\\.ff}");
    }

    private void EndSpeech()
    {
        var currentTime = GetCurrentTime();
        var postRoll = TimeSpan.FromMilliseconds(_config.PostRollMs);
        var speechEndTime = currentTime + postRoll;
        
        // Validate minimum segment duration
        var duration = speechEndTime - _speechStartTime;
        if (duration.TotalMilliseconds < _config.MinSegmentDurationMs)
        {
            Android.Util.Log.Debug("SlidingWindowVad", 
                $"❌ Ignored short segment: {duration.TotalMilliseconds:F0}ms < {_config.MinSegmentDurationMs}ms");
            _inSpeech = false;
            return;
        }
        
        var segment = new SpeechTimeSegment
        {
            Start = _speechStartTime,
            End = speechEndTime
        };
        
        _detectedSegments.Add(segment);
        _inSpeech = false;
        
        OnSpeechEnded?.Invoke(segment);
        
        Android.Util.Log.Info("SlidingWindowVad", 
            $"🔇 Speech ENDED: {segment}");
    }

    private TimeSpan GetCurrentTime()
    {
        return TimeSpan.FromSeconds((double)_totalSamplesProcessed / _config.SampleRate);
    }

    /// <summary>
    /// Force end any ongoing speech segment (call when recording stops).
    /// </summary>
    public void ForceEndSpeech()
    {
        lock (_lock)
        {
            if (_inSpeech)
            {
                EndSpeech();
            }
        }
    }

    /// <summary>
    /// Get all detected speech segments.
    /// </summary>
    public List<SpeechTimeSegment> GetSegments()
    {
        lock (_lock)
        {
            return new List<SpeechTimeSegment>(_detectedSegments);
        }
    }

    /// <summary>
    /// Clear all detected segments (but keep calibration).
    /// </summary>
    public void ClearSegments()
    {
        lock (_lock)
        {
            _detectedSegments.Clear();
        }
    }

    /// <summary>
    /// Reset the VAD completely (including calibration).
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _windowSamples.Clear();
            _speechSamplesInWindow = 0;
            _currentIntervalSamples.Clear();
            _totalSamplesProcessed = 0;
            
            _calibrationRmsValues.Clear();
            _isCalibrated = false;
            _ambientNoiseRms = _config.MinRmsThreshold;
            _currentMultiplier = _config.SpeechRmsMultiplier;
            _currentThreshold = _config.MinRmsThreshold * _currentMultiplier;
            
            _recentRmsHistory.Clear();
            _calibrationMaxRms = 0f;
            
            _inSpeech = false;
            _detectedSegments.Clear();
        }
    }

    /// <summary>
    /// Manually set the calibration values (useful when microphone distance is known).
    /// </summary>
    public void SetCalibration(float ambientNoiseRms)
    {
        lock (_lock)
        {
            _ambientNoiseRms = Math.Max(ambientNoiseRms, _config.MinRmsThreshold);
            UpdateThreshold();
            _isCalibrated = true;
            _calibrationRmsValues.Clear();
        }
    }

    private static float CalculateRms(List<short> samples)
    {
        if (samples == null || samples.Count == 0) return 0f;
        
        double sumOfSquares = 0.0;
        foreach (var sample in samples)
        {
            sumOfSquares += (double)sample * sample;
        }
        
        double meanSquare = sumOfSquares / samples.Count;
        return (float)Math.Sqrt(meanSquare);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        lock (_lock)
        {
            _windowSamples.Clear();
            _currentIntervalSamples.Clear();
            _calibrationRmsValues.Clear();
            _recentRmsHistory.Clear();
            _detectedSegments.Clear();
        }
    }
}

