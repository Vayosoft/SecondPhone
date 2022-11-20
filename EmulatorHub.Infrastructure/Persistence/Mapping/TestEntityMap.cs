using EmulatorHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vayosoft.Persistence.EF.MySQL;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class TestEntityMap : EntityConfigurationMapper<TestEntity>
    {
        public override void Configure(EntityTypeBuilder<TestEntity> builder)
        {
            builder.HasKey(t => new { t.Id, t.Timestamp });
            builder.Property(t => t.Id).UseMySqlIdentityColumn(); //.ValueGeneratedOnAdd()

            builder.Property(t => t.RegisteredDate).HasColumnType("DATE");

            builder.Property(t => t.Name).HasMaxLength(50);
            builder.Property(t => t.Alias).IsUnicode(false).HasMaxLength(50);
            builder.Property(p => p.Double).HasPrecision(8, 2); //default decimal(18,2)
            builder.Property(p => p.Enum).HasConversion<string>();

            //Shadow field
            //builder.Property<DateTime>("UpdatedOn");
            //context.Entry(entity).Property("UpdatedOn").CurrentValue = DateTime.Now;
            //reading => without .AsNoTracking()
            //linq => EF.Property<DateTime>(b, "UpdateOn"))

            //Private field
            //builder.Property("_dateOfBirth").HasColumnName("DateOfBirth");

            builder.HasIndex(p => new { p.Name, p.ProviderId }).IsUnique().HasFilter("NOT SoftDeleted");

            builder
                .HasQueryFilter(p => !p.SoftDeleted);
        }
    }
}
