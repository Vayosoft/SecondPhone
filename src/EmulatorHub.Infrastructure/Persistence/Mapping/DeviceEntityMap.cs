using EmulatorHub.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vayosoft.Persistence.EntityFramework;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class DeviceEntityMap : EntityConfigurationMapper<DeviceEntity>
    {
        public override void Configure(EntityTypeBuilder<DeviceEntity> builder)
        {
   

        }
    }


}
