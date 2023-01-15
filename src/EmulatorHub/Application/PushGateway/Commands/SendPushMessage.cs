using EmulatorHub.Application.PushGateway.Channels;
using EmulatorHub.Application.PushGateway.Models;
using EmulatorHub.Domain.Entities;
using FluentValidation;
using LanguageExt.Common;
using MediatR;
using Vayosoft.Commands;
using Vayosoft.Commons.Exceptions;
using Vayosoft.Persistence;
using Vayosoft.Persistence.Criterias;
using Vayosoft.Threading.Channels;
using ValidationException = FluentValidation.ValidationException;


namespace EmulatorHub.Application.PushGateway.Commands
{
    public sealed record SendPushMessage(string DeviceId, string Message) : ICommand<Result<Unit>>
    {
        public class SendPushMessageValidator : AbstractValidator<SendPushMessage>
        {
            public SendPushMessageValidator()
            {
                RuleFor(m => m.DeviceId)
                    .NotEmpty().WithMessage("DeviceId has not provided.");
                RuleFor(m => m.Message)
                    .NotEmpty().WithMessage("Payload is empty.");
            }
        }
    }

    internal sealed class HandleSendPushMessage : ICommandHandler<SendPushMessage, Result<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<SendPushMessage> _validator;
        private readonly HandlerChannel<PushMessage, MessageChannelHandler> _channel;


        public HandleSendPushMessage(
            IUnitOfWork unitOfWork,
            IValidator<SendPushMessage> validator,
            HandlerChannel<PushMessage, MessageChannelHandler> channel
            )
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
            _channel = channel;
        }

        public async Task<Result<Unit>> Handle(SendPushMessage request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var exception = new ValidationException(validationResult.Errors);
                return new Result<Unit>(exception);
                //return validationResult.Errors.ToErrorOr<TrackedItem>();
            }

            var criteria = new Criteria<Emulator>(e => e.Id == request.DeviceId)
                .Include(e => e.Client);

            var emulator = await _unitOfWork.FindAsync(criteria, cancellationToken);
            if (emulator == null)
            {
                return new Result<Unit>(new EntityNotFoundException(nameof(Emulator), request.DeviceId));
            }

            if (string.IsNullOrEmpty(emulator.Client.PushToken))
            {
                return new Result<Unit>(new ValidationException("The client has no token."));
            }

            _channel.Enqueue(new PushMessage(emulator.Client.PushToken, request.Message));

            return Unit.Value;
        }
    }

}
