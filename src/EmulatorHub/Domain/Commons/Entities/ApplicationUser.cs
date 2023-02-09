using Vayosoft.Identity;

namespace EmulatorHub.Domain.Commons.Entities
{
    public sealed class ApplicationUser : UserEntity
    {
        public ApplicationUser(string username)
            : base(username)
        {

        }
    }
}
