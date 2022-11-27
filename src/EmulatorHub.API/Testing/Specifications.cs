using EmulatorHub.Domain.Entities;
using Vayosoft.Persistence.Criterias;
using Vayosoft.Persistence.Specifications;

namespace EmulatorHub.API.Testing
{
    public class UserByTokenCriteria : Criteria<UserEntity>
    {
        public UserByTokenCriteria(string token)
        {
            Include(u => u.RefreshTokens);
            Where(u => u.RefreshTokens!.Any(t => t.Token == token));
        }
    }

    public class UserByNameCriteria : Criteria<UserEntity>
    {
        public UserByNameCriteria(string name)
        {
            Where(u => u.Username == name);
        }
    }

    public class GetAllUsersSpec : Specification<UserEntity>
    {
        public GetAllUsersSpec(string? token = default, string? username = default)
        {
            //Where(new UserByTokenCriteria(token) && new UserByNameCriteria(username));

            Include(u => u.RefreshTokens);
            //Where(u => u.Username == username && u.RefreshTokens!.Any(t => t.Token == token));

            OrderBy(u => u.Username);
        }
    }
}
