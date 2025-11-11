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

    private async Task<JsonNode> PostJsonAsync(string endpoint, object payload)
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
            response.EnsureSuccessStatusCode();
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
            await PostJsonAsync($"threads/{threadId}/messages", new
            {
                role = "user",
                content = userMessage
            });
            _logger.LogDebug("Added user message to thread {ThreadId}", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user message to thread {ThreadId}", threadId);
            throw;
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

