using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vayosoft.Testing;
using Xunit.Abstractions;

namespace EmulatorRC.IntegrationTests
{
    public class ConnectionTests
    {
        private readonly ITestOutputHelper _helper;

        public ConnectionTests(ITestOutputHelper helper)
        {
            _helper = helper;
        }

        [Fact]
        public async Task EchoTest()
        {
            var cts = new CancellationTokenSource(20000);
            var builder = new WebHostBuilder();
            builder
                .UseKestrel(options =>
                {
                    options.ListenLocalhost(5001);
                    options.ListenLocalhost(5000, listenOptions =>
                    {
                        listenOptions.UseConnectionHandler<EchoConnectionHandler>();
                    });
                })
                .ConfigureServices(services =>
                {
                    
                })
                .Configure(app =>
                {
                    app.Run(async (context) =>
                    {
                        await context.Response.WriteAsync("Test", CancellationToken.None);
                    });
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddProvider(new XUnitLoggerProvider(_helper));
                });

            var app = builder.Build();
            await app.RunAsync(token: cts.Token);
        }

        public class EchoConnectionHandler : ConnectionHandler
        {
            private readonly ILogger<EchoConnectionHandler> _logger;

            public EchoConnectionHandler(ILogger<EchoConnectionHandler> logger)
            {
                _logger = logger;
            }

            public override async Task OnConnectedAsync(ConnectionContext connection)
            {
                _logger.LogInformation("{connectionId} connected", connection.ConnectionId);

                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync();
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        await connection.Transport.Output.WriteAsync(segment);
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    connection.Transport.Input.AdvanceTo(buffer.End);
                }

                _logger.LogInformation("{connectionId} disconnected", connection.ConnectionId);
            }
        }
    }
}
