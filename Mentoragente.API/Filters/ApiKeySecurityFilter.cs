using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace Mentoragente.API.Filters;

/// <summary>
/// Swagger operation filter that adds API Key security requirement to endpoints with [Authorize]
/// </summary>
public class ApiKeySecurityFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the endpoint has [Authorize] attribute
        var hasAuthorize = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Any() == true
            || context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Any();

        // Skip if it's a webhook endpoint (they use Client-Token, not API Key)
        var isWebhook = context.MethodInfo.DeclaringType?.Name?.Contains("Webhook") == true;

        if (hasAuthorize && !isWebhook)
        {
            if (operation.Security == null)
            {
                operation.Security = new List<OpenApiSecurityRequirement>();
            }

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}


