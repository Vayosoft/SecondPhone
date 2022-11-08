using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using EmulatorRC.API.Model;

namespace EmulatorRC.API.Controllers
{
    [ApiController]
    public sealed class ErrorsController : ControllerBase
    {
        [Route("/error")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Error()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var codeInfo = exceptionFeature?.Error.GetHttpStatus();
            return Problem(title: "An error occurred while processing your request.",
                statusCode: (int)(codeInfo?.Code ?? HttpStatusCode.InternalServerError));
        }
    }
}
