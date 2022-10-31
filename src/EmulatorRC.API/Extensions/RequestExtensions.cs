namespace EmulatorRC.API.Extensions
{
    public static class RequestExtensions
    {
        public static string? GetDeviceIdOrDefault(this HttpRequest request, string? defaultValue = null)
        {
            var deviceId = request.Headers["X-DEVICE-ID"].FirstOrDefault(defaultValue);
            //TryGetValue("X-DEVICE-ID", out deviceId);
            //_memoryCache.Set(deviceId, "{}", new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(90) });
            return deviceId;
        }
    }
}
