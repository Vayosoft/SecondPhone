using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Identity.Tokens;
using Vayosoft.Persistence.EntityFramework;
using UserEntity = EmulatorHub.Domain.Entities.UserEntity;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class UserEntityMap : EntityConfigurationMapper<UserEntity>
    {
        public override void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder
                .Property(t => t.Username); //read_only field

            builder
                .HasIndex(u => new {u.Username, u.ProviderId}).IsUnique().HasFilter("NOT SoftDeleted");
            builder.HasIndex(p => p.SoftDeleted);

            builder.HasQueryFilter(p => !p.SoftDeleted);

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
                    ProviderId = 1,
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
}
