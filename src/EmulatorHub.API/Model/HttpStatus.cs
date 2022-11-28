using System.Net;

namespace EmulatorHub.API.Model
{
    public static class HttpStatusExtensions
    {
        public static HttpStatusCode ToHttpStatusCode(this Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException _ => HttpStatusCode.Unauthorized,
                NotImplementedException _ => HttpStatusCode.NotImplemented,
                InvalidOperationException _ => HttpStatusCode.Conflict,
                ArgumentException _ => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };
        }
    }
}
