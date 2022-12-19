using System.Net;
using System.Net.Sockets;
using System.Text;
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
        public async Task EchoClient()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            using var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5000), token);

            //https://habr.com/en/company/microsoft/blog/423105/
            async Task OnReceive(Socket socket, CancellationToken cancellationToken)
            {
                try
                {
                    var buffer = new byte[1024];
                    int bytes;
                    while ((bytes = await socket.ReceiveAsync(buffer, cancellationToken: cancellationToken)) > 0)
                    {
                        var response = Encoding.UTF8.GetString(buffer, 0, bytes);
                        _helper.WriteLine(response);
                    }
                }
                catch (OperationCanceledException){ }
            }

            async Task Send(Socket socket)
            {
                await using var stream = new NetworkStream(socket);
                var data = "1234567890\n"u8.ToArray();

                for (var i = 0; i < 10; i++)
                {
                    using var memory = new MemoryStream(data);
                    await memory.CopyToAsync(stream, token);
                }
            }

            var receiving = OnReceive(clientSocket, token);
            await Send(clientSocket);
            cts.Cancel();
            await receiving;
            cts.Dispose();
        }

        [Fact]
        public async Task EchoServer()
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
