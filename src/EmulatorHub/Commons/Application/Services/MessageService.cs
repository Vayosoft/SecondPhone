using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EmulatorHub.Commons.Application.Services
{
    public class MessageService
    {
        private readonly HttpClient _httpClient;

        public MessageService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> SendPushMessage(string deviceId, object message)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("http://localhost:5005/api/messages/push/send"),
                Method = HttpMethod.Post,
                
            };
            request.Headers.Add("x-device-id", deviceId);

            request.Content = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            var headers = response.ToString();
            var body = await response.Content.ReadAsStringAsync();
            return body;
        }
    }
}
