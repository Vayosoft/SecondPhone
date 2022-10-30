using System.Net;
using EmulatorRC.API.Errors;
using LanguageExt.Common;
using Microsoft.AspNetCore.Mvc;

namespace EmulatorRC.API.Controllers
{
    [ApiController]
    public class ApiControllerBase : ControllerBase
    {
        protected IActionResult Result<TResult>(Result<TResult> result)
        {
            return result.Match(obj => Ok(obj), Problem);
        }

        protected IActionResult Result<TResult, TContract>(Result<TResult> result, Func<TResult, TContract> mapper)
        {
            return result.Match(obj => Ok(mapper(obj)), Problem);
        }

        protected IActionResult Problem(Exception? exception)
        {
            switch (exception)
            {
                default:
                    var codeInfo = exception?.GetHttpStatusCodeInfo();
                    return Problem(title: "An error occurred while processing your request.",
                        statusCode: (int)(codeInfo?.Code ?? HttpStatusCode.InternalServerError));
            }
        }
    }
}
