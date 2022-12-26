using System.Diagnostics;
using EmulatorRC.Entities;
using System.Net.Sockets;
using System.Text;
using Commons.Core.Cryptography;
using Commons.Core.Extensions;
using Commons.Core.Helpers;
using Commons.Core.Utilities;
using Xunit.Abstractions;

namespace EmulatorRC.IntegrationTests;


public class TcpBridgeTests
{
    // private const string SourceFileName = @"D:\Distr\MySoft\a.txt";
    private const string SOURCE_FILE_NAME = @"D:\Distr\MySoft\VirtualBox-6.1.2-135663-Win.exe";
    private const string TARGET_FILE_NAME = @"D:\temp\tcp_test\VirtualBox-6.1.2-135663-Win.exe";
    public ITestOutputHelper Helper;

    public TcpBridgeTests(ITestOutputHelper helper)
    {
        Helper = Guard.NotNull(helper, nameof(helper));
    }


    [Fact]
    public void TcpImageClientTest()
    {
        var images = new List<byte[]>();
        var headers = new List<byte[]>();

        var files = Directory.GetFiles("D:\\temp\\images").ToList().OrderBy(f => f).ToList();
        foreach (var f in files)
        {
            var image = File.ReadAllBytes(f);
            images.Add(image);
            headers.Add(BitConverter.GetBytes(image.Length));
        }

        // var image = File.ReadAllBytes(@"D:\Sources\SecondPhone\resources\images\test\test-img-1.jpg");
        // var image2 = File.ReadAllBytes(@"D:\Sources\SecondPhone\resources\images\droidcam\640x480.jpg");

        // Helper.WriteLine($"image md5: {image.MD5()}");
        // Helper.WriteLine($"image2 md5: {image2.MD5()}");

        var tcsProducer = new TaskCompletionSource();
        var outer = new TcpImageClient("127.0.0.1", 5009, tcsProducer);
        // var outer = new TcpImageClient("192.168.10.6", 5009, tcsProducer);
        outer.OptionNoDelay = true;
        outer.ConnectAsync();
        while (!outer.IsConnected)
            Thread.Yield();

        var deviceSession = new DeviceSession
        {
            AccessToken = "11",
            DeviceId = "default",
            StreamType = "cam"
        }.ToJSON();
        
        var jsonHeader = Encoding.UTF8.GetBytes(deviceSession);
        var jsonHeaderLength = BitConverter.GetBytes(jsonHeader.Length);
        outer.Send(jsonHeaderLength);
        outer.Send(jsonHeader);

        foreach (var j in Enumerable.Range(0, 10000))
        {
            for (var i = 0; i < images.Count; i++)
            {
                outer.Send(headers[i]);
                outer.Send(images[i]);
                Thread.Sleep(40);
            }
        }

        /*// incomplete image buffer
        outer.Send(BitConverter.GetBytes(image.Length + 1));
        outer.Send(image.SubArray(0, 20));*/

        outer.DisconnectAsync();
        while (outer.IsConnected)
            Thread.Yield();
    }

    [Fact]
    public void TcpClientTest()
    {
        File.Delete(TARGET_FILE_NAME);

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
        
        var mb = Math.Round(new FileInfo(SOURCE_FILE_NAME).Length / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds, 2);
        
        Helper.WriteLine($"time={sw.ElapsedMilliseconds}(ms), ~{mb} (MB/sec)");

        var shash = new FileInfo(SOURCE_FILE_NAME).MD5();
        var thash = new FileInfo(TARGET_FILE_NAME).MD5();

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
            _sourceSize = new FileInfo(SOURCE_FILE_NAME).Length;
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

                _sourceFileStream = new FileStream(SOURCE_FILE_NAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1024, true);
                ThreadPool.QueueUserWorkItem(_ => StartSourceWrite());
            }
            else
            {
                Send("CMD /v2/video.4?640x480&id=default1");

                _targetFileStream = new FileStream(TARGET_FILE_NAME, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize: 1024, true);
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

    class TcpImageClient : NetCoreServer.TcpClient
    {
        private readonly TaskCompletionSource _taskCompletion;
        
        public TcpImageClient(string address, int port, TaskCompletionSource taskCompletion) : base(address, port)
        {
            _taskCompletion = taskCompletion;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Client caught an error with code {error}");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Client disconnected");
        }
        
    }
}