﻿using System.Collections.Concurrent;
using System.Text;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Model;
using EmulatorRC.Services;
using Microsoft.AspNetCore.SignalR;

namespace EmulatorRC.API.Hubs
{
    public sealed class ImagesHub : Hub
    {
        private readonly ILogger<ImagesHub> _logger;
        private readonly IEmulatorDataRepository _emulatorDataRepository;
        public static readonly ConcurrentDictionary<string, HashSet<string>> Devices = new();

        public ImagesHub(ILogger<ImagesHub> logger, IEmulatorDataRepository emulatorDataRepository)
        {
            _logger = logger;
            _emulatorDataRepository = emulatorDataRepository;
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                var deviceId = Context.GetHttpContext()?.Request.GetDeviceIdOrDefault();
                if (deviceId is not null)
                {
                    var connectionId = Context.ConnectionId;

                    if (!Devices.TryGetValue(deviceId, out _))
                    {
                        Devices.TryAdd(deviceId, new HashSet<string>());
                    }

                    _logger.LogInformation("ADDED IE {deviceId} > {connectionId}", deviceId, connectionId);

                    Devices[deviceId].Add(connectionId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var deviceId = Context.GetHttpContext()?.Request.GetDeviceIdOrDefault();
                if (deviceId is not null)
                {
                    var connectionId = Context.ConnectionId;

                    if (Devices.TryGetValue(deviceId, out var clientIds))
                    {
                        _logger.LogInformation("REMOVED IE {deviceId} > {connectionId}", deviceId, connectionId);

                        clientIds.Remove(connectionId);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            
            return base.OnDisconnectedAsync(exception);
        }

        public async Task GetScreen()
        {
            byte[]? bytes = null;
            var deviceId = Context.GetHttpContext()?.Request.GetDeviceIdOrDefault();
            if (deviceId is not null)
            {
                bytes = _emulatorDataRepository.GetLastScreen(deviceId);
            }

            var message = new ScreenMessage
            {
                Image = bytes ?? Array.Empty<byte>()
            };
            await Clients.Caller.SendAsync("OnGetScreen", message);
        }
    }
}

