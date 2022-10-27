using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MySignalRTest.Hubs;
using MySignalRTest.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MySignalRTest.Controllers
{

    [ApiController]
    [Route("api")]
    public class FileUploadController : ControllerBase
    {

        private readonly IHubContext<ImagesHub> _hubContext;
        private readonly IEmulatorDataRepository _emulatorDataRepository;

        public FileUploadController(IHubContext<ImagesHub> hubContext, IEmulatorDataRepository emulatorDataRepository)
        {
            _hubContext = hubContext;
            _emulatorDataRepository = emulatorDataRepository;

        }

        private void DeleteFileOlderThan(string path,DateTime date)
        {
            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.LastAccessTime < date)
                    fi.Delete();
            }
        }

        private string RefreshAndGetDeviceId()
        {
            var deviceId = Request.Headers["X-DEVICE-ID"].FirstOrDefault("DEFAULT");//TryGetValue("X-DEVICE-ID", out deviceId);

            //_memoryCache.Set(deviceId, "{}", new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(90) });
            
            return deviceId;
        }

        private string GetDeviceId()
        {
            return Request.Headers.FirstOrDefault(x => x.Key == "X-DEVICE-ID").Value.FirstOrDefault() ?? "DEFAULT";
        }

        [Route("screen/sendlast")]
        [HttpGet]
        public async Task<IActionResult> LastScreen()
        {
            try
            {
                var id = _emulatorDataRepository.GetLastScreenId(GetDeviceId());
                if (id is null) {
                    return NotFound();
                }

                await _hubContext.Clients.All.SendAsync("ImageMessage", $"{id}");

                return Ok("THANKS");
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");

            }
        }

        [Route("screen/{id}")]
        [HttpGet]
        public IActionResult GetScreen(string id)
        {
            try
            {
                var deviceId = GetDeviceId();
                
                var bytes = _emulatorDataRepository.GetScreen(deviceId, id);
                if (bytes is null)
                {
                    if (id == _emulatorDataRepository.GetLastScreenId(deviceId))
                        bytes = _emulatorDataRepository.GetLastScreen(deviceId);
                    if (bytes is null)
                        return NotFound();
                }
                return File(bytes, "image/jpeg");
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [Route("upload")]
        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> UploadAsync()
        {
            try
            {
                var deviceId = RefreshAndGetDeviceId();

                var id = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                using (var memoryStream = new MemoryStream())
                {
                    await Request.Body.CopyToAsync(memoryStream);
                    var array = memoryStream.ToArray();
                    _emulatorDataRepository.SetScreen(deviceId, $"{id}", array);
                    _emulatorDataRepository.SetLastScreenId(deviceId, $"{id}");
                    _emulatorDataRepository.SetLastScreen(deviceId, array);
                }

                if (ImagesHub.Devices.TryGetValue(deviceId, out var clientIds) && clientIds.Count > 0)
                {
                    await _hubContext.Clients.Clients(clientIds.ToArray()).SendAsync("ImageMessage", $"{id}");
                }

                return Ok("THANKS");
             
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }



        /*
         
         
         
        [Route("screen/{id}")]
        [HttpGet]
        public IActionResult GetScreen(String id)
        {

            string path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "UploadedFiles"));
            string filePath = Path.Combine(path, $"{id}.jpg");

            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(path);
            }
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();

            }


            DeleteFileOlderThan(path, DateTime.Now.AddMinutes(-1));
            byte[] bytes = System.IO.File.ReadAllBytes(filePath);
            //FileInfo fi = new FileInfo(filePath);
            //DeleteFileOlderThan(path, fi.CreationTime);
            //System.IO.File.Delete(filePath);
            return File(bytes, "image/jpeg");
        }

        [Route("upload")]
        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> UploadAsync()
        {
            string path = "";
            try
            {

                long id = DateTime.UtcNow.Ticks;

                path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "UploadedFiles"));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                using (var fileStream = new FileStream(Path.Combine(path, $"_{id}.jpg"), FileMode.Create))
                {
                    //await file.CopyToAsync(fileStream);
                    await Request.Body.CopyToAsync(fileStream);
                }
                DeleteFileOlderThan(path, DateTime.Now.AddMinutes(-1));
                System.IO.File.Move(Path.Combine(path, $"_{id}.jpg"), Path.Combine(path, $"{id}.jpg"), true);
                await _hubContext.Clients.All.SendAsync("ImageMessage", $"{id}");

                return Ok("THANKS");
             
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

         */

    }
}