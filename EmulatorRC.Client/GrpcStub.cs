using EmulatorRC.Client.Protos;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Grpc.Net.Client;
using Vayosoft.gRPC.Reactive;

namespace EmulatorRC.Client;

public class GrpcStub : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly Screener.ScreenerClient _client;
    private readonly AsyncDuplexStreamingCall<ScreenRequest, ScreenReply> _stream;
    private readonly CancellationTokenSource _cts;
    private readonly Task _readTask;

    public static GrpcStub Create<T>() where T : ClientBase
    {
        return new GrpcStub();
    }

    public GrpcStub()
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

        _channel = GrpcChannel.ForAddress("http://localhost:5004", new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Insecure,
            ServiceConfig = new ServiceConfig
            {
                LoadBalancingConfigs = { new RoundRobinConfig() },
                MethodConfigs = { defaultMethodConfig }
            }
        });

        _client = new Screener.ScreenerClient(_channel);

        var headers = new Metadata
        {
            { "Authorization", $"Bearer {"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYW50b25AdmF5b3NvZnQuY29tIiwibmJmIjoxNjY3NTg2MTIxLCJleHAiOjE2Njc1ODk3MjEsImlzcyI6Imp3dC10ZXN0IiwiYXVkIjoiand0LXRlc3QifQ.00LJtfCA2PPSHKi55FZ7AWKzoDVZj8KGGlllMesdFGM"}" },
            { "X-DEVICE-ID", "TEST_DEV" }
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