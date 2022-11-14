using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public class ScreenService : Screener.ScreenerBase
    {
        private readonly ILogger<ScreenService> _logger;
        private readonly ScreenChannel _channel;
        private readonly IHostApplicationLifetime _lifeTime;

        public ScreenService(
            ILogger<ScreenService> logger,
            ScreenChannel channel, 
            IHostApplicationLifetime lifeTime)
        {
            _logger = logger;
            _channel = channel;
            _lifeTime = lifeTime;
        }

        //[Authorize]
        public override async Task Connect(
            IAsyncStreamReader<ScreenRequest> requestStream,
            IServerStreamWriter<ScreenReply> responseStream,
            ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            //var user = httpContext.User;
            //if (!TryValidateUser(user))
            //{
            //    var headers = new Metadata
            //        {
            //            { "user", user.Identity?.Name ?? string.Empty }
            //        };
            //    throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied"), headers);
            //}

            var deviceId = httpContext.Request.GetDeviceIdOrDefault("default")!;
            //var requesterHeader = context.RequestHeaders.FirstOrDefault(e => e.Key.Equals("x-device-id", StringComparison.InvariantCultureIgnoreCase));

            _logger.LogInformation("CLIENT:[default] Connected for device: {deviceId}.", deviceId);

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);

            try
            {
                await foreach (var request in requestStream.ReadAllAsync(cancellationSource.Token))
                {
                    var response = await _channel.ReadAsync(deviceId, request.Id, cancellationSource.Token);
                    await responseStream.WriteAsync(response, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError("{type}| {message}",ex.GetType(), ex.Message);
            }

            _logger.LogInformation("CLIENT:[default] Stream closed.");
        }

        //private static Task AwaitCancellation(CancellationToken token)
        //{
        //    var completion = new TaskCompletionSource<object>();
        //    token.Register(() => completion.SetResult(null!));
        //    return completion.Task;
        //}

        //private static bool TryValidateUser(ClaimsPrincipal principal)
        //{
        //    return true;
        //}
    }
}
