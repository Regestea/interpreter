using System.Text;
using System.Text.Json;

namespace interpreter.Maui.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var payload = new { username, password };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Adjust to match your API login endpoint
        var response = await _httpClient.PostAsync("auth/login", content);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var body = await response.Content.ReadAsStringAsync();

        // Adjust property names to match your API's JWT response contract
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var token = root.GetProperty("token").GetString();

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        await SecureStorage.SetAsync("authToken", token);
        return true;
    }

    public Task LogoutAsync()
    {
        SecureStorage.Remove("authToken");
        return Task.CompletedTask;
    }

    public async Task<string?> GetTokenAsync()
    {
        return await SecureStorage.GetAsync("authToken");
    }
}

