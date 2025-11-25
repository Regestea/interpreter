using System.IO;

namespace interpreter.Maui.Services;

/// <summary>
/// Represents an audio process item.
/// </summary>
public class AudioProcess
{
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional audio stream data (for chunked audio from silence detection).
    /// If provided, this takes precedence over file-based processing.
    /// </summary>
    public Stream? AudioStream { get; set; }
    
    /// <summary>
    /// Timestamp when the audio chunk was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

