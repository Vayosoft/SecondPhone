using EmulatorHub.Application.Services;
using EmulatorHub.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence.Filters;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Commons.Entities;
using Vayosoft.Commons.Models;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;
using Vayosoft.Persistence.EF.MySQL;

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

        //public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<Entity> Users { set; get; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //foreach (var mutableEntityType in modelBuilder.Model.GetEntityTypes())
            //{
            //    if (typeof(ISoftDelete).IsAssignableFrom(mutableEntityType.ClrType)) 
            //        mutableEntityType.AddSoftDeleteQueryFilter();

            //    if (_userContext != null)
            //    {
            //        if (typeof(IProviderId).IsAssignableFrom(mutableEntityType.ClrType))
            //            mutableEntityType.AddProviderIdQueryFilter(_userContext);
            //    }
            //}

            var providerId = _userContext?.User.Identity.GetProviderId() ?? 0;
            modelBuilder
                .Entity<TestEntity>()
                .HasQueryFilter(p => p.ProviderId == providerId);
        }
    }
}
