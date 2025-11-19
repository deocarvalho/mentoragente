using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Mentoragente.API.Filters;

/// <summary>
/// Swagger operation filter that adds Z-Api-Token security requirement to Z-API webhook endpoints
/// </summary>
public class ZApiWebhookSecurityFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Only apply to Z-API webhook controller
        if (context.MethodInfo.DeclaringType?.Name == "ZApiWebhookController")
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
                            Id = "Z-Api-Token"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}

