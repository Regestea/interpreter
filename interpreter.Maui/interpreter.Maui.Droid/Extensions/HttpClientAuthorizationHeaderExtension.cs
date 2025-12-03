using System.Net.Http.Headers;

namespace interpreter.Maui.Extensions
{
    public static class HttpClientAuthorizationHeaderExtension
    {
        public static async Task<HttpClient> AddAuthHeader(this HttpClient httpClient)
        {
            var authToken = await SecureStorage.GetAsync("authToken");

            if (!string.IsNullOrEmpty(authToken))
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", authToken);
                }
                catch
                {
                    SecureStorage.Remove("authToken");
                }
            }

            return httpClient;
        }
    }
}