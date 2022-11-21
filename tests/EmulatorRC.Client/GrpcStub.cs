using EmulatorRC.Client.Protos;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Grpc.Net.Client;
using LanguageExt.Common;

namespace EmulatorRC.Client;

public class GrpcStub : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly Screener.ScreenerClient _client;
    private readonly AsyncDuplexStreamingCall<ScreenRequest, ScreenReply> _stream;
    private readonly CancellationTokenSource _cts;
    private readonly Task _readTask;
    private readonly string _stubId = Guid.NewGuid().ToString("N");

    public static GrpcStub Create<T>(string url, string authToken) where T : ClientBase
    {
        return new GrpcStub(url, authToken);
    }

    public GrpcStub(string url, string authToken)
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

        _channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Insecure,
            //Credentials = ChannelCredentials.SecureSsl,
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
            { "X-DEVICE-ID", "default" }
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
                Console.WriteLine("[{0}] Screen {1} {2} bites", _stubId, response.Id, response.Image.Length);
                await SendAsync(response.Id);
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