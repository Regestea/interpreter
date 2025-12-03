using System.Text;
using System.Text.Json;
using interpreter.Maui.DTOs;
using interpreter.Maui.Extensions;

namespace interpreter.Maui.Services;

public interface IApiClient
{
    Task<HttpResponseDto> SendAsync(string relativeUrl, HttpMethod method, object? body = null, bool includeAuth = true);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    // Base address is configured in DI
    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseDto> SendAsync(string relativeUrl, HttpMethod method, object? body = null, bool includeAuth = true)
    {
        var client = _httpClient;

        if (includeAuth)
        {
            client = await client.AddAuthHeader();
        }

        StringContent? jsonBody = null;
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            jsonBody = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var url = new Uri(client.BaseAddress!, relativeUrl).ToString();
        return await client.SendRequestAsync(url, method, jsonBody);
    }
}

