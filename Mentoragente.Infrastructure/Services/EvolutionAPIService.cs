using System.Text;
using System.Text.Json;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Infrastructure.Services;

public class EvolutionAPIService : IEvolutionAPIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly ILogger<EvolutionAPIService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public WhatsAppProvider Provider => WhatsAppProvider.EvolutionAPI;

    public EvolutionAPIService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMentorshipRepository mentorshipRepository,
        ILogger<EvolutionAPIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _mentorshipRepository = mentorshipRepository;
        _logger = logger;
        _baseUrl = _configuration["EvolutionAPI:BaseUrl"] ?? throw new InvalidOperationException("Evolution API base URL not configured");
        _apiKey = _configuration["EvolutionAPI:ApiKey"] ?? throw new InvalidOperationException("Evolution API key not configured");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<bool> SendMessageAsync(string phoneNumber, string message, Guid mentorshipId)
    {
        var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(mentorshipId);
        if (mentorship == null)
        {
            _logger.LogError("Mentorship {MentorshipId} not found", mentorshipId);
            throw new InvalidOperationException($"Mentorship {mentorshipId} not found");
        }

        return await SendMessageAsync(phoneNumber, message, mentorship);
    }

    public async Task<bool> SendMessageAsync(string phoneNumber, string message, Mentorship mentorship)
    {
        try
        {
            // Use instance_code, fallback to EvolutionInstanceName for backward compatibility
            var instanceCode = !string.IsNullOrWhiteSpace(mentorship.InstanceCode) 
                ? mentorship.InstanceCode 
                : mentorship.EvolutionInstanceName;

            if (string.IsNullOrWhiteSpace(instanceCode))
            {
                _logger.LogError("Instance code not configured for mentorship {MentorshipId}", mentorship.Id);
                throw new InvalidOperationException($"Instance code not configured for mentorship {mentorship.Id}");
            }

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

            // Use global API key from configuration
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/message/sendText/{instanceCode}")
            {
                Content = content
            };
            requestMessage.Headers.Add("apikey", _apiKey);

            var response = await _httpClient.SendAsync(requestMessage);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent successfully to {PhoneNumber} via instance {InstanceCode}", phoneNumber, instanceCode);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send message to {PhoneNumber} via instance {InstanceCode}. Status: {StatusCode}, Error: {Error}", 
                    phoneNumber, instanceCode, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (InvalidOperationException)
        {
            // Re-throw validation exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to {PhoneNumber} for mentorship {MentorshipId}", phoneNumber, mentorship.Id);
            return false;
        }
    }
}

