using Mentoragente.Domain.Interfaces;
using Mentoragente.Application.Services;
using Mentoragente.Application.Adapters;
using Mentoragente.Infrastructure.Services;
using Mentoragente.Infrastructure.Repositories;
using FluentValidation;
using Mentoragente.API.Configuration;

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
        });
        builder.Services.AddHealthChecks();

        // Add memory cache for mentorship caching
        builder.Services.AddMemoryCache();

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
        app.UseSwagger();
        app.UseSwaggerUI();

        // Health check endpoint
        app.MapHealthChecks("/health");
        
        // Root endpoint for Render health checks
        app.MapGet("/", () => new { 
            status = "ok", 
            service = "Mentoragente API",
            version = "1.0.0",
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow,
            endpoints = new {
                health = "/health",
                swagger = "/swagger",
                api = "/api"
            }
        });

        // Disable HTTPS redirection for Render/cloud deployment
        // app.UseHttpsRedirection();

        // Global exception handling middleware (must be early in pipeline)
        app.UseMiddleware<Middleware.GlobalExceptionHandlingMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}

