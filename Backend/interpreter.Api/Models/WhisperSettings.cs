namespace interpreter.Api.Models;

/// <summary>
/// Configuration settings for Whisper service.
/// </summary>
public class WhisperSettings
{
    /// <summary>
    /// The path to the Whisper GGUF model file.
    /// </summary>
    public string ModelPath { get; set; } = "ggml-base.bin";

    /// <summary>
    /// The language code for transcription (e.g., "en", "auto").
    /// </summary>
    public string Language { get; set; } = "auto";
}

