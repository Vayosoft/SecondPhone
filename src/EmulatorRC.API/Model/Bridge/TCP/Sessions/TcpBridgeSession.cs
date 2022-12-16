using System.Net;
using System.Net.Sockets;
using Commons.Core.Application;
using Commons.Core.Exceptions;
using EmulatorRC.API.Channels;
using EmulatorRC.API.Model.Bridge.TCP.Interfaces;
using NetCoreServer;

namespace EmulatorRC.API.Model.Bridge.TCP.Sessions
{
    public abstract class TcpBridgeSession : TcpSession, IBridgeSession
    {
        protected readonly TcpStreamChannel StreamChannel;
        protected readonly CancellationToken AppCancellationToken;
        protected readonly ApplicationCache Cache;

        public string ThisStreamId { get; protected set; }
        public string ThatStreamId { get; protected set; }

        public string ThisSideName { get; }
        public string ThatSideName { get; }

        public string ClientIp { get; }
        public int ClientPort { get; }
        public string ClientId { get; } = string.Empty;

        protected abstract ILogger Logger();

        
        protected TcpBridgeSession(TcpServer server,
            TcpStreamChannel streamChannel,
            string thisBridgePrefix,
            string secondBridgePrefix,
            IHostApplicationLifetime lifeTime,
            ApplicationCache cache
            ) : base(server)
        {
            if (server == null)
                throw new CommonsException<ExceptionCode.Operation>(ExceptionCode.Operation.InvalidArgument, $"TcpBridgeSession | {thisBridgePrefix} | TcpServer required");
            
            /*if (Socket.RemoteEndPoint is not IPEndPoint ep)
                throw new CommonsException<ExceptionCode.Operation>(ExceptionCode.Operation.InvalidArgument, "TcpBridgeSession| Unknown endpoint");*/
            
            if (Socket is { RemoteEndPoint: IPEndPoint ep })
            {
                ClientIp = ep.Address.ToString();
                ClientPort = ep.Port;
                ClientId = $"{ClientIp}:{ClientPort}";
            }
            
            StreamChannel = streamChannel;

            ThisSideName = thisBridgePrefix;
            ThatSideName = secondBridgePrefix;
            AppCancellationToken = lifeTime.ApplicationStopping;
            
            Cache = cache;
        }

        protected override void OnError(SocketError error)
        {
            Logger().LogError("OnError.{thisSide} | {thisStream} | {type}: Socket error: {error}", ThisSideName, ThisStreamId, error.GetType(), error);
        }

    }
}