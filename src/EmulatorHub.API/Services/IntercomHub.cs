using System.Collections.Concurrent;
using EmulatorHub.API.Protos;
using Grpc.Core;

namespace EmulatorHub.API.Services
{
    public class IntercomHub
    {
        public ICollection<string> Users => _users.Keys;

        private readonly ConcurrentDictionary<string, IServerStreamWriter<Message>> _users = new();

        public void Join(string name, IServerStreamWriter<Message> response) => _users.TryAdd(name, response);

        public void Remove(string name) => _users.TryRemove(name, out _);

        public async Task BroadcastMessageAsync(Message message) => await BroadcastMessages(message);

        private async Task BroadcastMessages(Message message)
        {
            foreach (var user in _users.Where(x => x.Key != message.User))
            {
                var item = await SendMessageToSubscriber(user, message);
                if (item != null)
                {
                    Remove(item.Value.Key);
                }
            }
        }

        private static async Task<KeyValuePair<string, IServerStreamWriter<Message>>?> SendMessageToSubscriber(
            KeyValuePair<string, IServerStreamWriter<Message>> user, Message message)
        {
            try
            {
                await user.Value.WriteAsync(message);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return user;
            }
        }
    }
}
