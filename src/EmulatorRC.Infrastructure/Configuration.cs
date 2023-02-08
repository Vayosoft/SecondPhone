using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Vayosoft.Dapper.MySQL;

namespace EmulatorRC.Infrastructure
{
    public static class Configuration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<DbConnection>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddTransient<IDbConnection, MySqlConnection>(_ => new MySqlConnection(connectionString));

            return services;
        }
    }
}