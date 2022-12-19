using EmulatorHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EmulatorHub.MySqlMigrations
{
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<HubDbContext>
    {
        public HubDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();

            var connectionString = config["cs"] ?? "Server=localhost;Port=3306;Database=emurc;Uid=root;Pwd=1q2w3e4r;";
            //var connectionString = config["cs"] ?? "Server=192.168.10.11;Port=3306;Database=emurc;Uid=admin;Pwd=~1q2w3e4r!";
            var serverVersion = new MySqlServerVersion(config["ver"] ?? "8.0.25");

            var dbContextOptions = new DbContextOptionsBuilder<HubDbContext>();
            dbContextOptions.UseMySql(connectionString, serverVersion, 
                b => b.MigrationsAssembly("EmulatorHub.MySqlMigrations"))

                .UseSnakeCaseNamingConvention()
            ;

            return new HubDbContext(dbContextOptions.Options, null);
        }
    }
}