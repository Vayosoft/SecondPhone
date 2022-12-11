﻿using System.Text;
using System.Threading.Channels;
using Commons.Core.Application;
using Commons.Core.Exceptions;
using Commons.Core.Helpers;
using Commons.Core.Utilities;
using EmulatorRC.API.Channels;
using EmulatorRC.API.Model.Bridge.TCP.Servers;
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
        private int _handshakeBufferLength;

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
                    // var x = buffer.SubArrayFast((int)offset, (int)size);

                    var tcpData = new TcpData(new byte[size], offset, size);
                    System.Buffer.BlockCopy(buffer, (int)offset, tcpData.Buffer, 0, (int)size);

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

                    if(_handshakeStatus == HandshakeStatus.Successful)
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


        
        private HandshakeStatus Handshake(ref TcpData tcpData)
        {
            try
            {
                var data = tcpData.Buffer.SubArrayFast((int)tcpData.Offset, (int)tcpData.Length).ToList();

                if (_handshakeBuffer == null)
                {
                    _handshakeBuffer = new List<byte>();
                    _handshakeBufferLength = BitConverter.ToUInt16(data.Take(4).ToArray(), 0);
                    data = data.Skip(4).ToList();
                }

                var restBuffer = new List<byte>();
                if (_handshakeBuffer.Count < _handshakeBufferLength)
                {
                    var required = Math.Min(_handshakeBufferLength - _handshakeBuffer.Count, data.Count);
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
                    ThatStreamId = $"{ThatStreamId}.{_authData.DeviceId}.{_authData.StreamType}";

                    StreamChannel.RegisterChannel(ThatStreamId);
                    StreamChannel.RegisterChannel(ThisStreamId);

                    ThreadPool.QueueUserWorkItem(_ => { ReadThatStream(); });

                    tcpData = new TcpData(restBuffer.ToArray(), 0, restBuffer.Count);
                    return HandshakeStatus.Successful;
                }

                return HandshakeStatus.Pending;
            }
            catch (Exception ex)
            {
                _logger.LogError("Handshake.{thisSide} | {thisStream} | {type}: {error}, source: {source}, bufferLength: {bLength}", 
                    ThisSideName, ThisStreamId, ex.GetType(), ex, Encoding.UTF8.GetString(tcpData.Buffer), tcpData.Length);
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
                await foreach (var data in StreamChannel.ReadAllAsync(ThatStreamId, AppCancellationToken))
                {
                    var b = new byte[data.Length];
                    System.Buffer.BlockCopy(data.Buffer, 0, b, 0, (int)data.Length);

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
                Disconnect();
            }
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