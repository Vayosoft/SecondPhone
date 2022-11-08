using EmulatorRC.API.Hubs;
using EmulatorRC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EmulatorRC.API.Extensions;

namespace EmulatorRC.API.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class UploadController : ControllerBase
    {

        private readonly IHubContext<ImagesHub> _imageHub;
        private readonly IEmulatorDataRepository _emulatorDataRepository;

        public UploadController(IHubContext<ImagesHub> imageHub, IEmulatorDataRepository emulatorDataRepository)
        {
            _imageHub = imageHub;
            _emulatorDataRepository = emulatorDataRepository;

        }

        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> PostAsync()
        {
            var deviceId = Request.GetDeviceIdOrDefault("DEFAULT")!;
            var imageId = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString();

            using (var memoryStream = new MemoryStream())
            {
                await Request.Body.CopyToAsync(memoryStream);
                var array = memoryStream.ToArray();

                _emulatorDataRepository.SetScreen(deviceId, imageId, array);
                _emulatorDataRepository.SetLastScreenId(deviceId, imageId);
                _emulatorDataRepository.SetLastScreen(deviceId, new Screen(imageId, array));
            }

            if (ImagesHub.Devices.TryGetValue(deviceId, out var clientIds) && clientIds.Count > 0)
            {
                await _imageHub.Clients.Clients(clientIds.ToArray()).SendAsync("ImageMessage", imageId);
            }

            return Ok("THANKS");
        }
    }
}