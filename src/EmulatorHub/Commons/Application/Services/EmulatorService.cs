using EmulatorHub.Commons.Domain.Entities;
using System.Text.Json;
using Vayosoft.Identity;

namespace EmulatorHub.Commons.Application.Services
{
    public class EmulatorService
    {
        private readonly HttpClient _httpClient;

        public EmulatorService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<Emulator>> GetEmulatorsAsync()
        {
            var response = await _httpClient.GetAsync("http://localhost:5005/api/emulators");
            var headers = response.ToString();
            var body = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<List<Emulator>>(body);
            return data;
        }
    }
}
