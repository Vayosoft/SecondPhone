using BenchmarkDotNet.Attributes;
using EmulatorHub.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class HttpBenchmarks
    {
        private static EmulatorService UserService { get; }

        static HttpBenchmarks()
        {
            var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpClient();
                    services.AddTransient<EmulatorService>();
                })
                .Build();

            UserService = host.Services.GetRequiredService<EmulatorService>();
        }


    }
}
