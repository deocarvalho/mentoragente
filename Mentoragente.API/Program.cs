using Mentoragente.Domain.Interfaces;
using Mentoragente.Application.Services;
using Mentoragente.Infrastructure.Services;
using Mentoragente.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

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

        // Register Application Layer services
        builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();

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

        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}

