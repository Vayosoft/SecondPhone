using BenchmarkDotNet.Attributes;
using EmulatorHub.Commons.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class HttpBenchmarks
    {
        private static MessageService UserService { get; }

        static HttpBenchmarks()
        {
            var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpClient();
                    services.AddTransient<MessageService>();
                })
                .Build();

            UserService = host.Services.GetRequiredService<MessageService>();
        }


    }
}
