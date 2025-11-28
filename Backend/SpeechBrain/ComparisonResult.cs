namespace SpeechBrain;

/// <summary>
/// Result of audio comparison
/// </summary>
public class ComparisonResult
{
    public double Score { get; set; }
    public bool IsMatch { get; set; }
    public string Status { get; set; } = string.Empty;
}