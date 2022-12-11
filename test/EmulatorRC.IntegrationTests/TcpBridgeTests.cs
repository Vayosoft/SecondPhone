using System.Diagnostics;
using EmulatorRC.Entities;
using System.Net.Sockets;
using Commons.Core.Cryptography;
using Commons.Core.Extensions;
using Commons.Core.Helpers;
using Xunit.Abstractions;
using Commons.Core.Utilities;
using System.Text;

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
        var res = new DeviceSession
        {
            AccessToken = "123",
            DeviceId = "aasdadsd",
            StreamType = "cam"
        }.ToJSON();
        Helper.WriteLine(res);
        return;


        var sw = new Stopwatch();
        var tcs = new TaskCompletionSource();

        var emulatorClient = new TcpCoreClient("127.0.0.1", 5010, false, tcs);
        emulatorClient.OptionNoDelay = true;
        /*emulatorClient.OptionReceiveBufferSize = 8 * 1024 * 1024;
        emulatorClient.OptionSendBufferSize = 8 * 1024 * 1024;*/
        emulatorClient.ConnectAsync();
        while (!emulatorClient.IsConnected)
            Thread.Yield();

        var producerTcpClient = new TcpCoreClient("127.0.0.1", 5009, true, tcs);
        producerTcpClient.OptionNoDelay = true;
        /*producerTcpClient.OptionReceiveBufferSize = 8 * 1024 * 1024;
        producerTcpClient.OptionSendBufferSize = 8 * 1024 * 1024;*/
        producerTcpClient.ConnectAsync();
        while (!producerTcpClient.IsConnected)
            Thread.Yield();
        
        sw.Start();


        await tcs.Task;

        while (emulatorClient.IsConnected)
            Thread.Yield();
        sw.Stop();

        producerTcpClient.DisconnectAsync();

        var mb = Math.Round(new FileInfo(SourceFileName).Length / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds, 2);
        
        Helper.WriteLine($"time={sw.ElapsedMilliseconds}(ms), ~{mb} (MB/sec)");

        var shash = new FileInfo(SourceFileName).MD5();
        var thash = new FileInfo(SourceFileName).MD5();

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
            var ds = new DeviceSession
            {
                DeviceId = "12345",
                StreamType = "file",
                AccessToken = "asasas1212"
            };

            SendAsync(ds.ToBytes());

            if (_isSourceClient)
            {
                _sourceFileStream = new FileStream(SourceFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1024, true);
                ThreadPool.QueueUserWorkItem(_ => StartSourceWrite());
            }
            else
            {
                _targetFileStream = new FileStream(TargetFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize: 1024, true);
                _writtenBytes = 0;
            }
        }


        private async void StartSourceWrite()
        {
            var userBuffer = new byte[1024];
            int readBytes;
            while ((readBytes = await _sourceFileStream!.ReadAsync(userBuffer)) != 0)
            {
                Send(userBuffer, 0, readBytes);
            }

            _taskCompletion.TrySetResult();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if(size == 0)
                return;

            var b = new byte[size];
            Buffer.BlockCopy(buffer, (int)offset, b, 0, (int)size);

            _writtenBytes += size;
            // _helper.WriteLine($"received={size}, {_writtenBytes}/{_sourceSize}");

            _targetFileStream?.Write(buffer, (int)offset, (int)size);

            if (_writtenBytes >= _sourceSize)
                Disconnect();
            // _targetFileStream?.Flush();
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