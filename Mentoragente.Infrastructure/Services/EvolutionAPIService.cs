using System.Text;
using System.Text.Json;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Infrastructure.Services;

public class EvolutionAPIService : IEvolutionAPIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EvolutionAPIService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly string _instanceName;
    private readonly JsonSerializerOptions _jsonOptions;

    public EvolutionAPIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EvolutionAPIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["EvolutionAPI:BaseUrl"] ?? throw new InvalidOperationException("Evolution API base URL not configured");
        _apiKey = _configuration["EvolutionAPI:ApiKey"] ?? throw new InvalidOperationException("Evolution API key not configured");
        _instanceName = _configuration["EvolutionAPI:InstanceName"] ?? throw new InvalidOperationException("Evolution API instance name not configured");
        
        _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<bool> SendMessageAsync(string phoneNumber, string message)
    {
        try
        {
            var requestBody = new
            {
                number = phoneNumber,
                options = new 
                { 
                    delay = 1200,
                    presence = "composing" 
                },
                textMessage = new
                {
                    text = message
                }
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/message/sendText/{_instanceName}", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent successfully to {PhoneNumber}", phoneNumber);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send message to {PhoneNumber}. Status: {StatusCode}, Error: {Error}", 
                    phoneNumber, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}

