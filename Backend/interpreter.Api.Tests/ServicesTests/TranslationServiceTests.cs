using interpreter.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace interpreter.Api.Tests.ServicesTests;

public class TranslationServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TranslationService> _loggerMock;

    public TranslationServiceTests()
    {
        // Create real HttpClientFactory for integration testing
        var serviceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider();
        
        _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        _loggerMock = Substitute.For<ILogger<TranslationService>>();
    }

    [Fact]
    public async Task TranslateAsync_WithValidText_ReturnsTranslationResult()
    {
        // Arrange
        var service = new TranslationService(_httpClientFactory, _loggerMock);

        // Act - Make real HTTP request to Google Translate API
        var result = await service.TranslateAsync("Bonjour le monde", "en");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.TranslatedText);
        Assert.Contains("hello", result.TranslatedText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("fr", result.DetectedLanguage);
        Assert.Equal("Bonjour le monde", result.OriginalText);
    }

    [Fact]
    public async Task TranslateAsync_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange
        var service = new TranslationService(_httpClientFactory, _loggerMock);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.TranslateAsync("", "fa"));
    }

    [Fact]
    public async Task TranslateAsync_WithNullText_ThrowsArgumentException()
    {
        // Arrange
        var service = new TranslationService(_httpClientFactory, _loggerMock);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.TranslateAsync(null!, "fa"));
    }

    [Fact]
    public async Task TranslateAsync_WithEmptyTargetLanguage_ThrowsArgumentException()
    {
        // Arrange
        var service = new TranslationService(_httpClientFactory, _loggerMock);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.TranslateAsync("Hello", ""));
    }

    [Fact]
    public async Task TranslateAsync_WithNullTargetLanguage_ThrowsArgumentException()
    {
        // Arrange
        var service = new TranslationService(_httpClientFactory, _loggerMock);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.TranslateAsync("Hello", null!));
    }

    [Fact]
    public async Task TranslateAsync_WithSpanishText_ReturnsEnglishTranslation()
    {
        // Arrange
        var service = new TranslationService(_httpClientFactory, _loggerMock);

        // Act - Real HTTP request
        var result = await service.TranslateAsync("Hola, ¿cómo estás?", "en");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.TranslatedText);
        Assert.Contains("hello", result.TranslatedText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("es", result.DetectedLanguage);
        Assert.Equal("Hola, ¿cómo estás?", result.OriginalText);
    }
}


