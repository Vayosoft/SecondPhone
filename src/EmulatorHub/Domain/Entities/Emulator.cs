using Vayosoft.Commons.Entities;

namespace EmulatorHub.Domain.Entities
{
    public class Emulator : EntityBase<string>, IProviderId<long>
    {
        public string Name { get; set; }
        public MobileClient Client { get; set; } = null!;

        public long ProviderId { get; set; }
        object IProviderId.ProviderId => ProviderId;
    }
}
