using Microsoft.AspNetCore.Http;

namespace EmulatorHub.Application.Services
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpAccessor;

        public UserContext(IHttpContextAccessor httpAccessor)
        {
            _httpAccessor = httpAccessor;
        }

        public long GetProviderId()
        {
            return 1;
        }
    }
}
