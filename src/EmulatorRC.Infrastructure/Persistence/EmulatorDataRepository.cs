using System.Data;
using Dapper;
using EmulatorRC.Entities;
using EmulatorRC.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace EmulatorRC.Infrastructure.Persistence
{
    public sealed class EmulatorDataRepository : IEmulatorDataRepository
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceScope _scope;

        public EmulatorDataRepository(IServiceProvider serviceProvider, IMemoryCache memoryCache)
        {
            _scope = serviceProvider.CreateScope();
            _memoryCache = memoryCache;
        }

        private const string GetDeviceSql = $"select id, name, client_id as {nameof(Emulator.ClientId)}, provider_id as {nameof(Emulator.ProviderId)} from devices where client_id = @id";
        public async Task<Emulator?> GetByClientIdAsync(string clientId)
        {
            if (!_memoryCache.TryGetValue($"device#{clientId}", out Emulator? device));
            {
                var connection = _scope.ServiceProvider.GetRequiredService<IDbConnection>();
                using (connection)
                {
                    device = await connection.QuerySingleAsync<Emulator>(GetDeviceSql, new { id = clientId });
                }

                _memoryCache.Set($"device#{clientId}", device, TimeSpan.FromMinutes(1));
            }
            
            return device;
        }
    }
}

