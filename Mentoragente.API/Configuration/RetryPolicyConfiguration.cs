using Polly;
using Polly.Extensions.Http;

namespace Mentoragente.API.Configuration;

/// <summary>
/// Configuration for retry policies used by HttpClient instances
/// </summary>
public static class RetryPolicyConfiguration
{
    /// <summary>
    /// Gets a retry policy for HTTP calls with exponential backoff
    /// Handles transient HTTP errors (5xx, 408, 429) and network errors
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <param name="baseDelaySeconds">Base delay in seconds for exponential backoff (default: 2)</param>
    /// <returns>Retry policy for HTTP responses</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries = 3, int baseDelaySeconds = 2)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Handles 5xx, 408 (RequestTimeout), and network errors
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // Also handle 429 (Rate Limit)
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(baseDelaySeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    if (outcome.Exception != null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Retry {retryCount}/{maxRetries} after {timespan.TotalSeconds}s due to exception: {outcome.Exception.Message}");
                    }
                    else if (outcome.Result != null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Retry {retryCount}/{maxRetries} after {timespan.TotalSeconds}s due to HTTP {outcome.Result.StatusCode}");
                    }
                });
    }

    /// <summary>
    /// Gets a retry policy specifically for OpenAI API calls
    /// Uses longer delays and handles rate limiting (429) more gracefully
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetOpenAIRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                {
                    // Exponential backoff: 2s, 4s, 8s
                    // For rate limits (429), use longer delays
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    if (outcome.Result?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"OpenAI API rate limit hit. Retry {retryCount}/3 after {timespan.TotalSeconds}s");
                    }
                    else if (outcome.Exception != null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"OpenAI API transient error. Retry {retryCount}/3 after {timespan.TotalSeconds}s: {outcome.Exception.Message}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"OpenAI API error. Retry {retryCount}/3 after {timespan.TotalSeconds}s (Status: {outcome.Result?.StatusCode})");
                    }
                });
    }

    /// <summary>
    /// Gets a retry policy specifically for Evolution API calls
    /// Uses shorter delays for faster message delivery
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetEvolutionAPIRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                {
                    // Exponential backoff: 1s, 2s, 4s (faster than OpenAI)
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    if (outcome.Exception != null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Evolution API transient error. Retry {retryCount}/3 after {timespan.TotalSeconds}s: {outcome.Exception.Message}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Evolution API error. Retry {retryCount}/3 after {timespan.TotalSeconds}s (Status: {outcome.Result?.StatusCode})");
                    }
                });
    }
}

