using System.Text.Json;
using System.Web;

namespace interpreter.Api.Services;

/// <summary>
/// Translation service implementation using Google Translate API.
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TranslationService> _logger;
    private const string GoogleTranslateUrl = "https://translate.google.com/translate_a/single";

    public TranslationService(IHttpClientFactory httpClientFactory, ILogger<TranslationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("GoogleTranslate");
        _logger = logger;
    }

    /// <summary>
    /// Translates text from auto-detected language to the target language.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="targetLanguage">The target language code (e.g., "en", "fa", "fr").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The translation result containing translated text and detected source language.</returns>
    public async Task<TranslationResult> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text to translate cannot be null or empty.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            throw new ArgumentException("Target language cannot be null or empty.", nameof(targetLanguage));
        }

        try
        {
            var encodedText = HttpUtility.UrlEncode(text);
            var url = $"{GoogleTranslateUrl}?client=gtx&sl=auto&tl={targetLanguage}&dt=t&q={encodedText}";

            _logger.LogInformation("Translating text to {TargetLanguage}", targetLanguage);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogDebug("Translation API response: {Response}", responseContent);

            return ParseTranslationResponse(responseContent, text);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while translating text");
            throw new Exception("Failed to communicate with translation service.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while translating text");
            throw new Exception("An error occurred during translation.", ex);
        }
    }

    /// <summary>
    /// Parses the Google Translate API response.
    /// Response format: [[["translated","original",null,null,3,null,null,[[],[]],[[["hash","model"]],[["hash","model"]]]]],null,"detectedLang",...]
    /// </summary>
    private TranslationResult ParseTranslationResponse(string responseContent, string originalText)
    {
        try
        {
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            var translatedText = string.Empty;
            var detectedLanguage = string.Empty;

            // Parse translated text from nested array structure
            // root[0] contains array of translation segments
            // root[0][0] is the first segment with [0] = translated, [1] = original
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var translationsArray = root[0];
                if (translationsArray.ValueKind == JsonValueKind.Array && translationsArray.GetArrayLength() > 0)
                {
                    // Build complete translated text from all segments
                    var textBuilder = new System.Text.StringBuilder();
                    foreach (var segment in translationsArray.EnumerateArray())
                    {
                        if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0)
                        {
                            var segmentText = segment[0].GetString();
                            if (!string.IsNullOrEmpty(segmentText))
                            {
                                textBuilder.Append(segmentText);
                            }
                        }
                    }
                    translatedText = textBuilder.ToString();
                }

                // Parse detected language (usually at index 2)
                if (root.GetArrayLength() > 2)
                {
                    var langElement = root[2];
                    if (langElement.ValueKind == JsonValueKind.String)
                    {
                        detectedLanguage = langElement.GetString() ?? string.Empty;
                    }
                }
            }

            _logger.LogInformation("Translation completed. Detected language: {DetectedLanguage}", detectedLanguage);

            return new TranslationResult
            {
                TranslatedText = translatedText,
                DetectedLanguage = detectedLanguage,
                OriginalText = originalText
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse translation response");
            throw new Exception("Failed to parse translation response.", ex);
        }
    }
}

