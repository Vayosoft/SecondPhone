using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using EmulatorHub.API.Model;

namespace EmulatorHub.API.Controllers
{
    //[ApiController]
    //public class ErrorsController : ControllerBase
    //{
    //    [Route("/error")]
    //    [ApiExplorerSettings(IgnoreApi = true)]
    //    public IActionResult Error()
    //    {
    //        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
    //        return Problem(
    //            statusCode: (int)(exceptionFeature?.Error.ToHttpStatusCode() ?? HttpStatusCode.InternalServerError),
    //            title: "An error occurred while processing your request.",
    //            detail: exceptionFeature?.Error.Message ?? string.Empty
    //        );
    //    }
    //}
}
