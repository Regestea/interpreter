using Microsoft.AspNetCore.Mvc;
using interpreter.Api.Services;

namespace interpreter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslationController : ControllerBase
{
    private readonly ILogger<TranslationController> _logger;
    private readonly ITranslationService _translationService;

    public TranslationController(
        ILogger<TranslationController> logger,
        ITranslationService translationService)
    {
        _logger = logger;
        _translationService = translationService;
    }

    /// <summary>
    /// Translate text to the specified target language with auto-detected source language
    /// </summary>
    /// <param name="request">Translation request containing text and target language</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request</param>
    /// <returns>Translation result with translated text and detected source language</returns>
    [HttpPost("translate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Translate([FromBody] TranslationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Text to translate cannot be empty" });
        }

        if (string.IsNullOrWhiteSpace(request.TargetLanguage))
        {
            return BadRequest(new { error = "Target language must be specified" });
        }

        try
        {
            _logger.LogInformation("Translating text to language: {TargetLanguage}", request.TargetLanguage);

            var result = await _translationService.TranslateAsync(
                request.Text, 
                request.TargetLanguage, 
                cancellationToken);

            return Ok(new
            {
                translatedText = result.TranslatedText,
                detectedLanguage = result.DetectedLanguage,
                originalText = result.OriginalText,
                targetLanguage = request.TargetLanguage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed");
            return StatusCode(500, new { error = "Translation failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Translate text to the specified target language via query parameters
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="targetLanguage">Target language code (e.g., "en", "fa", "fr")</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request</param>
    /// <returns>Translation result</returns>
    [HttpGet("translate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TranslateGet(
        [FromQuery] string text, 
        [FromQuery] string targetLanguage, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest(new { error = "Text to translate cannot be empty" });
        }

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            return BadRequest(new { error = "Target language must be specified" });
        }

        try
        {
            _logger.LogInformation("Translating text to language: {TargetLanguage}", targetLanguage);

            var result = await _translationService.TranslateAsync(text, targetLanguage, cancellationToken);

            return Ok(new
            {
                translatedText = result.TranslatedText,
                detectedLanguage = result.DetectedLanguage,
                originalText = result.OriginalText,
                targetLanguage = targetLanguage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed");
            return StatusCode(500, new { error = "Translation failed", message = ex.Message });
        }
    }
}

/// <summary>
/// Request model for translation
/// </summary>
public class TranslationRequest
{
    /// <summary>
    /// The text to translate
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The target language code (e.g., "en", "fa", "fr")
    /// </summary>
    public string TargetLanguage { get; set; } = string.Empty;
}

