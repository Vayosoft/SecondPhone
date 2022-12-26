using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Commons.Core.Application;
using Commons.Core.Exceptions;
using Commons.Core.Helpers;
using Commons.Core.Utilities;
using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using NetCoreServer;

namespace EmulatorRC.API.Model.Bridge.TCP.Sessions
{
    public class TcpOuterBridgeSession : TcpBridgeSession
    {
        private readonly ILogger _logger;
        
        private DeviceSession _authData;
        
        private HandshakeStatus _handshakeStatus;
        private List<byte> _handshakeBuffer;
        private uint _handshakeBufferLength;
        

        protected override ILogger Logger()
        {
            return _logger;
        }
        
        public TcpOuterBridgeSession(TcpServer server,
            TcpStreamChannel streamChannel,
            string thisSideName,
            string thatSideName,
            ILoggerFactory logger, 
            IHostApplicationLifetime lifeTime,
            ApplicationCache cache
            ) : base(server, streamChannel, thisSideName, thatSideName, lifeTime, cache)
        {
            if (server == null)
                throw new CommonsException<ExceptionCode.Operation>(ExceptionCode.Operation.InvalidArgument, $"TcpOuterBridgeSession| {thisSideName} | TcpServer required");
            
            /*if (Socket.RemoteEndPoint is not IPEndPoint ep)
                throw new CommonsException<ExceptionCode.Operation>(ExceptionCode.Operation.InvalidArgument, "TcpBridgeSession| Unknown endpoint");*/

            ThisStreamId = ThatStreamId = string.Empty;
            
            _logger = logger.CreateLogger(ThisSideName);
            
            _handshakeBuffer = null;
            _handshakeBufferLength = 0;
            _handshakeStatus = HandshakeStatus.Pending;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (buffer != null && size > 0)
            {
                try
                {
                    if (size > 10485760) // 10MB
                    {
                        _logger.LogError("OnReceived.{thisSide} | {thisStream}: too big buffer size: {size}", ThisSideName, ThisStreamId, size);
                        Disconnect();
                        return;
                    }
                    
                    var tcpData = buffer.SubArray((int)offset, (int)size);
                    if (_handshakeStatus == HandshakeStatus.Successful)
                    {
                        StreamChannel.Write(ThisStreamId, tcpData);
                        return;
                    }
                        
                    if (_handshakeStatus == HandshakeStatus.Pending)
                    {
                        _handshakeStatus = Handshake(ref tcpData);
                        if (_handshakeStatus == HandshakeStatus.Failed)
                        {
                            Disconnect();
                            return;
                        }
                    }

                    if(_handshakeStatus == HandshakeStatus.Successful && tcpData.Length > 0)
                        StreamChannel.Write(ThisStreamId, tcpData);
                }
                catch (Exception ex)
                {
                    _logger.LogError("OnReceived.{thisSide} | {thisStream} | {type} | server caught an error with buffer: {error}", ThisSideName, ThisStreamId, ex.GetType(), ex);
                }
            }
            else
            {
                _logger.LogTrace("OnReceived.{thisSide} | {thisStream} | received zero-size buffer", ThisSideName, ThisStreamId);
            }
        }


        
        private HandshakeStatus Handshake(ref byte[] tcpData)
        {
            try
            {
                const int requiredHeaderLength = 4;

                var data = tcpData.ToList();
                if (_handshakeBuffer == null)
                {
                    if (tcpData.Length < requiredHeaderLength)
                        throw new ArgumentException("handshake header required 4 bytes");

                    _handshakeBuffer = new List<byte>();
                    _handshakeBufferLength = BitConverter.ToUInt32(data.Take(requiredHeaderLength).ToArray(), 0);
                    data = data.Skip(requiredHeaderLength).ToList();
                }

                var restBuffer = new List<byte>();
                if (_handshakeBuffer.Count < _handshakeBufferLength)
                {
                    var required = (int)Math.Min(_handshakeBufferLength - _handshakeBuffer.Count, data.Count);
                    _handshakeBuffer.AddRange(data.Take(required));
                    restBuffer.AddRange(data.Skip(required));
                }

                if (_handshakeBuffer.Count > 128 * 1024 * 1024)
                    throw new Exception("Handshake | Buffer has exceeded max size: 128(KB)");
                
                if (_handshakeBuffer.Count >= _handshakeBufferLength)
                {
                    var authData = JSONUtils.Deserialize<DeviceSession>(Encoding.UTF8.GetString(_handshakeBuffer.ToArray()));
                    if (authData == null)
                        return HandshakeStatus.Failed;

                    _authData = authData;

                    ThisStreamId = $"{ThisSideName}.{authData.DeviceId}.{authData.StreamType}";
                    ThatStreamId = $"{ThatSideName}.{authData.DeviceId}.{authData.StreamType}";

                    StreamChannel.RegisterChannel(ThatStreamId);
                    StreamChannel.RegisterChannel(ThisStreamId);

                    ThreadPool.QueueUserWorkItem(_ => { ReadThatStream(); });

                    tcpData = restBuffer.ToArray();
                    return HandshakeStatus.Successful;
                }

                return HandshakeStatus.Pending;
            }
            catch (Exception ex)
            {
                _logger.LogError("Handshake.{thisSide} | {thisStream} | {type}: {error}, source: {source}, bufferLength: {bLength}", 
                    ThisSideName, ThisStreamId, ex.GetType(), ex, Encoding.UTF8.GetString(tcpData), tcpData.Length);
                return HandshakeStatus.Failed;
            }
        }

        /*protected override void OnSent(long sent, long pending)
        {
            _logger.LogInformation("OnSent | {sent} | {pending}", sent, pending);
        }*/

        private async void ReadThatStream()
        {
            try
            {
                // _logger.LogInformation("OUTER.ReadThatStream: ThatStreamId={ThatStreamId}", ThatStreamId);
                await foreach (var data in StreamChannel.ReadAllAsync(ThatStreamId, AppCancellationToken))
                {
                    Send(data.SubArray());
                }
            }
            catch (OperationCanceledException) { }
            catch (ChannelClosedException) { }
            catch (Exception ex)
            {
                _logger.LogError("ReadThatStream.{thisSide} | {thisStream} | {type}: {error}", ThisSideName, ThisStreamId, ex.GetType(), ex);
            }
            finally
            {
                _logger.LogInformation("ReadThatStream.{thisSide} | {thisStream}: Stream from: {thatStream} closed", ThisSideName, ThisStreamId, ThatStreamId);
                Disconnect();
            }
        }

        protected override void OnError(SocketError error)
        {
            _logger.LogInformation("Side: {side} | {clientId} Socket error: {error}", ThatStreamId, ClientId, error);
        }

        protected override void OnDisconnected()
        {
            StreamChannel.DisposeChannel(ThisStreamId);
            StreamChannel.DisposeChannel(ThatStreamId);

            _logger.LogInformation("OnDisconnected.{thisSide} | {thisStream} | that side: {thatStream}", ThisSideName, ThisStreamId, ThatStreamId);
        }

        public enum HandshakeStatus
        {
            Pending,
            Successful,
            Failed
        }


    }
}