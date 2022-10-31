using Microsoft.AspNetCore.SignalR.Client;

 var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/zub")
    .Build();


connection.On<string>("OnGetLastScreen", data => Console.WriteLine("Got data {0}", data));

 var producer = Task.Run(async () =>
 {
     foreach (var n in Enumerable.Range(0, 10))
     {
         await Task.Delay(1000);
         await connection.InvokeAsync("GetLastScreen");
     }
 });
 
 await connection.StartAsync();
 await Task.Delay(1000);
 await producer;

