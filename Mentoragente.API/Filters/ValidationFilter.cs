using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mentoragente.API.Filters;

/// <summary>
/// Action filter to automatically handle FluentValidation errors
/// This provides consistent validation error responses
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Check if there are validation errors in ModelState
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            context.Result = new BadRequestObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Validation Error",
                status = 400,
                detail = "One or more validation errors occurred",
                errors = errors,
                traceId = context.HttpContext.TraceIdentifier
            });

            return;
        }

        await next();
    }
}

