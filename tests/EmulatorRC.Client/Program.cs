﻿// See https://aka.ms/new-console-template for more information

using EmulatorRC.Client;
using EmulatorRC.Client.Protos;
using EmulatorRC.Client.Tokens;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using System.Threading.Channels;
using EmulatorRC.API.Protos;
using Google.Protobuf;

var tokenResult = JwtUtils.GenerateToken("qwertyuiopasdfghjklzxcvbnm123456");

using var cts = new CancellationTokenSource();
var uploadTask = Task.Run(async () =>
{
    var channel = GrpcChannel.ForAddress("http://localhost:5004", new GrpcChannelOptions
    {
        Credentials = ChannelCredentials.Insecure,
        //Credentials = ChannelCredentials.SecureSsl
    });

    var client = new Uploader.UploaderClient(channel);
    var headers = new Metadata
    {
        { "X-DEVICE-ID", "default" }
    };
    using var call = client.UploadMessage(headers);
    foreach (var enumerateFile in Directory.EnumerateFiles("C:\\Users\\anton\\OneDrive\\Pictures"))
    {
        if (cts.IsCancellationRequested) break;
        var image = await File.ReadAllBytesAsync(enumerateFile);
        await 1000;
        await call.RequestStream.WriteAsync(new UploadMessageRequest
        {
            Image = ByteString.CopyFrom(image)
        });
    }
});

await using var screenClient = new GrpcStub(tokenResult.Token);
Console.WriteLine("Starting to send messages");
Console.WriteLine("Type a message to echo then press enter.");
while (true)
{
    var result = Console.ReadLine();
    if (string.IsNullOrEmpty(result)) break;

    await screenClient.SendAsync(result);
}

cts.Cancel();
await uploadTask;
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

class TestObservable : IObserver<ScreenReply>
{
    public void OnCompleted()
    {
        Console.WriteLine("Completed!");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine("Error: " + error.Message);
    }

    public void OnNext(ScreenReply value)
    {
        Console.WriteLine("ScreenStream: " + value.Image.ToStringUtf8());
    }
}