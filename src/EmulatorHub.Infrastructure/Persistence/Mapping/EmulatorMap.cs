using EmulatorHub.Commons.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vayosoft.Persistence.EntityFramework;

namespace EmulatorHub.Infrastructure.Persistence.Mapping
{
    public partial class EmulatorMap : EntityConfigurationMapper<Emulator>
    {
        public override void Configure(EntityTypeBuilder<Emulator> builder)
        {
   

        }
    }


}
