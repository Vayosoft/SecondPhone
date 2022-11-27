using EmulatorHub.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vayosoft.Persistence.EntityFramework;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class RemoteDeviceMap : EntityConfigurationMapper<RemoteDevice>
    {
        public override void Configure(EntityTypeBuilder<RemoteDevice> builder)
        {
   

        }
    }


}
