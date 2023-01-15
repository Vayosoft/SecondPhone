using EmulatorHub.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using Vayosoft;
using Vayosoft.Caching;
using Vayosoft.EntityFramework.MySQL;
using Vayosoft.Identity;
using Vayosoft.Identity.EntityFramework;
using Vayosoft.Persistence;
using Vayosoft.Redis;
using EmulatorHub.Application.Commons.Services;
using EmulatorHub.Application.Commons;
using EmulatorHub.Application.PushGateway;
using EmulatorHub.Application.PushGateway.Commands;

namespace EmulatorHub.Infrastructure
{
    public static class Configuration
    {
        public static IServiceCollection AddHubApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor()
                .AddScoped<IUserContext, UserContext>();

            services.AddCoreServices();
            services.AddValidation();

            services
                .AddRedisConnection()
                .AddCaching(configuration);

            services.AddHubDataContext(configuration);
            services.AddPushService();
            services.AddSmsService();

            return services;
        }

        private static IServiceCollection AddHubDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMySqlContext<HubDbContext>(configuration);
            services
                .AddScoped<IUnitOfWork>(s => s.GetRequiredService<HubDbContext>())
                .AddScoped<IDataProvider>(s => s.GetRequiredService<HubDbContext>())
                .AddScoped<ILinqProvider>(s => s.GetRequiredService<HubDbContext>());

            services.AddMySqlContext<IdentityContext>(configuration);

            return services;
        }

        private static void AddValidation(this IServiceCollection services)
        {
            //services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<SetProduct.CertificateRequestValidator>())
            services.AddValidatorsFromAssembly(Assembly.GetAssembly(typeof(SendPushMessage)), ServiceLifetime.Transient);
            //services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        }
    }
}
