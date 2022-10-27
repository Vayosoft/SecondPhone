using System.Net;
using Vayosoft.Core.SharedKernel.Exceptions;

namespace EmulatorRC.API.Errors
{
    public record HttpStatusCodeInfo(HttpStatusCode Code, string Message)
    {
        public static HttpStatusCodeInfo Create(HttpStatusCode code, string message) =>
            new(code, message);
    }

    public static class HttpStatusCodeInfoExtensions
    {
        public static HttpStatusCodeInfo GetHttpStatusCodeInfo(this Exception exception)
        {
            var code = exception switch
            {
                UnauthorizedAccessException _ => HttpStatusCode.Unauthorized,
                NotImplementedException _ => HttpStatusCode.NotImplemented,
                InvalidOperationException _ => HttpStatusCode.Conflict,
                ArgumentException _ => HttpStatusCode.BadRequest,
                ValidationException _ => HttpStatusCode.BadRequest,
                AggregateNotFoundException _ => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };

            return new HttpStatusCodeInfo(code, exception.Message);
        }
    }
}
