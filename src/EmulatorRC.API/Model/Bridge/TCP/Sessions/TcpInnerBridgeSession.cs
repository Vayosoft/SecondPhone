using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Channels;
using Commons.Core.Application;
using Commons.Core.Exceptions;
using Commons.Core.Helpers.DataSplitter;
using EmulatorRC.API.Channels;
using EmulatorRC.API.Model.Bridge.TCP.Servers;
using EmulatorRC.Entities;
using NetCoreServer;

namespace EmulatorRC.API.Model.Bridge.TCP.Sessions
{
    public class TcpInnerBridgeSession : TcpBridgeSession
    {
        private CancellationTokenSource _ctsFakeFireTokenSource;
        private readonly ILogger _logger;
        
        private DeviceSession _authData;
        private bool _isFirstPacket = true;

        private readonly object _streamLock = new();
        private bool _isFakeStreamRunning;
        private bool _isQueueStreamRunning;

        private readonly byte[] _mockImageHeader;
        private byte[] _fakeImg;
        private byte[] _fakeImgLength;

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
            _mockImageHeader = CreateMockHeader(640, 480);
            _fakeImg = File.ReadAllBytes(@"D:\Sources\SecondPhone\src\EmulatorRC.API\file-119.jpg");
            _fakeImgLength = CreateLengthPrefix(_fakeImg);

        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (buffer != null && size > 0)
            {
                try
                {
                    var tcpData = new TcpData(new byte[size], offset, size);
                    System.Buffer.BlockCopy(buffer, (int)offset, tcpData.Buffer, 0, (int)size);

                    var payload = Encoding.UTF8.GetString(tcpData.Buffer);
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
                            // stop fake fire
                            if (_isFakeStreamRunning)
                                SwitchFakeFire(false);

                            // start reading from outer  (thatStream)
                            if(!_isQueueStreamRunning)
                                StartThatStreamReadingTask();
                            
                            // forward received to outer (thisStream)
                            channel.Writer.TryWrite(tcpData);
                        }
                        else if (!_isFakeStreamRunning) // no Outer channels
                        {
                            // start fake fire
                            SwitchFakeFire(true);
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
            if (payload.StartsWith("CMD /v2/video.4?"))
            {
                authData = new DeviceSession
                {
                    DeviceId = "default",
                    StreamType = "cam"
                };
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
                    var b = new byte[data.Length];
                    System.Buffer.BlockCopy(data.Buffer, 0, b, 0, (int)data.Length);

                    // await File.WriteAllBytesAsync(@$"D:\temp\tcp_test\data\{Guid.NewGuid()}.jpg", b);
                    var res = SendAsync(b, 0, b.Length);
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

                if (IsConnected)
                    SwitchFakeFire(true);
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

            SwitchFakeFire(false);

            _logger.LogInformation("Side: {side} | {clientId} Pipe closed.", ThisStreamId, ClientId);
        }


        private void SwitchFakeFire(bool enable)
        {
            if (_isFakeStreamRunning == enable)
                return;

            lock (_streamLock)
            {
                if (_isFakeStreamRunning == enable)
                    return;

                if (_ctsFakeFireTokenSource != null)
                {
                    _ctsFakeFireTokenSource.Cancel();
                    _ctsFakeFireTokenSource.Dispose();
                    _ctsFakeFireTokenSource = null;
                    _isFakeStreamRunning = false;
                }
                
                if (enable)
                {
                    _ctsFakeFireTokenSource = CancellationTokenSource.CreateLinkedTokenSource(AppCancellationToken);
                    ThreadPool.QueueUserWorkItem(_ => StartFakeFireTask(_ctsFakeFireTokenSource.Token));
                    
                    _isFakeStreamRunning = true;
                }
            }
        }
        
        private async void StartFakeFireTask(CancellationToken cancellationToken)
        {
            _isFakeStreamRunning = true;
            try
            {
                SendAsync(_mockImageHeader);
                while (IsConnected && !StreamChannel.IsChannelExists(ThatStreamId) && !cancellationToken.IsCancellationRequested)
                {
                    SendAsync(_fakeImgLength);
                    SendAsync(_fakeImg);
                    await Task.Delay(200, cancellationToken);
                }
            }
            catch (OperationCanceledException) {}
            catch (Exception ex)
            {
                _logger.LogError("StartFakeFireTask: {side} | {type} | {message}", ThisSideName, ex.GetType(), ex.Message);
            }
            finally
            {
                _isFakeStreamRunning = false;
                if (StreamChannel.IsChannelExists(ThatStreamId)) // outer connected
                {
                    StartThatStreamReadingTask();
                }
            }
        }

        private static byte[] CreateLengthPrefix(byte[] image)
        {
            var length = image.Length;
            
            var byteBuffer = new List<byte>();
            byteBuffer.Add((byte)(length & 255));
            byteBuffer.Add((byte)(length >> 8 & 255));
            byteBuffer.Add((byte)(length >> 16 & 255));
            byteBuffer.Add((byte)(length >> 24 & 255));
            
            return byteBuffer.ToArray();
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