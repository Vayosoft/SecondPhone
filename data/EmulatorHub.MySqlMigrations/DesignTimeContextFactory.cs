using EmulatorHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmulatorHub.MySqlMigrations
{
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<HubDbContext>
    {
        public HubDbContext CreateDbContext(string[] args)
        {
            var dbContextOptions = new DbContextOptionsBuilder();

            var connectionString = "Server=localhost;Port=3306;Database=emurc;Uid=root;Pwd=1q2w3e4r;";
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 25));

            dbContextOptions.UseMySql(connectionString, serverVersion, 
                b => b.MigrationsAssembly("EmulatorHub.MySqlMigrations"));

            return new HubDbContext(dbContextOptions.Options);
        }
    }
}