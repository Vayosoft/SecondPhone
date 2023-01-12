using EmulatorHub.PushService.Application.Channels;
using EmulatorHub.PushService.Application.Commands;
using EmulatorHub.PushService.Application.Models;
using LanguageExt.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vayosoft.Commands;
using Vayosoft.PushBrokers;
using Vayosoft.Threading.Channels;

namespace EmulatorHub.PushService
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
