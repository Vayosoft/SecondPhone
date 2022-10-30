﻿using EmulatorRC.API.Hubs;
using EmulatorRC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EmulatorRC.API.Extensions;


namespace EmulatorRC.API.Controllers
{

    [Route("api/[Controller]")]
    public class ScreenController : ApiControllerBase 
    {

        private readonly IHubContext<ImagesHub> _imageHub;
        private readonly IEmulatorDataRepository _emulatorDataRepository;

        public ScreenController(IHubContext<ImagesHub> imageHub, IEmulatorDataRepository emulatorDataRepository)
        {
            _imageHub = imageHub;
            _emulatorDataRepository = emulatorDataRepository;

        }

        [HttpGet("sendLast")]
        public async Task<IActionResult> LastScreen()
        {
            var deviceId = Request.GetDeviceIdOrDefault("DEFAULT");

            var imageId = _emulatorDataRepository.GetLastScreenId(deviceId);
            if (imageId is null)
            {
                return NotFound();
            }

            await _imageHub.Clients.All.SendAsync("ImageMessage", imageId);

            return Ok("THANKS");
        }

        [HttpGet("getLast")]
        public IActionResult GetLastScreen()
        {
            var deviceId = Request.GetDeviceIdOrDefault("DEFAULT");

            var bytes = _emulatorDataRepository.GetLastScreen(deviceId);
            if (bytes is null)
            {
                return NotFound();
            }

            return File(bytes, "image/jpeg");
        }

        [HttpGet("{id}")]
        public IActionResult GetScreen(string id)
        {
            var deviceId = Request.GetDeviceIdOrDefault("DEFAULT");

            var bytes = _emulatorDataRepository.GetScreen(deviceId, id);
            if (bytes is null)
            {
                if (id == _emulatorDataRepository.GetLastScreenId(deviceId))
                    bytes = _emulatorDataRepository.GetLastScreen(deviceId);
                if (bytes is null)
                {
                    return NotFound();
                }
            }
            return File(bytes, "image/jpeg");
        }
    }
}