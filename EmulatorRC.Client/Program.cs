// See https://aka.ms/new-console-template for more information

using EmulatorRC.Client.Protos;
using Grpc.Core;
using Grpc.Net.Client;

var channel = GrpcChannel.ForAddress("http://localhost:5001");
var client = new Screener.ScreenerClient(channel);

foreach (var i in Enumerable.Range(0, 10))
{
    var response = await client.GetScreenAsync(new ScreenRequest() { Id = $"ID_{i}" });
    Console.WriteLine(response.Image);
    await Task.Delay(1000);
}

var screen = client.GetScreenStream(new ScreenRequest() { Id = "1" });
await foreach (var response in screen.ResponseStream.ReadAllAsync())
{
    Console.WriteLine("Screen: " + response.Image);
}


//Console.WriteLine("Starting background task to receive messages");
//var readTask = Task.Run(async () =>
//{
//    await foreach (var response in call.ResponseStream.ReadAllAsync())
//    {
//        Console.WriteLine(response.Message);
//        // Echo messages sent to the service
//    }
//});

//Console.WriteLine("Starting to send messages");
//Console.WriteLine("Type a message to echo then press enter.");
//while (true)
//{
//    var result = Console.ReadLine();
//    if (string.IsNullOrEmpty(result))
//    {
//        break;
//    }

//    await call.RequestStream.WriteAsync(new HelloRequest { Name = result });
//}

//Console.WriteLine("Disconnecting");
//await call.RequestStream.CompleteAsync();
//await readTask;
