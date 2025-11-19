using Microsoft.Extensions.Diagnostics.HealthChecks;
using Supabase;
using Mentoragente.API.HealthChecks;

namespace Mentoragente.API;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddMentoragenteHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Supabase health check
        services.AddHealthChecks()
            .AddCheck<SupabaseHealthCheck>(
                "supabase",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database", "supabase", "ready" })
            .AddCheck<OpenAIHealthCheck>(
                "openai",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "external", "openai", "ready" })
            .AddCheck<ZApiHealthCheck>(
                "zapi",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "external", "whatsapp", "zapi", "ready" })
            .AddCheck<EvolutionApiHealthCheck>(
                "evolution-api",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "external", "whatsapp", "evolution", "ready" });

        // Register Supabase client factory for health check
        // We use a factory to create the client on-demand to avoid initialization issues
        services.AddSingleton<Func<Supabase.Client>>(sp => () =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var supabaseUrl = config["Supabase:Url"];
            var supabaseKey = config["Supabase:ServiceRoleKey"];
            
            if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey))
            {
                throw new InvalidOperationException("Supabase URL and ServiceRoleKey must be configured");
            }

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };
            
            return new Supabase.Client(supabaseUrl, supabaseKey, options);
        });

        // Register HttpClient for external API health checks
        services.AddHttpClient<OpenAIHealthCheck>();
        services.AddHttpClient<ZApiHealthCheck>();
        services.AddHttpClient<EvolutionApiHealthCheck>();

        return services;
    }
}

