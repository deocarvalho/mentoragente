using Microsoft.Extensions.Diagnostics.HealthChecks;
using Supabase;

namespace Mentoragente.API.HealthChecks;

public class SupabaseHealthCheck : IHealthCheck
{
    private readonly Func<Supabase.Client> _supabaseClientFactory;
    private readonly ILogger<SupabaseHealthCheck> _logger;

    public SupabaseHealthCheck(Func<Supabase.Client> supabaseClientFactory, ILogger<SupabaseHealthCheck> logger)
    {
        _supabaseClientFactory = supabaseClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create client on-demand
            var supabaseClient = _supabaseClientFactory();
            
            // Try to query a simple table to verify database connectivity
            var response = await supabaseClient
                .From<Mentoragente.Domain.Entities.User>()
                .Select("id")
                .Limit(1)
                .Get(cancellationToken);

            return HealthCheckResult.Healthy("Supabase database connection is working");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase health check failed");
            return HealthCheckResult.Unhealthy("Supabase database connection failed", ex);
        }
    }
}

