using EmulatorHub.PushBroker.Application.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Vayosoft.PushBrokers;
using Vayosoft.Threading.Channels.Handlers;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace EmulatorHub.PushBroker.Application.Channels
{
    internal sealed class MessageChannelHandler : ChannelHandlerBase<PushMessage>
    {
        private readonly PushBrokerFactory _pushFactory;
        private readonly ILogger<MessageChannelHandler> _logger;

        public MessageChannelHandler(PushBrokerFactory pushFactory, ILogger<MessageChannelHandler> logger)
        {
            _pushFactory = pushFactory;
            _logger = logger;
        }

        protected override ValueTask HandleAsync(PushMessage message, CancellationToken token = default)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Sending push message...\r\n{payload}", message.Payload);
            }

            //_pushFactory
            //    .GetFor("Android")
            //    .Send(message.PushToken, JObject.Parse(message.Payload));

            return ValueTask.CompletedTask;
        }
    }
}
