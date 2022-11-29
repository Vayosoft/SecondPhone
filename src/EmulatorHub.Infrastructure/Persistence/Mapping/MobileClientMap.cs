using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EmulatorHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Persistence.EntityFramework;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class MobileClientMap : EntityConfigurationMapper<MobileClient>
    {
        public override void Configure(EntityTypeBuilder<MobileClient> builder)
        {
            builder
                .HasIndex(u => new { u.Id, u.ProviderId }).IsUnique().HasFilter("NOT SoftDeleted");

            builder.HasIndex(p => p.SoftDeleted);

            builder.HasQueryFilter(p => !p.SoftDeleted);
        }
    }
}
