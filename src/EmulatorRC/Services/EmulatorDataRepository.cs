using Microsoft.Extensions.Caching.Memory;

namespace EmulatorRC.Services
{
    public class EmulatorDataRepository : IEmulatorDataRepository
    {
        private readonly IMemoryCache _memoryCache;

        public EmulatorDataRepository(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;

        }

        public string? GetLastScreenId(string deviceId)
        {
            return _memoryCache.Get<string>($"{deviceId}->LastScreenId");
        }

        public Screen? GetLastScreen(string deviceId)
        {
            return _memoryCache.Get<Screen>($"{deviceId}->LastScreen");
        }

        public byte[]? GetScreen(string deviceId, string id)
        {
            return _memoryCache.Get<byte[]>($"{deviceId}->{id}.jpg");
        }

        public void SetLastScreenId(string deviceId, string id)
        {
            _memoryCache.Set($"{deviceId}->LastScreenId", id);
        }

        public void SetLastScreen(string deviceId, Screen screen)
        {
            _memoryCache.Set($"{deviceId}->LastScreen", screen);
        }

        public void SetScreen(string deviceId, string id, byte[] screen)
        {
            _memoryCache.Set($"{deviceId}->{id}.jpg", screen, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) });
        }
    }

    public record Screen(string Id, byte[] Image);
}

