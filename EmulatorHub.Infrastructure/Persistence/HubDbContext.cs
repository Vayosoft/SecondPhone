using EmulatorHub.Entities;
using Microsoft.EntityFrameworkCore;
using Vayosoft.EF.MySQL;

namespace EmulatorHub.Infrastructure.Persistence
{
    public sealed class HubDbContext : DataContext
    {
        public HubDbContext(DbContextOptions options) : base(options) { }

        public DbSet<UserEntity> Users => Set<UserEntity>();
        //public DbSet<UserEntity> Users { set; get; } = null!;
    }
}
