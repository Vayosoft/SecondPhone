using EmulatorHub.Domain.Commons.Entities;
using Vayosoft.Commons.Entities;
using Vayosoft.Mapping;

namespace EmulatorHub.Application.ClientContext.Models
{
    [ConventionalMap(typeof(MobileClient), direction: MapDirection.EntityToDto)]
    public record MobileClientDto : IEntity<string>
    {
        object IEntity.Id => Id;
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
