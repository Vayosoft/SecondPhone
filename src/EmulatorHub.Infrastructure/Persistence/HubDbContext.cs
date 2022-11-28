using EmulatorHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;
using Vayosoft.Persistence.EntityFramework;

namespace EmulatorHub.Infrastructure.Persistence
{
    public sealed class HubDbContext : DataContext
    {
        private readonly IUserContext? _userContext;
        public HubDbContext(DbContextOptions options, IUserContext? userContext = null) 
            : base(options)
        {
            _userContext = userContext;
        }

        public DbSet<UserEntity> Users { set; get; } = null!;
        public DbSet<DeviceEntity> Devices { set; get; } = null!;
        public DbSet<ClientEntity> Clients { set; get; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var providerId = _userContext?.User?.Identity.GetProviderId() ?? 1;
            modelBuilder
                .Entity<DeviceEntity>()
                .HasQueryFilter(p => p.ProviderId == providerId)
                .HasIndex(p => p.ProviderId);
        }
    }
}
