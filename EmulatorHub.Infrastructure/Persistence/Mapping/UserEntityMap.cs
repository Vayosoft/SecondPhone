using EmulatorHub.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Identity.Tokens;
using Vayosoft.Persistence.EF.MySQL;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class UserEntityMap : EntityConfigurationMapper<UserEntity>
    {
        public override void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder
                .Property(t => t.Username);

            builder
                .HasIndex(u => u.Username).IsUnique();

            builder.HasData(
                new UserEntity("su")
                {
                    Id = 1,
                    PasswordHash = "VBbXzW7xlaD3YiqcVrVehA==",
                    Phone = "0500000000",
                    Email = "su@vayosoft.com",
                    Type = UserType.Supervisor,
                    Registered = new DateTime(2022, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc),
                    CultureId = "ru-RU",
                    ProviderId = 1000,
                }
            );
        }
    }

    public partial class RefreshTokenMap : EntityConfigurationMapper<RefreshToken>
    {
        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder
                .HasKey(t => new { t.UserId, t.Token });
            builder
                .HasOne(t => t.User as UserEntity)
                .WithMany(t => t.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public partial class TestEntityMap : EntityConfigurationMapper<TestEntity>
    {
        public override void Configure(EntityTypeBuilder<TestEntity> builder)
        {
            builder.HasKey(t => new { t.Id, t.Timestamp });
            builder.Property(t => t.Id).UseMySqlIdentityColumn(); //.ValueGeneratedOnAdd()
            builder.Property(t => t.Timestamp);

            builder
                .HasQueryFilter(p => !p.SoftDeleted);
        }
    }
}
