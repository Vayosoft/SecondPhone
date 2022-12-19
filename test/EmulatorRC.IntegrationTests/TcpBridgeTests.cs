using System.Diagnostics;
using EmulatorRC.Entities;
using System.Net.Sockets;
using Commons.Core.Cryptography;
using Commons.Core.Extensions;
using Commons.Core.Helpers;
using Xunit.Abstractions;

namespace EmulatorRC.IntegrationTests;


public class TcpBridgeTests
{
    // private const string SourceFileName = @"D:\Distr\MySoft\a.txt";
    private const string SourceFileName = @"D:\Distr\MySoft\VirtualBox-6.1.2-135663-Win.exe";
    private const string TargetFileName = @"D:\temp\tcp_test\VirtualBox-6.1.2-135663-Win.exe";
    public ITestOutputHelper Helper;

    public TcpBridgeTests(ITestOutputHelper helper)
    {
        Helper = Guard.NotNull(helper, nameof(helper));
    }


    [Fact]
    public async Task TcpClientTest()
    {
        File.Delete(TargetFileName);

        var sw = new Stopwatch();
        var tcsProducer = new TaskCompletionSource();
        var tcsReceiver = new TaskCompletionSource();


        var emulatorClient = new TcpCoreClient("127.0.0.1", 5010, false, tcsReceiver);
        emulatorClient.OptionNoDelay = true;
        /*emulatorClient.OptionReceiveBufferSize = 8 * 1024 * 1024;
        emulatorClient.OptionSendBufferSize = 8 * 1024 * 1024;*/
        emulatorClient.ConnectAsync();
        while (!emulatorClient.IsConnected)
            Thread.Yield();


        var producerTcpClient = new TcpCoreClient("127.0.0.1", 5009, true, tcsProducer);
        producerTcpClient.OptionNoDelay = true;
        /*producerTcpClient.OptionReceiveBufferSize = 8 * 1024 * 1024;
        producerTcpClient.OptionSendBufferSize = 8 * 1024 * 1024;*/
        producerTcpClient.ConnectAsync();
        while (!producerTcpClient.IsConnected)
            Thread.Yield();
        
        sw.Start();
        Task.WaitAll(tcsProducer.Task, tcsReceiver.Task);
        sw.Stop();

        producerTcpClient.DisconnectAsync();
        while (emulatorClient.IsConnected || producerTcpClient.IsConnected)
            Thread.Yield();
        
        var mb = Math.Round(new FileInfo(SourceFileName).Length / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds, 2);
        
        Helper.WriteLine($"time={sw.ElapsedMilliseconds}(ms), ~{mb} (MB/sec)");

        var shash = new FileInfo(SourceFileName).MD5();
        var thash = new FileInfo(TargetFileName).MD5();

        Helper.WriteLine($"sourceHash={shash}");
        Helper.WriteLine($"targetHash={thash}, equal: {shash == thash}");
    }

    class TcpCoreClient : NetCoreServer.TcpClient
    {
        private readonly bool _isSourceClient;
        private FileStream? _sourceFileStream;
        private FileStream? _targetFileStream;
        private readonly TaskCompletionSource _taskCompletion;
        private readonly long _sourceSize;
        private long _writtenBytes;
        
        public TcpCoreClient(string address, int port, bool isSourceClient, TaskCompletionSource taskCompletion) : base(address, port)
        {
            _isSourceClient = isSourceClient;
            _taskCompletion = taskCompletion;
            _sourceSize = new FileInfo(SourceFileName).Length;
        }

        protected override void OnConnected()
        {
            
            if (_isSourceClient)
            {
                var ds = new DeviceSession
                {
                    DeviceId = "default1",
                    StreamType = "cam",
                    AccessToken = "asasas1212"
                }.ToBytes();
                var length = BitConverter.GetBytes(ds.Length);
                var handshake = new byte[4 + ds.Length];

                Buffer.BlockCopy(length, 0, handshake, 0, 4);
                Buffer.BlockCopy(ds.ToArray(), 0, handshake, 4, ds.Length);

                Send(handshake);

                _sourceFileStream = new FileStream(SourceFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1024, true);
                ThreadPool.QueueUserWorkItem(_ => StartSourceWrite());
            }
            else
            {
                Send("CMD /v2/video.4?640x480&id=default1");

                _targetFileStream = new FileStream(TargetFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize: 1024, true);
                _writtenBytes = 0;
            }
        }


        // producer
        private async void StartSourceWrite()
        {
            //Send(CreateMockHeader(640, 480));
            var userBuffer = new byte[2048];
            int readBytes;
            while ((readBytes = await _sourceFileStream!.ReadAsync(userBuffer)) != 0)
            {
                Send(userBuffer, 0, readBytes);
            }

            _taskCompletion.TrySetResult();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if(size is 0 or 9)
                return;

            var b = new byte[size];
            Buffer.BlockCopy(buffer, (int)offset, b, 0, (int)size);

            _writtenBytes += size;
            // _helper.WriteLine($"received={size}, {_writtenBytes}/{_sourceSize}");
            _targetFileStream?.Write(buffer, (int)offset, (int)size);
            _targetFileStream?.Flush();

            if (_writtenBytes >= _sourceSize)
            {
                Disconnect();
                _taskCompletion.TrySetResult();
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Client caught an error with code {error}");
        }

        protected override async void OnDisconnected()
        {
            if (_sourceFileStream != null)
            {
                _sourceFileStream.Flush();
                await _sourceFileStream.DisposeAsync();
            }

            if (_targetFileStream != null)
            {
                _targetFileStream.Flush();
                await _targetFileStream.DisposeAsync();
            }
            
        }
    }
}