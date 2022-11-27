using Vayosoft.Commons.Entities;
using Vayosoft.Identity;

namespace EmulatorHub.Domain.Entities
{
    public class RemoteClient : UserEntity, ISoftDelete
    {
        public RemoteClient(string username) 
            : base(username)
        {
        }

        public string? PushToken { get; set; }
        public bool SoftDeleted { get; set; }
    }
}
