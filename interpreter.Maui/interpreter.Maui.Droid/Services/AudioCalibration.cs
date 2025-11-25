namespace interpreter.Maui.Services;

/// <summary>
/// Stores calibration data for audio processing.
/// </summary>
public class AudioCalibration
{
    /// <summary>
    /// Average RMS (Root Mean Square) of environmental noise.
    /// Default value represents a quiet environment.
    /// </summary>
    public double EnvironmentalNoiseRms { get; set; } = 0.001;

    /// <summary>
    /// Environmental noise level in dBFS (decibels relative to full scale).
    /// Clamped to integer range between -55 and -20.
    /// </summary>
    public int EnvironmentalNoiseDbfs { get; set; } = -50;

    /// <summary>
    /// Threshold for voice detection based on environmental noise.
    /// Typically set to a multiple of EnvironmentalNoiseRms.
    /// </summary>
    public double VoiceDetectionThreshold { get; set; } = 0.01;

    /// <summary>
    /// Indicates whether calibration has been performed.
    /// </summary>
    public bool IsCalibrated { get; set; }

    /// <summary>
    /// Timestamp of the last calibration.
    /// </summary>
    public DateTime? LastCalibrationTime { get; set; }
}

