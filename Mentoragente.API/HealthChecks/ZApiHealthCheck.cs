using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace Mentoragente.API.HealthChecks;

public class ZApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZApiHealthCheck> _logger;

    public ZApiHealthCheck(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ZApiHealthCheck> logger)
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
            var baseUrl = _configuration["ZApi:BaseUrl"] ?? "https://api.z-api.io";
            var clientToken = _configuration["ZApi:Client-Token"];

            if (string.IsNullOrWhiteSpace(clientToken))
            {
                return HealthCheckResult.Degraded("Z-API Client-Token is not configured");
            }

            // Z-API doesn't have a dedicated health endpoint, so we'll check if the base URL is reachable
            // or use a lightweight endpoint if available
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/");
            request.Headers.Add("Client-Token", clientToken);
            
            // Set a short timeout for health checks
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.SendAsync(request, cts.Token);

            // Even if we get a 404 or other error, if we get a response, the service is reachable
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return HealthCheckResult.Degraded("Z-API Client-Token is invalid");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.IsSuccessStatusCode)
            {
                // Service is reachable (404 is expected for root endpoint, but means service is up)
                return HealthCheckResult.Healthy("Z-API service is reachable");
            }
            else
            {
                return HealthCheckResult.Degraded($"Z-API returned status code: {response.StatusCode}");
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Z-API health check timed out");
            return HealthCheckResult.Degraded("Z-API health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Z-API health check failed");
            return HealthCheckResult.Unhealthy("Z-API service is unreachable", ex);
        }
    }
}

