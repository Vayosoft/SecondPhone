using Vayosoft.Identity;

namespace EmulatorHub.Application.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;

        public UserService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public Task<UserEntity> GetUsersAsync()
        {
            throw new NotImplementedException();
        }
    }
}
