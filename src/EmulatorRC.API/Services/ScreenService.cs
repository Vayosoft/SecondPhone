using System.Security.Claims;
using System.Threading.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using EmulatorRC.Services;
using Google.Protobuf;
using Grpc.Core;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public class ScreenService : Screener.ScreenerBase
    {
        private readonly ILogger<ScreenService> _logger;
        private readonly IEmulatorDataRepository _emulatorDataRepository;
        private readonly ScreenChannel _screenChannel;

        public ScreenService(ILogger<ScreenService> logger, IEmulatorDataRepository emulatorDataRepository, ScreenChannel screenChannel)
        {
            _logger = logger;
            _emulatorDataRepository = emulatorDataRepository;
            _screenChannel = screenChannel;
        }

        public override async Task Connect(IAsyncStreamReader<ScreenRequest> requestStream, IServerStreamWriter<ScreenReply> responseStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var deviceId = httpContext.Request.GetDeviceIdOrDefault("default")!;
            //var requesterHeader = context.RequestHeaders.FirstOrDefault(e => e.Key.Equals("x-device-id", StringComparison.InvariantCultureIgnoreCase));

            var user = httpContext.User;
            if (!TryValidateUser(user))
            {
                var headers = new Metadata
                {
                    { "user", user.Identity?.Name ?? string.Empty }
                };
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied"), headers);
            }

            _logger.LogInformation("Connected for {deviceId}", deviceId);
            var reader = _screenChannel.Subscribe();

            try
            {
                while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
                {
                    var id = requestStream.Current.Id;

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Stream request => handledId: {handledId}", id);
                    }

                    var screen = await reader.ReadAsync(context.CancellationToken);
                    var response = new ScreenReply
                    {
                        Id = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                        Image = UnsafeByteOperations.UnsafeWrap(screen)
                    };

                    await responseStream.WriteAsync(response);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { /*ignored*/ }
        }

        //[Authorize]
        //public override async Task Connect(IAsyncStreamReader<ScreenRequest> requestStream, IServerStreamWriter<ScreenReply> responseStream, ServerCallContext context)
        //{
        //    var httpContext = context.GetHttpContext();
        //    var deviceId = httpContext.Request.GetDeviceIdOrDefault("default")!;
        //    //var requesterHeader = context.RequestHeaders.FirstOrDefault(e => e.Key.Equals("x-device-id", StringComparison.InvariantCultureIgnoreCase));

        //    var user = httpContext.User;
        //    if (!TryValidateUser(user))
        //    {
        //        var headers = new Metadata
        //        {
        //            { "user", user.Identity?.Name ?? string.Empty }
        //        };
        //        throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied"), headers);
        //    }

        //    _logger.LogInformation("Connected for {deviceId}", deviceId);

        //    try
        //    {
        //        while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
        //        {
        //            var id = requestStream.Current.Id;

        //            if (_logger.IsEnabled(LogLevel.Debug))
        //            {
        //                _logger.LogDebug("Stream request => handledId: {handledId}", id);
        //            }

        //            var screen = _emulatorDataRepository.GetLastScreen(deviceId);
        //            var response = screen is null || screen.Id.Equals(id, StringComparison.Ordinal)
        //                ? new ScreenReply()
        //                : new ScreenReply
        //                {
        //                    Id = screen.Id,
        //                    Image = UnsafeByteOperations.UnsafeWrap(screen.Image)
        //                };

        //            await responseStream.WriteAsync(response);
        //        }
        //    }
        //    catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { /*ignored*/ }
        //}

        //private static Task AwaitCancellation(CancellationToken token)
        //{
        //    var completion = new TaskCompletionSource<object>();
        //    token.Register(() => completion.SetResult(null!));
        //    return completion.Task;
        //}

        private static bool TryValidateUser(ClaimsPrincipal principal)
        {
            return true;
        }
    }

    public class ScreenChannel
    {
        private const int MAX_QUEUE = 1;

        private readonly Channel<byte[]> _channel;

        public ScreenChannel()
        {
            var options = new BoundedChannelOptions(1)
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropOldest
            };
            _channel = Channel.CreateBounded<byte[]>(options);
        }

        public async ValueTask Enqueue(byte[] image, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(image, cancellationToken);
        }

        public void Complete()
        {
            _channel.Writer.Complete();
        }

        public ChannelReader<byte[]> Subscribe()
        {
            return _channel.Reader;
        }
    }
}
