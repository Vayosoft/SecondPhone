using EmulatorRC.Client.Protos;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Grpc.Net.Client;

namespace EmulatorRC.Client;

public class GrpcStub : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly Screener.ScreenerClient _client;
    private readonly AsyncDuplexStreamingCall<ScreenRequest, ScreenReply> _stream;
    private readonly CancellationTokenSource _cts;
    private readonly Task _readTask;

    public static GrpcStub Create<T>(string authToken) where T : ClientBase
    {
        return new GrpcStub(authToken);
    }

    public GrpcStub(string authToken)
    {
        var defaultMethodConfig = new MethodConfig
        {
            Names = { MethodName.Default },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 5,
                InitialBackoff = TimeSpan.FromSeconds(1),
                MaxBackoff = TimeSpan.FromSeconds(5),
                BackoffMultiplier = 1.5,
                RetryableStatusCodes = { StatusCode.Unavailable }
            }
        };

        _channel = GrpcChannel.ForAddress("https://localhost:5004", new GrpcChannelOptions
        {
            //Credentials = ChannelCredentials.Insecure,
            Credentials = ChannelCredentials.SecureSsl,
            ServiceConfig = new ServiceConfig
            {
                LoadBalancingConfigs = { new RoundRobinConfig() },
                MethodConfigs = { defaultMethodConfig }
            }
        });

        _client = new Screener.ScreenerClient(_channel);

        var headers = new Metadata
        {
            { "Authorization", $"Bearer {authToken}" },
            { "X-DEVICE-ID", Guid.NewGuid().ToString() }
        };

        _stream = _client.Connect(headers);
        _cts = new CancellationTokenSource();
        _readTask = ReadAsync(_cts.Token);
    }

    public async Task SendAsync(string id)
    {
        await _stream.RequestStream.WriteAsync(new ScreenRequest { Id = id });
    }

    private async Task ReadAsync(CancellationToken token)
    {
        try
        {
            //while (await _stream.ResponseStream.MoveNext(token))
            //{
            //    Console.WriteLine("Screen {0} bites", _stream.ResponseStream.Current.Image.Length);
            //}

            await foreach (var response in _stream.ResponseStream.ReadAllAsync(cancellationToken: token))
            {
                Console.WriteLine("Screen {0} bites", response.Image.Length);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            var userEntry = ex.Trailers.FirstOrDefault(e => e.Key.Equals("user", StringComparison.InvariantCultureIgnoreCase));
            Console.WriteLine($"User '{userEntry?.Value}' does not have permission to read from stream.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.RequestStream.CompleteAsync();
        await _readTask;
        _stream.Dispose();
        _cts.Dispose();
        _channel.Dispose();
    }
}