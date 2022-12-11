using EmulatorHub.Commons.Application.Services;
using EmulatorHub.Infrastructure.Persistence;
using EmulatorHub.PushBroker;
using MediatR;
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
using Vayosoft.PushBrokers;
using EmulatorHub.PushBroker.Application.Commands;

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

            services.AddCoreServices();
            services.AddValidation();
            services.AddPushBrokerServices();

            return services;
        }

        public static void AddValidation(this IServiceCollection services)
        {
            //services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<SetProduct.CertificateRequestValidator>())
            services.AddValidatorsFromAssembly(Assembly.GetAssembly(typeof(SendPushMessage)), ServiceLifetime.Transient);
            //services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        }
    }
}
