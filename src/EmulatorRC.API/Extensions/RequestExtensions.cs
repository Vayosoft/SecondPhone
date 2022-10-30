namespace EmulatorRC.API.Extensions
{
    public static class RequestExtensions
    {
        public static string GetDeviceId(this HttpRequest request)
        {
            var deviceId = request.Headers["X-DEVICE-ID"].FirstOrDefault("DEFAULT");
            //TryGetValue("X-DEVICE-ID", out deviceId);
            //_memoryCache.Set(deviceId, "{}", new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(90) });
            return deviceId;
        }
    }
}
