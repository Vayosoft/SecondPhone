using System.Net;

namespace EmulatorRC.API.Model
{
    public record HttpStatus(HttpStatusCode Code, string Message)
    {
        public static HttpStatus Create(HttpStatusCode code, string message) =>
            new(code, message);
    }

    public static class HttpStatusCodeInfoExtensions
    {
        public static HttpStatus GetHttpStatus(this Exception exception)
        {
            var code = exception switch
            {
                UnauthorizedAccessException _ => HttpStatusCode.Unauthorized,
                NotImplementedException _ => HttpStatusCode.NotImplemented,
                InvalidOperationException _ => HttpStatusCode.Conflict,
                ArgumentException _ => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            return new HttpStatus(code, exception.Message);
        }
    }
}
