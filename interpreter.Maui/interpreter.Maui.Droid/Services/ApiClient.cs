using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers; // برای MediaTypeHeaderValue
using interpreter.Maui.DTOs;
using interpreter.Maui.Extensions; // اگر اکستنشن‌های دیگری دارید نگه دارید

namespace interpreter.Maui.Services;

public interface IApiClient
{
    // پارامتر idempotencyKey به اینترفیس اضافه شد
    Task<HttpResponseDto> SendAsync(string relativeUrl, HttpMethod method, object? body = null, bool includeAuth = true, Guid? idempotencyKey = null);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseDto> SendAsync(string relativeUrl, HttpMethod method, object? body = null, bool includeAuth = true, Guid? idempotencyKey = null)
    {
        // ۱. ساخت کلید: اگر نال بود، یکی جدید بساز و تبدیل به استرینگ کن
        string keyToSend = idempotencyKey?.ToString() ?? Guid.NewGuid().ToString();

        // ۲. ساخت HttpRequestMessage (برای کنترل کامل روی هدرها)
        // ترکیب BaseAddress و relativeUrl به صورت خودکار توسط HttpClient انجام می‌شود
        var request = new HttpRequestMessage(method, relativeUrl);

        // ۳. افزودن هدر Idempotency به این ریکوئست خاص
        request.Headers.Add("IdempotencyKey", keyToSend);

        // ۴. افزودن بادی (در صورت وجود)
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        // ۵. افزودن Auth (جایگزین امن برای متد AddAuthHeader)
        if (includeAuth)
        {
            // نکته: بهتر است توکن را از سرویس احراز هویت خود بگیرید و اینجا به ریکوئست اضافه کنید.
            // اگر متد AddAuthHeader شما هدر را به _httpClient اضافه می‌کند، در محیط‌های چند نخی مشکل‌ساز است.
            // کد زیر یک مثال استاندارد است (شما می‌توانید لاجیک گرفتن توکن را جایگزین کنید):
            
            // var token = await _authService.GetTokenAsync();
            // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // اگر مجبورید از اکستنشن خودتان استفاده کنید، مطمئن شوید هدر را روی request می‌گذارد نه client
        }

        try
        {
            // ۶. ارسال درخواست
            var response = await _httpClient.SendAsync(request);

            // ۷. خواندن پاسخ
            var responseContent = await response.Content.ReadAsStringAsync();

            return new HttpResponseDto
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                Content = responseContent
                // اگر پراپرتی‌های دیگری در DTO دارید اینجا ست کنید
            };
        }
        catch (Exception ex)
        {
            // مدیریت خطا
            return new HttpResponseDto
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.InternalServerError,
                Content = ex.Message
            };
        }
    }
}