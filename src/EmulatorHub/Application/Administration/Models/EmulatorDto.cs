using EmulatorHub.Domain.Commons.Entities;
using Vayosoft.Commons.Entities;
using Vayosoft.Mapping;

namespace EmulatorHub.Application.Administration.Models
{
    [ConventionalMap(typeof(Emulator), direction: MapDirection.EntityToDto)]
    public record EmulatorDto : IEntity<string>
    {
        object IEntity.Id => Id;
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
