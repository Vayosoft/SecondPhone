using Vayosoft.Commons.Entities;

namespace EmulatorHub.Domain.Commons.Entities
{
    public class MobileClient : EntityBase<string>, IProviderId<long>, ISoftDelete
    {
        public string Name { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public string PushToken { get; set; }

        public long ProviderId { get; set; }
        object IProviderId.ProviderId => ProviderId;

        public bool SoftDeleted { get; set; }
    }
}
