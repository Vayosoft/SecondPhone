using EmulatorHub.Entities;
using Microsoft.EntityFrameworkCore;
using Vayosoft.EF.MySQL;

namespace EmulatorHub.Infrastructure.Persistence
{
    public sealed class HubDbContext : DataContext
    {
        public HubDbContext(DbContextOptions options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<UserEntity> Users => Set<UserEntity>();
    }
}
