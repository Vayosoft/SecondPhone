using System.Net;
using System.Net.Sockets;
using Commons.Core.Application;
using Commons.Core.Helpers;
using EmulatorRC.API.Channels;
using EmulatorRC.API.Model.Bridge.TCP.Interfaces;
using EmulatorRC.API.Model.Bridge.TCP.Sessions;
using NetCoreServer;

namespace EmulatorRC.API.Model.Bridge.TCP.Servers
{
    public class TcpBridgeServer : TcpServer, IBridgeServer
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHostApplicationLifetime _lifeTime;
        private readonly TcpStreamChannel _streamChannel;
        private readonly ApplicationCache _cache;
        private readonly BridgeRole _bridgeRole;
        private readonly string _fakeImagePath;

        public string ThisSideName { get; }
        public string ThatSideName { get; }

        public TcpBridgeServer(
            BridgeRole role,
            string thisSideName,
            string thatSideName,
            IPAddress address, 
            int port,
            int bufferSize,
            ILoggerFactory loggerFactory,
            TcpStreamChannel streamChannel,
            IHostApplicationLifetime lifeTime,
            ApplicationCache cache,
            string fakeImagePath
            ) : base(address, port)
        {
            ThisSideName = Guard.NotEmpty(thisSideName, nameof(thisSideName));
            ThatSideName = Guard.NotEmpty(thatSideName, nameof(thatSideName));
            _logger = loggerFactory.CreateLogger(thisSideName);
            _loggerFactory = loggerFactory;
            _streamChannel = streamChannel;
            _lifeTime = lifeTime;
            _cache = cache;
            _bridgeRole = role;
            _fakeImagePath = fakeImagePath;

            if (bufferSize > 0)
            {
                OptionReceiveBufferSize = bufferSize;
                OptionSendBufferSize = bufferSize;
            }
        }

        protected override TcpSession CreateSession()
        {
            TcpSession s = _bridgeRole == BridgeRole.Outer
                ? new TcpOuterBridgeSession(this, _streamChannel, ThisSideName, ThatSideName, _loggerFactory, _lifeTime, _cache)
                : new TcpInnerBridgeSession(this, _streamChannel, ThisSideName, ThatSideName, _loggerFactory, _lifeTime, _cache, _fakeImagePath);

            _logger.LogInformation($"{ThisSideName} -> {ThatSideName} | new TCP connection");
            return s;
        }

        protected override void OnError(SocketError error)
        {
            _logger.LogError($"Server caught an error with code {error}");
        }

        protected override void OnDisconnected(TcpSession session)
        {
            TcpBridgeSession s = null;
            try
            {
                s = (TcpBridgeSession)session;
                _logger.LogDebug($"TcpBridgeServer | {s.ThisSideName} | {s.ThisStreamId} <-> {s.ThatStreamId} TCP session closed");
            }
            catch (Exception e)
            {
                _logger.LogError($"[TcpBridgeServer.OnDisconnected.{s?.ThisSideName}] {e.Message}");
            }
        }

        protected override void OnStopped()
        {
        }

        protected override void OnStarted()
        {
            _logger.LogInformation($"TCP listener started on: {Endpoint}");
        }
        
    }
}