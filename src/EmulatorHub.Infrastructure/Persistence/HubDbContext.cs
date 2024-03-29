﻿using EmulatorHub.Commons.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;
using Vayosoft.Persistence.EntityFramework;

namespace EmulatorHub.Infrastructure.Persistence
{
    public sealed class HubDbContext : DataContext
    {
        private readonly IUserContext? _userContext;
        public HubDbContext(DbContextOptions<HubDbContext> options, IUserContext? userContext = null) 
            : base(options)
        {
            _userContext = userContext;
        }

        public DbSet<UserEntity> Users { set; get; } = null!;
        public DbSet<Emulator> Devices { set; get; } = null!;
        public DbSet<MobileClient> Clients { set; get; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var providerId = _userContext?.User?.Identity.GetProviderId() ?? 0;
            modelBuilder
                .Entity<Emulator>()
                .HasQueryFilter(p => p.ProviderId == providerId)
                .HasIndex(p => p.ProviderId);
        }
    }
}
