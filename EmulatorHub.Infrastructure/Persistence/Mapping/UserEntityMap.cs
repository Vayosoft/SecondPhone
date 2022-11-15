using EmulatorHub.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vayosoft.EF.MySQL;
using Vayosoft.Identity;
using Vayosoft.Identity.Tokens;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class RefreshTokenMap : EntityConfigurationMapper<RefreshToken>
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
                        Registered = DateTime.UtcNow,
                        CultureId = "ru-RU",
                        ProviderId = 1000,
                    }
                );
            }
        }

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
}
