using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EmulatorHub.Commons.Application.Services.Commons
{
    public static class Server
    {
        public static IHost Host { get; } = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddHttpClient();
                services.AddTransient<EmulatorService>();
            })
            .Build();
    }
}
