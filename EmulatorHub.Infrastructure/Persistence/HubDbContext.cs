using EmulatorHub.Application.Services;
using EmulatorHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Persistence.EF.MySQL;

namespace EmulatorHub.Infrastructure.Persistence
{
    public sealed class HubDbContext : DataContext
    {
        private readonly IUserContext? _userContext;
        public HubDbContext(DbContextOptions options, IUserContext? userContext = null) : base(options)
        {
            _userContext = userContext;
        }

        public DbSet<UserEntity> Users => Set<UserEntity>();
        //public DbSet<UserEntity> Users { set; get; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var providerId = _userContext?.GetProviderId() ?? 0;
            modelBuilder
                .Entity<TestEntity>()
                .HasQueryFilter(p => p.ProviderId == providerId);
        }
    }
}
