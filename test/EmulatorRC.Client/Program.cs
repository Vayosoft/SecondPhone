// See https://aka.ms/new-console-template for more information

using System.Buffers;
using EmulatorRC.Client;
using Grpc.Core;
using Grpc.Net.Client;
using EmulatorRC.API.Protos;
using Google.Protobuf;
using System;
using EmulatorHub.Application.Commons.Services.IdentityProvider;

var tokenResult = TokenUtils.GenerateToken("qwertyuiopasdfghjklzxcvbnm123456", TimeSpan.FromMinutes(5));
var url = "http://192.168.10.6:5006";
//var url = "http://localhost:5004";

var cts = new CancellationTokenSource();

var uploadTask = Task.Run(async () =>
{
    try{
        var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions
    
        {
            Credentials = ChannelCredentials.Insecure,
            //Credentials = ChannelCredentials.SecureSsl
        });

        var client = new DeviceService.DeviceServiceClient(channel);
        var headers = new Metadata
        {
            {"X-DEVICE-ID", "default"}
        };

        var pool = ArrayPool<byte>.Shared;
        using var call = client.UploadScreens(headers);
        while (!cts.IsCancellationRequested)
        {
            var buffer = pool.Rent(50 * 1024);
            try
            {
                await call.RequestStream.WriteAsync(new DeviceScreen
                {
                    Image = ByteString.CopyFrom(buffer)
                }, CancellationToken.None);
            }
            finally
            {
                pool.Return(buffer);
            }
           
        }

        await call.RequestStream.CompleteAsync();
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
});

var screenClient = new GrpcStub(url, tokenResult.Token);
//var screenClient2 = new GrpcStub(url, tokenResult.Token);
try
{
    Console.WriteLine("Starting to send messages");
    Console.WriteLine("Type a message to echo then press enter.");
    while (true)
    {
        Console.WriteLine(url);
        var result = Console.ReadLine();

        if (result is "1")
        {
            cts.Cancel();
            break;
        }

        await screenClient.SendAsync(result ?? string.Empty);
       // await screenClient2.SendAsync(result ?? string.Empty);
    }
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

await uploadTask;
await screenClient.DisposeAsync();
cts.Dispose();
Console.WriteLine("Done!");

//AsymmetricKey.Create();

//----------------------------------------------------------

//var defaultMethodConfig = new MethodConfig
//{
//    Names = { MethodName.Default },
//    RetryPolicy = new RetryPolicy
//    {
//        MaxAttempts = 5,
//        InitialBackoff = TimeSpan.FromSeconds(1),
//        MaxBackoff = TimeSpan.FromSeconds(5),
//        BackoffMultiplier = 1.5,
//        RetryableStatusCodes = { StatusCode.Unavailable }
//    }
//};

//var channel = GrpcChannel.ForAddress("http://localhost:5004", new GrpcChannelOptions
//{
//    Credentials = ChannelCredentials.Insecure,
//    ServiceConfig = new ServiceConfig
//    {
//        LoadBalancingConfigs = { new RoundRobinConfig() },
//        MethodConfigs = { defaultMethodConfig }
//    }
//});

//var client = new Screener.ScreenerClient(channel);

//foreach (var i in Enumerable.Range(0, 2))
//{
//    var response = await client.GetScreenAsync(new ScreenRequest { Id = $"ID_{i}" });
//    Console.WriteLine("Screen: " + response.Image.ToStringUtf8());
//    await Task.Delay(1000);
//}

//using var screen = client.GetScreenStream(new ScreenRequest() { Id = "1" });
//await foreach (var response in screen.ResponseStream.ReadAllAsync())
//{
//    Console.WriteLine("ScreenStream: " + response.Image.ToStringUtf8());
//}

//***************************************************************************************

//var metadata = new Metadata
//{
//    { "X-DEVICE-ID", "TEST_DEV" }
//};

//using var call = client.Connect(metadata);
//Console.WriteLine("Starting background task to receive messages");
//var readTask = Task.Run(async () =>
//{
//    await foreach (var response in call.ResponseStream.ReadAllAsync())
//    {
//        Console.WriteLine("Screen2Stream: " + response.Image.ToStringUtf8());
//    }
//});

//Console.WriteLine("Starting to send messages");
//Console.WriteLine("Type a message to echo then press enter.");
//while (true)
//{
//    var result = Console.ReadLine();
//    if (string.IsNullOrEmpty(result)) break;
//    await call.RequestStream.WriteAsync(new ScreenRequest() { Id = "1" });
//}

//Console.WriteLine("Disconnecting");
//await call.RequestStream.CompleteAsync();
//await readTask;

//***************************************************************************************

class TestObservable : IObserver<DeviceScreen>
{
    public void OnCompleted()
    {
        Console.WriteLine("Completed!");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine("Error: " + error.Message);
    }

    public void OnNext(DeviceScreen value)
    {
        Console.WriteLine("ScreenStream: " + value.Image.ToStringUtf8());
    }
}
