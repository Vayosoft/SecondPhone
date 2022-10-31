using Microsoft.AspNetCore.SignalR.Client;
using System.Collections;

 var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/zub")
.Build();


connection.On<byte[]>("OnGetLastScreen", data => Console.WriteLine("Got data '{0}'", BitConverter.ToString(data)));

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

