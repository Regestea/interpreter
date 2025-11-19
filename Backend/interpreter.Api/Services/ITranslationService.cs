namespace interpreter.Api.Services;

/// <summary>
/// Interface for translation service using Google Translate API.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates text from auto-detected language to the target language.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="targetLanguage">The target language code (e.g., "en", "fa", "fr").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The translation result containing translated text and detected source language.</returns>
    Task<TranslationResult> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a translation operation.
/// </summary>
public class TranslationResult
{
    /// <summary>
    /// The translated text.
    /// </summary>
    public string TranslatedText { get; set; } = string.Empty;
    
    /// <summary>
    /// The detected source language code.
    /// </summary>
    public string DetectedLanguage { get; set; } = string.Empty;
    
    /// <summary>
    /// The original text.
    /// </summary>
    public string OriginalText { get; set; } = string.Empty;
}

