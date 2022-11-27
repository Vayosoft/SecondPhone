using Vayosoft.Commons.Entities;
using Vayosoft.Commons.Enums;
using Vayosoft.Identity;
using Vayosoft.Identity.Tokens;
using Vayosoft.Utilities;

namespace EmulatorHub.Domain.Entities
{
    public class UserEntity : EntityBase<long>, IUser, IProviderId<long>, ISoftDelete
    {
        private UserEntity() { }
        public UserEntity(string username)
        {
            Username = Guard.NotEmpty(username);
        }

        public string Username { get; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public UserType Type { get; set; }
        public DateTime? Registered { get; set; }
        public DateTime? Deregistered { get; set; }
        public string CultureId { get; set; } = null!;
        public long ProviderId { get; set; }
        object IProviderId.ProviderId => ProviderId;
        public LogEventType? LogLevel { get; set; }
        public List<RefreshToken>? RefreshTokens { get; set; }

        public string? PushToken { get; set; }
        public bool SoftDeleted { get; set; }
    }
}
