using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Channels;
using EmulatorHub.API.Protos;

namespace EmulatorRC.Client
{
    public class ChatStub
    {
        public async Task Run()
        {
            var url = "http://localhost:5006";
            var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions

            {
                Credentials = ChannelCredentials.Insecure,
                //Credentials = ChannelCredentials.SecureSsl
            });
            var client = new HubRoom.HubRoomClient(channel);

            using (var chat = client.join())
            {
                _ = Task.Run(async () =>
                {
                    while (await chat.ResponseStream.MoveNext(cancellationToken: CancellationToken.None))
                    {
                        var response = chat.ResponseStream.Current;
                        Console.WriteLine($"{response.User}: {response.Text}");
                    }
                });

                await chat.RequestStream.WriteAsync(new Message { User = "testUser", Text = $"testUser has joined the room" });

                string line;
                while ((line = Console.ReadLine()) != null)
                {
                    if (line.ToLower() == "bye")
                    {
                        break;
                    }
                    await chat.RequestStream.WriteAsync(new Message { User = "testUser", Text = line });
                }
                await chat.RequestStream.CompleteAsync();
            }

            Console.WriteLine("Disconnecting");
            await channel.ShutdownAsync();
        }
    }
}
