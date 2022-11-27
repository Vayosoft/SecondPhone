using EmulatorHub.API.Protos;
using Grpc.Core;

namespace EmulatorHub.API.Services
{
    public class IntercomService : HubRoom.HubRoomBase
    {
        private readonly IntercomHub _hub;

        public IntercomService(IntercomHub hub)
        {
            _hub = hub;
        }

        public override async Task join(IAsyncStreamReader<Message> requestStream, IServerStreamWriter<Message> responseStream, ServerCallContext context)
        {
            if (!await requestStream.MoveNext()) return;

            do
            {
                _hub.Join(requestStream.Current.User, responseStream);
                await _hub.BroadcastMessageAsync(requestStream.Current);
            } while (await requestStream.MoveNext());

            _hub.Remove(context.Peer);
        }
    }
}
