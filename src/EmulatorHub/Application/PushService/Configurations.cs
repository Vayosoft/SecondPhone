using EmulatorHub.Application.PushService.Channels;
using EmulatorHub.Application.PushService.Commands;
using EmulatorHub.Application.PushService.Models;
using LanguageExt.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vayosoft.Commands;
using Vayosoft.PushBrokers;
using Vayosoft.Threading.Channels;

namespace EmulatorHub.Application.PushService
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
