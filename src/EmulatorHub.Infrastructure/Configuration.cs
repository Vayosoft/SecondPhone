using EmulatorHub.Commons.Application.Services;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vayosoft.Caching;
using Vayosoft.EntityFramework.MySQL;
using Vayosoft.Identity;
using Vayosoft.Identity.EntityFramework;
using Vayosoft.Persistence;
using Vayosoft.PushBrokers;

namespace EmulatorHub.Infrastructure
{
    public static class Configuration
    {
        public static IServiceCollection AddApplicationCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCaching(configuration);

            return services;
        }

        public static IServiceCollection AddHubDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMySqlContext<HubDbContext>(configuration);
            services
                .AddScoped<IUnitOfWork>(s => s.GetRequiredService<HubDbContext>())
                .AddScoped<IDataProvider>(s => s.GetRequiredService<HubDbContext>())
                .AddScoped<ILinqProvider>(s => s.GetRequiredService<HubDbContext>());

            services.AddMySqlContext<IdentityContext>(configuration);

            return services;
        }

        public static IServiceCollection AddHubServices(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddScoped<IUserContext, UserContext>();
            services.AddPushBrokers();

            return services;
        }
    }
}
