using Mentoragente.Domain.Interfaces;
using Mentoragente.Application.Services;
using Mentoragente.Infrastructure.Services;
using Mentoragente.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace Mentoragente.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        // FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<Mentoragente.Application.Validators.CreateUserRequestValidator>();
        builder.Services.AddFluentValidationAutoValidation();

        // Authentication (API Key - optional, can be enabled per controller)
        builder.Services.AddAuthentication("ApiKey")
            .AddScheme<Mentoragente.API.Middleware.ApiKeyAuthenticationOptions, Mentoragente.API.Middleware.ApiKeyAuthenticationHandler>(
                "ApiKey", options => { });

        // Register Application Layer services
        builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IMentoriaService, MentoriaService>();
        builder.Services.AddScoped<IAgentSessionService, AgentSessionService>();

        // Register Infrastructure Layer services
        builder.Services.AddHttpClient<IOpenAIAssistantService, OpenAIAssistantService>();
        builder.Services.AddHttpClient<IEvolutionAPIService, EvolutionAPIService>();

        // Register Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IMentoriaRepository, MentoriaRepository>();
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

        app.MapHealthChecks("/health");

        // Disable HTTPS redirection for Render/cloud deployment
        // app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}

