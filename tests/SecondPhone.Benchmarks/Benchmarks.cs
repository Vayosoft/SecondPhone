using BenchmarkDotNet.Attributes;
using EmulatorHub.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SecondPhone.Benchmarks
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        private static UserService UserService { get; }

        static Benchmarks()
        {
            var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpClient();
                    services.AddTransient<UserService>();
                })
                .Build();

            UserService = host.Services.GetRequiredService<UserService>();
        }


    }
}
