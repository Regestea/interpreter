namespace Models.Shared.Responses;

public class InterpreterResponse
{
    public byte[]? TranslatedAudio { get; set; }
    public string? OriginalText { get; set; }
    public string? TranslatedText { get; set; }
    public string? AudioInputLanguage { get; set; } // e.g., "en"
}