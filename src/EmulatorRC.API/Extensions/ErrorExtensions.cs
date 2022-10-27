using System.Net;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace EmulatorRC.API.Extensions
{
    public static class ErrorExtensions
    {
        public static ValidationProblemDetails ToProblemDetails(this IEnumerable<ValidationFailure> failures, string instance)
        {
            var errors = failures
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(
                    failureGroup => failureGroup.Key, 
                    failureGroup => failureGroup.ToArray());

            return new ValidationProblemDetails(errors)
           {
               Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
               Title = "One or more validation errors occurred.",
               Status = (int)HttpStatusCode.BadRequest,
               Instance = instance,
           };
        }
    }
}
