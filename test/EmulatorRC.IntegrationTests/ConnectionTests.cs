using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Commons.Core.Cryptography;
using Commons.Core.Extensions;
using EmulatorRC.Entities;
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

            var client = new Client();
            await client.ConnectAsync(cancellationToken);

            var emulator = new Emulator(sourceFileLength);
            await emulator.ConnectAsync(cancellationToken);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await Task.WhenAll(emulator.StartAsync(cancellationToken), client.StartAsync(cancellationToken));
            stopwatch.Stop();

            Assert.Equal(new FileInfo(SourceFilePath).MD5(), new FileInfo(DestinationFilePath).MD5());

            var bps = Math.Round((double)sourceFileLength / 1024 / 1024 / stopwatch.Elapsed.TotalSeconds, 2);
            _logger.WriteLine("Elapsed: {0} ~{1} (MB/sec)", stopwatch.Elapsed, bps);

            File.Delete(DestinationFilePath);
        }

        public class Emulator
        {
            private readonly long _sourceFileLength;
            private readonly Socket _socket;
            public Emulator(long sourceFileLength)
            {
                _sourceFileLength = sourceFileLength;
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }

            public async Task ConnectAsync(CancellationToken cancellationToken)
            {
                await _socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5001), cancellationToken);
                await Handshake(_socket, cancellationToken);
            }

            private static async Task Handshake(Socket socket, CancellationToken token)
            {
                const string handshake = "CMD /v2/video.4?640x480&id=default";
                var header = Encoding.UTF8.GetBytes(handshake);

                await socket.SendAsync(header, token);
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return ReceiveAsync(_socket, cancellationToken);
            }

            private async Task ReceiveAsync(Socket socket, CancellationToken token)
            {
                long totalLength = 0;
                try
                {
                    int bytesRead;
                    var buffer = new byte[4096];
                    //await using var networkStream = new NetworkStream(socket);
                    await using var fileStream =
                        new FileStream(DestinationFilePath, FileMode.OpenOrCreate, FileAccess.Write);
                    //while ((bytesRead = await networkStream.ReadAsync(buffer, token)) > 0)

                    while ((bytesRead = await socket.ReceiveAsync(buffer, token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                        totalLength += bytesRead;
                        if (totalLength != _sourceFileLength) continue;
                        break;
                    }
                }
                catch (OperationCanceledException) { }
            }

            public void Dispose()
            {
                _socket.Disconnect(false);
                _socket.Dispose();
            }
        }

        public class Client : IDisposable
        {
            private readonly Socket _socket;

            public Client()
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }

            public async Task ConnectAsync(CancellationToken cancellationToken)
            {
                await _socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5000), cancellationToken);
                await Handshake(_socket, cancellationToken);
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return SendAsync(_socket, cancellationToken);
            }

            private static async Task Handshake(Socket socket, CancellationToken token)
            {
                var handshake = JsonSerializer.SerializeToUtf8Bytes(
                    new DeviceSession
                    {
                        DeviceId = "default",
                        StreamType = "cam",
                    });

                var header = handshake.Length.ToByteArray();

                Array.Resize(ref header, 4 + handshake.Length);
                Array.Copy(handshake, 0, header, 4, handshake.Length);

                await socket.SendAsync(header, token);
            }

            private static async Task SendAsync(Socket socket, CancellationToken token)
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

            public void Dispose()
            {
                _socket.Disconnect(false);
                _socket.Dispose();
            }
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
