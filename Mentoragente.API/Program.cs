using Mentoragente.Domain.Interfaces;
using Mentoragente.Application.Services;
using Mentoragente.Application.Adapters;
using Mentoragente.Infrastructure.Services;
using Mentoragente.Infrastructure.Repositories;
using FluentValidation;
using Mentoragente.API.Configuration;
using System.Threading.RateLimiting;

namespace Mentoragente.API;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure global JSON serialization for enums
        Infrastructure.Extensions.JsonConfigurationExtensions.ConfigureGlobalJsonSerialization();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers(options =>
        {
            // Add global validation filter for consistent error responses
            options.Filters.Add<Filters.ValidationFilter>();
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Mentoragente API",
                Version = "v1",
                Description = "Multi-tenant SaaS platform for AI-powered WhatsApp assistants"
            });
            
            // Include XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Add Z-Api-Token security definition for Z-API webhooks
            c.AddSecurityDefinition("Z-Api-Token", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "Z-API Token for webhook authentication. Z-API automatically sends this header in webhook requests. Get this token from your Z-API instance settings.",
                Name = "Z-Api-Token",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
            });

            // Add API Key security definition for administrative endpoints
            c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "API Key for administrative endpoints. Get this key from appsettings.json (ApiKey).",
                Name = "X-API-Key",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
            });

            // Add operation filter to apply Client-Token security only to Z-API webhook endpoints
            c.OperationFilter<Filters.ZApiWebhookSecurityFilter>();
            
            // Add operation filter to apply API Key security to all endpoints with [Authorize]
            c.OperationFilter<Filters.ApiKeySecurityFilter>();
            
            // Enable Swagger annotations
            c.EnableAnnotations();
        });
        // Add comprehensive health checks
        builder.Services.AddMentoragenteHealthChecks(builder.Configuration);

        // Add memory cache for mentorship caching
        builder.Services.AddMemoryCache();

        // Configure CORS
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? Array.Empty<string>();
        
        builder.Services.AddCors(options =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // Development: Allow all origins for easier development
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            }
            else
            {
                // Production: Restrict to specific origins
                if (allowedOrigins.Length > 0)
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
                }
                else
                {
                    // If no origins configured, deny all (most secure)
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.AllowAnyMethod()
                              .AllowAnyHeader();
                        // No origins allowed = CORS will block all cross-origin requests
                    });
                }
            }
        });

        // Rate Limiting
        builder.Services.AddRateLimiter(options =>
        {
            // Webhooks: 500 req/min per IP
            options.AddPolicy("WebhookPolicy", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 500,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Administrative endpoints: 10 req/min per API Key
            options.AddPolicy("AdminPolicy", context =>
            {
                // Extract API Key from header
                var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: apiKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        // FluentValidation - using DependencyInjectionExtensions (FluentValidation.AspNetCore is deprecated)
        builder.Services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateUserRequestValidator>();

        // Authentication (API Key - optional, can be enabled per controller)
        builder.Services.AddAuthentication("ApiKey")
            .AddScheme<Middleware.ApiKeyAuthenticationOptions, Middleware.ApiKeyAuthenticationHandler>(
                "ApiKey", options => { });

        // Register Application Layer services
        builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();
        builder.Services.AddScoped<IUserOrchestrationService, UserOrchestrationService>();
        builder.Services.AddScoped<IAgentSessionOrchestrationService, AgentSessionOrchestrationService>();
        builder.Services.AddScoped<IAccessValidationService, AccessValidationService>();
        builder.Services.AddScoped<ISessionUpdateService, SessionUpdateService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IMentorshipService, MentorshipService>();
        builder.Services.AddScoped<IAgentSessionService, AgentSessionService>();
        builder.Services.AddScoped<IMentorshipCacheService, MentorshipCacheService>();
        builder.Services.AddScoped<IPhoneNumberValidator, PhoneNumberValidator>();
        builder.Services.AddScoped<IInputSanitizer, InputSanitizer>();
        builder.Services.AddScoped<ILogSanitizer, LogSanitizer>();

        // Register Infrastructure Layer services with retry policies
        builder.Services.AddHttpClient<IOpenAIAssistantService, OpenAIAssistantService>()
            .AddPolicyHandler(RetryPolicyConfiguration.GetOpenAIRetryPolicy());

        builder.Services.AddHttpClient<IEvolutionAPIService, EvolutionAPIService>()
            .AddPolicyHandler(RetryPolicyConfiguration.GetEvolutionAPIRetryPolicy());

        builder.Services.AddHttpClient<IZApiService, ZApiService>()
            .AddPolicyHandler(RetryPolicyConfiguration.GetEvolutionAPIRetryPolicy()); // Reuse same retry policy

        // Register WhatsApp adapters
        // Note: We use specific interfaces (IEvolutionWebhookAdapter, IZApiWebhookAdapter) 
        // for DI resolution, but controllers use the base interface (IWhatsAppWebhookAdapter) internally.
        // The specific interfaces are just markers - they inherit from IWhatsAppWebhookAdapter.
        builder.Services.AddScoped<IEvolutionWebhookAdapter, EvolutionWebhookAdapter>();
        builder.Services.AddScoped<IZApiWebhookAdapter, ZApiWebhookAdapter>();

        // Register WhatsApp service factory
        builder.Services.AddScoped<IWhatsAppServiceFactory, WhatsAppServiceFactory>();

        // Register Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IMentorshipRepository, MentorshipRepository>();
        builder.Services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();
        builder.Services.AddScoped<IAgentSessionDataRepository, AgentSessionDataRepository>();
        builder.Services.AddScoped<IConversationRepository, ConversationRepository>();

        var app = builder.Build();

        // Log environment
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🔧 Environment: {Environment}", app.Environment.EnvironmentName);

        // Configure the HTTP request pipeline
        // Swagger only in Development and Staging environments
        if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Staging")
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mentoragente API v1");
                c.RoutePrefix = "swagger";
            });
        }

        // Health check endpoints
        // /health/live - Liveness probe (just checks if the app is running)
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false, // Don't run any checks, just return healthy if app is running
            ResponseWriter = WriteHealthCheckResponse
        });

        // /health/ready - Readiness probe (checks all dependencies)
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"), // Only run checks tagged with "ready"
            ResponseWriter = WriteHealthCheckResponse
        });

        // /health - Overall health (all checks)
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });
        
        // Root endpoint for Render health checks
        app.MapGet("/", () => 
        {
            var endpoints = new Dictionary<string, string>
            {
                { "health", "/health" },
                { "health_live", "/health/live" },
                { "health_ready", "/health/ready" },
                { "api", "/api" }
            };

            // Only include swagger endpoint in Development/Staging
            if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Staging")
            {
                endpoints.Add("swagger", "/swagger");
            }

            return new { 
                status = "ok", 
                service = "Mentoragente API",
                version = "1.0.0",
                environment = app.Environment.EnvironmentName,
                timestamp = DateTime.UtcNow,
                endpoints = endpoints
            };
        });

        // CORS must be before UseAuthentication and UseAuthorization
        app.UseCors();

        // Disable HTTPS redirection for Render/cloud deployment
        // app.UseHttpsRedirection();

        // Global exception handling middleware (must be early in pipeline)
        app.UseMiddleware<Middleware.GlobalExceptionHandlingMiddleware>();

        // Phone number rate limiting middleware (for enrollment endpoints)
        app.UseMiddleware<Middleware.PhoneNumberRateLimitMiddleware>();

        // Enable rate limiting
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }

    private static async Task WriteHealthCheckResponse(
        Microsoft.AspNetCore.Http.HttpContext context,
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
    {
        context.Response.ContentType = "application/json";
        
        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        };

        var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}

