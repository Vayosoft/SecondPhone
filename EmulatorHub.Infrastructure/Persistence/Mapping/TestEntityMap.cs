using EmulatorHub.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Persistence.EF.MySQL;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class TestEntityMap : EntityConfigurationMapper<TestEntity>
    {
        public override void Configure(EntityTypeBuilder<TestEntity> builder)
        {
            builder.HasKey(t => new { t.Id, t.Timestamp });
            builder.Property(t => t.Id).UseMySqlIdentityColumn(); //.ValueGeneratedOnAdd()

            builder.Property(t => t.Timestamp);
            builder.Property(t => t.RegisteredDate).HasColumnType("DATE");

            builder.Property(t => t.Name).HasMaxLength(50);
            builder.Property(t => t.Alias).IsUnicode(false).HasMaxLength(50);
            builder.Property(p => p.Double).HasPrecision(8,2);

            builder
                .HasQueryFilter(p => !p.SoftDeleted);
        }
    }
}
