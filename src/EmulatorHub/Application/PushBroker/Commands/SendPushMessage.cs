using MediatR;
using Vayosoft.Commands;

namespace EmulatorHub.Application.PushBroker.Commands
{
    public sealed record SendPushMessage(string DeviceId, string Message) : ICommand
    {

    }

    internal sealed class HandleSendPushMessage : ICommandHandler<SendPushMessage>
    {
        public HandleSendPushMessage()
        {

        }

        public Task<Unit> Handle(SendPushMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

}
