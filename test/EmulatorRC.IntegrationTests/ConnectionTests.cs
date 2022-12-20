using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Commons.Core.Cryptography;
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
        private readonly ITestOutputHelper _logger;

        public ConnectionTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        private const string SourceFilePath = "../../../data/Smith_CleanArchAspNetCore.pptx";
        private const string DestinationFilePath = "../../../data/Smith_CleanArchAspNetCore_Copy.pptx";

        [Fact]
        public async Task EchoClient()
        {
            var sourceFileLength = new FileInfo(SourceFilePath).Length;
            using var cts = new CancellationTokenSource(5000);
            var cancellationToken = cts.Token;

            using var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5000), cancellationToken);

            async Task ReceiveAsync(Socket socket, CancellationToken token)
            {
                var stopwatch = new Stopwatch();
                long totalLength = 0;
                try
                {
                    int bytesRead;
                    var buffer = new byte[4096];
                    //await using var networkStream = new NetworkStream(socket);
                    await using var fileStream =
                        new FileStream(DestinationFilePath, FileMode.OpenOrCreate, FileAccess.Write);
                    //while ((bytesRead = await networkStream.ReadAsync(buffer, token)) > 0)
                    stopwatch.Start();
                    while ((bytesRead = await socket.ReceiveAsync(buffer, token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                        totalLength += bytesRead;
                        if (totalLength != sourceFileLength) continue;
                        break;
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    stopwatch.Stop();
                }

                var bps = Math.Round((double)totalLength / 1024 / 1024 / stopwatch.Elapsed.TotalSeconds, 2);
                _logger.WriteLine("Elapsed: {0} (~{1} MB/sec)", stopwatch.Elapsed, bps);
            }

            async Task SendAsync(Socket socket, CancellationToken token)
            {
                try
                {
                    int bytesRead;
                    var buffer = new byte[4096];
                    //await using var networkStream = new NetworkStream(socket);
                    await using var fileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read);
                    while ((bytesRead = await fileStream.ReadAsync(buffer, token)) > 0)
                    {
                        //await networkStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                        await socket.SendAsync(buffer.AsMemory(0, bytesRead), token);
                    }
                }
                catch (OperationCanceledException){ }
            }

            await Task.WhenAll(ReceiveAsync(clientSocket, cancellationToken), SendAsync(clientSocket, cancellationToken));

            Assert.Equal(new FileInfo(SourceFilePath).MD5(), new FileInfo(DestinationFilePath).MD5());

            File.Delete(DestinationFilePath);
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
                    loggingBuilder.AddProvider(new XUnitLoggerProvider(_logger));
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
                    var result = await connection.Transport.Input.ReadAsync(connection.ConnectionClosed);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        await connection.Transport.Output.WriteAsync(segment, connection.ConnectionClosed);
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
