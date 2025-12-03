using System.Net;

namespace interpreter.Maui.DTOs
{
    public class HttpResponseDto
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Content { get; set; } = null!;
    }
}