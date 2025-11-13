using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mentoragente.Domain.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mentoragente.Infrastructure.Services;

public class OpenAIAssistantService : IOpenAIAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIAssistantService> _logger;

    public OpenAIAssistantService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIAssistantService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        var baseUrl = _configuration["OpenAI:BaseUrl"] ?? throw new InvalidOperationException("OpenAI API base address not configured");

        // Ensure baseUrl ends with a slash for proper URI combination
        // HttpClient requires BaseAddress to end with / for relative URIs to append correctly
        if (!baseUrl.EndsWith("/"))
        {
            baseUrl = baseUrl + "/";
        }

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
    }

    private async Task<JsonNode> PostJsonAsync(string endpoint, object payload, bool throwOnError = true)
    {
        // Remove leading slash if present - HttpClient will combine with BaseAddress
        // If BaseAddress is "https://api.openai.com/v1" and endpoint is "threads",
        // it becomes "https://api.openai.com/v1/threads"
        if (endpoint.StartsWith("/"))
        {
            endpoint = endpoint.TrimStart('/');
        }

        // Construct full URL for logging - ensure proper combination
        var baseAddress = _httpClient.BaseAddress?.ToString() ?? "";
        // If baseAddress doesn't end with /, add it for proper combination
        if (!baseAddress.EndsWith("/") && !string.IsNullOrEmpty(endpoint))
        {
            baseAddress = baseAddress + "/";
        }
        var fullUrl = baseAddress + endpoint;
        _logger.LogDebug("OpenAI API request: POST {Url}", fullUrl);

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        var json = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI API error: {StatusCode} - {Response}", response.StatusCode, json);
            
            if (throwOnError)
            {
                // Check if error is about active run
                try
                {
                    var errorJson = JsonNode.Parse(json);
                    var errorMessage = errorJson?["error"]?["message"]?.ToString() ?? "";
                    if (errorMessage.Contains("while a run") || errorMessage.Contains("run is active"))
                    {
                        throw new InvalidOperationException("RUN_ACTIVE");
                    }
                }
                catch (InvalidOperationException)
                {
                    // Re-throw the RUN_ACTIVE exception
                    throw;
                }
                catch
                {
                    // If JSON parsing fails, continue with normal error handling
                }
                
                response.EnsureSuccessStatusCode();
            }
        }
        
        return JsonNode.Parse(json)!;
    }

    public async Task<string> CreateThreadAsync()
    {
        try
        {
            var result = await PostJsonAsync("threads", new { });
            var threadId = result["id"]!.ToString();
            _logger.LogInformation("Created OpenAI thread: {ThreadId}", threadId);
            return threadId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating OpenAI thread");
            throw;
        }
    }

    public async Task AddUserMessageAsync(string threadId, string userMessage)
    {
        try
        {
            // Wait for any active runs to complete before adding message
            await WaitForActiveRunsToCompleteAsync(threadId);
            
            await PostJsonAsync($"threads/{threadId}/messages", new
            {
                role = "user",
                content = userMessage
            });
            _logger.LogDebug("Added user message to thread {ThreadId}", threadId);
        }
        catch (InvalidOperationException ex) when (ex.Message == "RUN_ACTIVE")
        {
            // If we still get the error (race condition), wait and retry once
            _logger.LogWarning("Run was active when adding message, waiting and retrying for thread {ThreadId}", threadId);
            await WaitForActiveRunsToCompleteAsync(threadId);
            
            await PostJsonAsync($"threads/{threadId}/messages", new
            {
                role = "user",
                content = userMessage
            });
            _logger.LogDebug("Added user message to thread {ThreadId} after waiting", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user message to thread {ThreadId}", threadId);
            throw;
        }
    }

    private async Task WaitForActiveRunsToCompleteAsync(string threadId)
    {
        const int maxWaitTime = 60; // Maximum 60 seconds
        const int pollInterval = 1000; // Check every second
        var elapsed = 0;

        while (elapsed < maxWaitTime)
        {
            var activeRun = await GetActiveRunAsync(threadId);
            if (activeRun == null)
            {
                return; // No active run
            }

            var runId = activeRun["id"]!.ToString();
            var status = activeRun["status"]!.ToString();
            
            _logger.LogDebug("Waiting for run {RunId} to complete. Status: {Status}", runId, status);

            if (status == "completed" || status == "failed" || status == "cancelled" || status == "expired")
            {
                _logger.LogDebug("Run {RunId} completed with status {Status}", runId, status);
                return;
            }

            await Task.Delay(pollInterval);
            elapsed += pollInterval / 1000;
        }

        _logger.LogWarning("Timeout waiting for active runs to complete for thread {ThreadId}", threadId);
    }

    private async Task<JsonNode?> GetActiveRunAsync(string threadId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"threads/{threadId}/runs?limit=1");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
            var runs = json["data"]?.AsArray();
            
            if (runs == null || runs.Count == 0)
            {
                return null;
            }

            var latestRun = runs[0]!;
            var status = latestRun["status"]!.ToString();
            
            // Check if run is in a state that blocks adding messages
            if (status == "queued" || status == "in_progress" || status == "requires_action")
            {
                return latestRun;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for active runs in thread {ThreadId}", threadId);
            return null;
        }
    }

    public async Task<string> RunAssistantAsync(string threadId, string assistantId)
    {
        try
        {
            var run = await PostJsonAsync($"threads/{threadId}/runs", new
            {
                assistant_id = assistantId
            });

            var runId = run["id"]!.ToString();
            _logger.LogDebug("Started run {RunId} for thread {ThreadId} with assistant {AssistantId}", runId, threadId, assistantId);

            // Poll until the run is complete
            while (true)
            {
                await Task.Delay(1000);
                var response = await _httpClient.GetAsync($"threads/{threadId}/runs/{runId}");
                var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
                var status = json["status"]!.ToString();
                
                if (status == "completed")
                    break;
                    
                if (status == "failed" || status == "cancelled" || status == "expired")
                {
                    var error = json["last_error"]?.ToString() ?? "Unknown error";
                    _logger.LogError("Run {RunId} ended with status {Status}: {Error}", runId, status, error);
                    throw new Exception($"Run ended with status {status}: {error}");
                }
            }

            // Get last assistant message
            var messagesResponse = await _httpClient.GetAsync($"threads/{threadId}/messages?limit=1");
            var messagesJson = JsonNode.Parse(await messagesResponse.Content.ReadAsStringAsync())!;
            var lastMessage = messagesJson["data"]![0]!;
            var text = lastMessage["content"]![0]!["text"]!["value"]!.ToString();
            
            _logger.LogDebug("Retrieved response from thread {ThreadId}", threadId);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running assistant for thread {ThreadId}", threadId);
            throw;
        }
    }
}

