// See https://aka.ms/new-console-template for more information

using EmulatorRC.Client.Protos;
using Grpc.Core;
using Grpc.Net.Client;

var channel = GrpcChannel.ForAddress("http://localhost:5001");
var client = new Screener.ScreenerClient(channel);

//foreach (var i in Enumerable.Range(0, 2))
//{
//    var response = await client.GetScreenAsync(new ScreenRequest() { Id = $"ID_{i}" });
//    Console.WriteLine("Screen: " + response.Image.ToStringUtf8());
//    await Task.Delay(1000);
//}

//using var screen = client.GetScreenStream(new ScreenRequest() { Id = "1" });
//await foreach (var response in screen.ResponseStream.ReadAllAsync())
//{
//    Console.WriteLine("ScreenStream: " + response.Image.ToStringUtf8());
//}

//***************************************************************************************
using var call = client.GetScreen2Stream();
Console.WriteLine("Starting background task to receive messages");
var readTask = Task.Run(async () =>
{
    await foreach (var response in call.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine("Screen2Stream: " + response.Image.ToStringUtf8());
    }
});

Console.WriteLine("Starting to send messages");
Console.WriteLine("Type a message to echo then press enter.");
while (true)
{
    var result = Console.ReadLine();
    if (string.IsNullOrEmpty(result))
    {
        break;
    }

    await call.RequestStream.WriteAsync(new ScreenRequest() { Id = "1" });
}

Console.WriteLine("Disconnecting");
await call.RequestStream.CompleteAsync();
await readTask;
