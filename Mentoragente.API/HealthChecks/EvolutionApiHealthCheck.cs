using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace Mentoragente.API.HealthChecks;

public class EvolutionApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EvolutionApiHealthCheck> _logger;

    public EvolutionApiHealthCheck(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EvolutionApiHealthCheck> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["EvolutionAPI:BaseUrl"];
            var apiKey = _configuration["EvolutionAPI:ApiKey"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return HealthCheckResult.Degraded("Evolution API BaseUrl is not configured");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return HealthCheckResult.Degraded("Evolution API key is not configured");
            }

            // Evolution API typically has a health or status endpoint
            // Try a lightweight endpoint to check availability
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/");
            request.Headers.Add("apikey", apiKey);
            
            // Set a short timeout for health checks
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.SendAsync(request, cts.Token);

            // Even if we get a 404 or other error, if we get a response, the service is reachable
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return HealthCheckResult.Degraded("Evolution API key is invalid");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.IsSuccessStatusCode)
            {
                // Service is reachable
                return HealthCheckResult.Healthy("Evolution API service is reachable");
            }
            else
            {
                return HealthCheckResult.Degraded($"Evolution API returned status code: {response.StatusCode}");
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Evolution API health check timed out");
            return HealthCheckResult.Degraded("Evolution API health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evolution API health check failed");
            return HealthCheckResult.Unhealthy("Evolution API service is unreachable", ex);
        }
    }
}

