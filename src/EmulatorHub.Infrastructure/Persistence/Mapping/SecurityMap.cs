using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vayosoft.Identity.Security;
using Vayosoft.Persistence.EntityFramework;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class SecurityObjectMap : EntityConfigurationMapper<SecurityObjectEntity>
    {
        public override void Configure(EntityTypeBuilder<SecurityObjectEntity> builder)
        {
            builder.ToTable("sec_objs").HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("objid").ValueGeneratedOnAdd();
            builder.Property(t => t.Name).HasColumnName("obj_name").IsRequired();
            builder.Property(t => t.Description).HasColumnName("obj_desc");
        }
    } 
    
    public partial class SecurityRoleMap : EntityConfigurationMapper<SecurityRoleEntity>
    {
        public override void Configure(EntityTypeBuilder<SecurityRoleEntity> builder)
        {
            builder.ToTable("sec_roles").HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("roleid").ValueGeneratedOnAdd();
            builder.Property(t => t.ProviderId).HasColumnName("providerid");
            builder.Property(t => t.Name).HasColumnName("role_name").IsRequired();
            builder.Property(t => t.Description).HasColumnName("role_desc");

            builder.HasData(new List<SecurityRoleEntity>
            {
                new()
                {
                    Id = "f6694d71d26e40f5a2abb357177c9bdt",
                    Name = "Support",
                },
                new()
                {
                    Id = "f6694d71d26e40f5a2abb357177c9bdx",
                    Name = "Administrator",
                },
                new()
                {
                    Id = "f6694d71d26e40f5a2abb357177c9bdz",
                    Name = "Supervisor",
                },
            });
        }
    }

    public partial class SecurityRolePermissionsMap : EntityConfigurationMapper<SecurityRolePermissionsEntity>
    {
        public override void Configure(EntityTypeBuilder<SecurityRolePermissionsEntity> builder)
        {
            builder.ToTable("sec_role_permissions").HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("permid").ValueGeneratedOnAdd();
            builder.Property(t => t.RoleId).HasColumnName("roleid").IsRequired();
            builder.Property(t => t.ObjectId).HasColumnName("objid").IsRequired();
            builder.Property(t => t.Permissions).HasColumnName("perms").IsRequired();
        }
    }

    public partial class UserRoleMap : EntityConfigurationMapper<UserRoleEntity>
    {
        public override void Configure(EntityTypeBuilder<UserRoleEntity> builder)
        {
            builder.ToTable("sec_user_roles").HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("urid").ValueGeneratedOnAdd();
            builder.Property(t => t.RoleId).HasColumnName("userid").IsRequired();
            builder.Property(t => t.RoleId).HasColumnName("roleid").IsRequired();

            builder.HasData(new UserRoleEntity
            {
                Id = "0e5085516ee34d4bab806757e41f6dd6",
                UserId = 1,
                RoleId = "f6694d71d26e40f5a2abb357177c9bdz"
            });
        }
    }

}
