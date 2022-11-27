using Vayosoft.Commons.Entities;

namespace EmulatorHub.Domain.Entities
{
    public class RemoteDevice : EntityBase<long>
    {
        public string? Name { get; set; }
        public DateTime? Registered { get; set; }
        public RemoteClient Client { get; set; } = null!;

        public long ProviderId { get; set; }
    }
}
