using EmulatorHub.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vayosoft.EF.MySQL;
using Vayosoft.Persistence;

namespace EmulatorHub.Infrastructure
{
    public static class Configuration
    {
        public static IServiceCollection AddHubDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMySqlContext<HubDbContext>(configuration);
            services
                .AddScoped<IUnitOfWork>(s => s.GetRequiredService<HubDbContext>())
                .AddScoped<ILinqProvider>(s => s.GetRequiredService<HubDbContext>());

            return services;
        }
    }
}
