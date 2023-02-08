using Grpc.Core;

namespace EmulatorRC.API.Extensions
{
    public static class RequestExtensions
    {
        public static string GetDeviceIdOrDefault(this HttpContext httpContext, string defaultValue = null)
        {
            return httpContext.Request.Headers["X-DEVICE-ID"].FirstOrDefault(defaultValue);
        }

        public static string GetDeviceIdOrDefault(this ServerCallContext context, string defaultValue = null)
        {
            return context.GetHttpContext().GetDeviceIdOrDefault(defaultValue);
        }

        public static string GetClientId(this ServerCallContext context)
        {
            return context.GetHttpContext().Request.Headers["X-CLIENT-ID"].FirstOrDefault();
        }
    }
}
