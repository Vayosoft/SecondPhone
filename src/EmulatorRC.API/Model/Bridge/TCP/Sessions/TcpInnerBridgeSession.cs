using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Commons.Core.Application;
using Commons.Core.Exceptions;
using Commons.Core.Helpers;
using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using NetCoreServer;

namespace EmulatorRC.API.Model.Bridge.TCP.Sessions
{
    public class TcpInnerBridgeSession : TcpBridgeSession
    {
        private readonly ILogger _logger;
        
        private DeviceSession _authData;
        private bool _isFirstPacket = true;

        private readonly object _streamLock = new();
        private bool _isQueueStreamRunning;

        private const string PING_COMMAND = "CMD /v1/ping";

        private const RegexOptions REGEX_OPTIONS = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;

        protected override ILogger Logger()
        {
            return _logger;
        }


        public TcpInnerBridgeSession(TcpServer server,
            TcpStreamChannel streamChannel,
            string thisBridgePrefix,
            string secondBridgePrefix,
            ILoggerFactory logger, 
            IHostApplicationLifetime lifeTime,
            ApplicationCache cache
            ) : base(server, streamChannel, thisBridgePrefix, secondBridgePrefix, lifeTime, cache)
        {
            if (server == null)
                throw new CommonsException<ExceptionCode.Operation>(ExceptionCode.Operation.InvalidArgument, "TcpInnerBridgeSession| TcpServer required");
            
            /*if (Socket.RemoteEndPoint is not IPEndPoint ep)
                throw new CommonsException<ExceptionCode.Operation>(ExceptionCode.Operation.InvalidArgument, "TcpBridgeSession| Unknown endpoint");*/
            
            ThisStreamId = ThatStreamId = string.Empty;
            
            _logger = logger.CreateLogger(ClientId);
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (buffer != null && size > 0)
            {
                try
                {
                    var tcpData = buffer.SubArrayFast((int)offset, (int)size);
                    System.Buffer.BlockCopy(buffer, (int)offset, tcpData, 0, (int)size);

                    var payload = Encoding.UTF8.GetString(tcpData);
                    if (payload == PING_COMMAND)
                        return;

                    _logger.LogInformation("OnReceived | {thisSideName}: {message}", ThisSideName, payload);
                    if (_isFirstPacket)
                    {
                        _isFirstPacket = false;
                        if (!Handshake(payload))
                        {
                            Disconnect();
                            return;
                        }
                    }
                    
                    if (payload.StartsWith("GET /battery"))
                    {
                        SendAsync("\r\n\r\n100");
                    }
                    else
                    {
                        if (StreamChannel.TryGetChannel(ThisStreamId, out var channel)) // Outer exists, so bridge required
                        {
                            // start reading from outer  (thatStream)
                            if(!_isQueueStreamRunning)
                                StartThatStreamReadingTask();
                            
                            // forward received to outer (thisStream)
                            channel.Writer.TryWrite(tcpData);
                        }
                    }
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

        private bool Handshake(string payload)
        {
            DeviceSession authData = null;
            if (payload.StartsWith("CMD /v2/video.4?")) // CMD /v2/video.4?640x480&id=default1
            {
                var s = payload.Split("?")[1];
                if (s.Length == 0)
                    return false;

                var m = Regex.Match(s, "(\\d+)x(\\d+)&id=(\\w+)", REGEX_OPTIONS);
                if (!m.Success || m.Groups.Count < 4)
                    return false;
                
                if (!int.TryParse(m.Groups[1].Value, out var w) || !int.TryParse(m.Groups[2].Value, out var h))
                    return false;

                var deviceId = m.Groups[3].Value;

                if (w == 0 || h == 0 || string.IsNullOrEmpty(deviceId))
                    return false;

                SendAsync(CreateMockHeader(w, h));

                authData = new DeviceSession { DeviceId = deviceId, StreamType = "cam" };
            }

            if (authData == null)
                return false;

            _authData = authData;

            ThisStreamId = $"{ThisSideName}.{authData.DeviceId}.{authData.StreamType}";
            ThatStreamId = $"{ThatSideName}.{_authData.DeviceId}.{_authData.StreamType}";
            

            return true;
        }
        
        private async void ReadThatStream()
        {
            try
            {
                _isQueueStreamRunning = true;

                await foreach (var data in StreamChannel.ReadAllAsync(ThatStreamId, AppCancellationToken))
                {
                    var res = SendAsync(data.SubArrayFast());
                    // _logger.LogInformation("SendToClientAsync: {side} | {ThatStreamId} -> {ThisStreamId}| {message}, {res}", ThisStreamId, ThatStreamId, ThisStreamId, Encoding.UTF8.GetString(b, 0, b.Length), res);
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

                _isQueueStreamRunning = false;

                Disconnect();
                // if (IsConnected)
                // SwitchFakeFire(true);
            }
        }

        private void StartThatStreamReadingTask()
        {
            if (_isQueueStreamRunning)
                return;

            lock (_streamLock)
            {
                if (_isQueueStreamRunning)
                    return;

                _isQueueStreamRunning = true;

                ThreadPool.QueueUserWorkItem(_ => ReadThatStream());
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
            
            _logger.LogInformation("Side: {side} | {clientId} Pipe closed.", ThisStreamId, ClientId);
        }

        private static byte[] CreateMockHeader(int width, int height)
        {
            int some1 = 0x21; //25,
            int some2 = 0x307fe8f5;

            var byteBuffer = new List<byte>();

            //0x02, 0x80 - 640
            byteBuffer.Add((byte)(width >> 8 & 255));
            byteBuffer.Add((byte)(width & 255));

            //0x01, 0xe0 - 480
            byteBuffer.Add((byte)(height >> 8 & 255));
            byteBuffer.Add((byte)(height & 255));
            
            //0x21 - 33
            byteBuffer.Add((byte)(some1 & 255));

            //0xf5, 0xe8, 0x7f, 0x30
            byteBuffer.Add((byte)(some2 & 255));
            byteBuffer.Add((byte)(some2 >> 8 & 255));
            byteBuffer.Add((byte)(some2 >> 16 & 255));
            byteBuffer.Add((byte)(some2 >> 24 & 255));

            // byteBuffer.Reverse();

            return byteBuffer.ToArray();
        }

    }
}