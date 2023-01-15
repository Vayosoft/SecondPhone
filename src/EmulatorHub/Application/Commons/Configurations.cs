using Microsoft.Extensions.DependencyInjection;
using Vayosoft.SmsBrokers;

namespace EmulatorHub.Application.Commons
{
    public static class Configurations
    {
        public static IServiceCollection AddSmsService(this IServiceCollection services)
        {
            services.AddSmsBrokers();
            return services;
        }
    }
}
