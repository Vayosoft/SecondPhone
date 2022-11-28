using Vayosoft.Commons.Entities;
using Vayosoft.Identity;

namespace EmulatorHub.Domain.Entities
{
    public class ClientEntity : EntityBase<string>, IProviderId<long>, ISoftDelete
    {
        public string? Name { get; set; }
        public UserEntity User { get; set; } = null!;
        public string? PushToken { get; set; }
      
        public long ProviderId { get; set; }
        object IProviderId.ProviderId => ProviderId;
        
        public bool SoftDeleted { get; set; }
    }
}
