using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace Mentoragente.API.HealthChecks;

public class OpenAIHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIHealthCheck> _logger;

    public OpenAIHealthCheck(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIHealthCheck> logger)
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
            var apiKey = _configuration["OpenAI:ApiKey"];
            var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return HealthCheckResult.Degraded("OpenAI API key is not configured");
            }

            // Use a lightweight endpoint to check API availability
            // We'll use the models endpoint which is fast and doesn't consume credits
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/models");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            
            // Set a short timeout for health checks
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("OpenAI API is accessible");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return HealthCheckResult.Degraded("OpenAI API key is invalid");
            }
            else
            {
                return HealthCheckResult.Degraded($"OpenAI API returned status code: {response.StatusCode}");
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("OpenAI health check timed out");
            return HealthCheckResult.Degraded("OpenAI API health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI health check failed");
            return HealthCheckResult.Unhealthy("OpenAI API is unreachable", ex);
        }
    }
}

