using EmulatorHub.PushBroker.Application.Channels;
using EmulatorHub.PushBroker.Application.Commands;
using EmulatorHub.PushBroker.Application.Models;
using LanguageExt.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vayosoft.Commands;
using Vayosoft.PushBrokers;
using Vayosoft.Threading.Channels;

namespace EmulatorHub.PushBroker
{
    public static class Configurations
    {
        public static IServiceCollection AddPushService(this IServiceCollection services)
        {
            services.AddPushBrokers();
            services.AddSingleton<MessageChannelHandler>();
            services.AddSingleton<HandlerChannel<PushMessage, MessageChannelHandler>>();
            services.AddCommandHandler<SendPushMessage, Result<Unit>, HandleSendPushMessage>();
            return services;
        }
    }
}
