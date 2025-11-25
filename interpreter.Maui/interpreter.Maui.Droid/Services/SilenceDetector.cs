using System;

namespace interpreter.Maui.Services;

/// <summary>
/// Detects silence in PCM16 audio data based on calibration settings.
/// </summary>
public class SilenceDetector
{
    private readonly AudioCalibration _calibration;
    private readonly int _sampleRate;
    private readonly TimeSpan _silenceThreshold;
    private int _consecutiveSilentSamples;

    public SilenceDetector(AudioCalibration calibration, int sampleRate, TimeSpan silenceThreshold)
    {
        _calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
        _sampleRate = sampleRate;
        _silenceThreshold = silenceThreshold;
        _consecutiveSilentSamples = 0;
    }

    /// <summary>
    /// Calculates the RMS (Root Mean Square) of PCM16 audio data.
    /// </summary>
    public static double CalculateRms(byte[] buffer, int offset, int length)
    {
        if (buffer == null || length <= 0)
            return 0.0;

        long sumOfSquares = 0;
        int sampleCount = 0;

        for (int i = offset; i < offset + length && i < buffer.Length - 1; i += 2)
        {
            // Read 16-bit PCM sample (little-endian)
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
    /// Converts RMS to dBFS (decibels relative to full scale).
    /// </summary>
    public static double RmsToDbfs(double rms)
    {
        if (rms <= 0)
            return -100; // Minimum dBFS

        // Full scale for 16-bit audio is 32767
        const double fullScale = 32767.0;
        double dbfs = 20 * Math.Log10(rms / fullScale);
        
        // Clamp to reasonable range
        return Math.Max(-100, Math.Min(0, dbfs));
    }

    /// <summary>
    /// Checks if the audio buffer contains silence based on calibration settings.
    /// Silence is defined as audio QUIETER than the environmental noise level.
    /// For example, if environmental noise is -50 dBFS, anything quieter (more negative) is silence.
    /// </summary>
    public bool IsSilence(byte[] buffer, int offset, int length)
    {
        double rms = CalculateRms(buffer, offset, length);
        double dbfs = RmsToDbfs(rms);

        // Audio is silent if it's quieter (more negative) than environmental noise
        // Example: If env noise = -50 dBFS, then -60 dBFS is silence, -40 dBFS is speech
        bool isSilent = dbfs < _calibration.EnvironmentalNoiseDbfs;

        if (isSilent)
        {
            _consecutiveSilentSamples += length / 2; // Convert bytes to samples (16-bit = 2 bytes per sample)
        }
        else
        {
            _consecutiveSilentSamples = 0;
        }

        return isSilent;
    }

    /// <summary>
    /// Checks if the current silence duration exceeds the threshold (e.g., 2 seconds).
    /// </summary>
    public bool HasSilenceThresholdExceeded()
    {
        double silenceDuration = (double)_consecutiveSilentSamples / _sampleRate;
        return silenceDuration >= _silenceThreshold.TotalSeconds;
    }

    /// <summary>
    /// Gets the current silence duration in seconds.
    /// </summary>
    public double GetSilenceDuration()
    {
        return (double)_consecutiveSilentSamples / _sampleRate;
    }

    /// <summary>
    /// Resets the silence counter.
    /// </summary>
    public void Reset()
    {
        _consecutiveSilentSamples = 0;
    }
}

