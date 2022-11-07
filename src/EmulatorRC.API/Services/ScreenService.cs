using System.Net;
using System.Security.Claims;
using System.Text;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using EmulatorRC.Services;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public class ScreenService : Screener.ScreenerBase
    {
        private readonly ILogger<ScreenService> _logger;
        private readonly IEmulatorDataRepository _emulatorDataRepository;

        public ScreenService(ILogger<ScreenService> logger, IEmulatorDataRepository emulatorDataRepository)
        {
            _logger = logger;
            _emulatorDataRepository = emulatorDataRepository;
        }

        //public override Task<ScreenReply> GetScreen(ScreenRequest request, ServerCallContext context)
        //{
        //    _logger.LogInformation("Saying hello to {Name}", request.Id);
        //    return Task.FromResult(new ScreenReply
        //    {
        //       Image = ByteString.CopyFrom(Encoding.UTF8.GetBytes("GetScreen"))
        //    });
        //}

        //public override async Task GetScreenStream(ScreenRequest request, IServerStreamWriter<ScreenReply> responseStream, ServerCallContext context)
        //{
        //    foreach (var x in Enumerable.Range(1, 2))
        //    {
        //        await responseStream.WriteAsync(new ScreenReply
        //        {
        //            Image = ByteString.CopyFrom(Encoding.UTF8.GetBytes("GetScreenStream"))
        //        });

        //        await Task.Delay(200);
        //    }
        //}

        [Authorize]
        public override async Task Connect(IAsyncStreamReader<ScreenRequest> requestStream, IServerStreamWriter<ScreenReply> responseStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var deviceId = httpContext.Request.GetDeviceIdOrDefault("default")!;
            //var requesterHeader = context.RequestHeaders.FirstOrDefault(e => e.Key.Equals("x-device-id", StringComparison.InvariantCultureIgnoreCase));
            //context.ResponseTrailers.Add("X-SERVER-NAME", "");

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

            try
            {
                while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
                {
                    var lastId = requestStream.Current.Id;

                    _logger.LogDebug("Stream request => lastId: {lastId}", lastId);

                    var screen = _emulatorDataRepository.GetLastScreen(deviceId);
                    if (screen is null || screen.Id == lastId) continue;
                    var response = new ScreenReply
                    {
                        Id = screen.Id,
                        //Image = ByteString.CopyFrom(bytes)
                        Image = UnsafeByteOperations.UnsafeWrap(screen.Image)
                    };

                    await responseStream.WriteAsync(response);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { /*ignored*/ }

            //await AwaitCancellation(context.CancellationToken);
        }

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
}
