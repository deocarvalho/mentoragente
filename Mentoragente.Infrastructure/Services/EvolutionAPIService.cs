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
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly ILogger<EvolutionAPIService> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

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

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<bool> SendMessageAsync(string phoneNumber, string message, Guid mentorshipId)
    {
        try
        {
            // Get mentorship to retrieve Evolution API configuration
            var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(mentorshipId);
            if (mentorship == null)
            {
                _logger.LogError("Mentorship {MentorshipId} not found", mentorshipId);
                throw new InvalidOperationException($"Mentorship {mentorshipId} not found");
            }

            if (string.IsNullOrWhiteSpace(mentorship.EvolutionApiKey))
            {
                _logger.LogError("Evolution API Key not configured for mentorship {MentorshipId}", mentorshipId);
                throw new InvalidOperationException($"Evolution API Key not configured for mentorship {mentorshipId}");
            }

            if (string.IsNullOrWhiteSpace(mentorship.EvolutionInstanceName))
            {
                _logger.LogError("Evolution Instance Name not configured for mentorship {MentorshipId}", mentorshipId);
                throw new InvalidOperationException($"Evolution Instance Name not configured for mentorship {mentorshipId}");
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

            // Create request with mentorship-specific API key
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/message/sendText/{mentorship.EvolutionInstanceName}")
            {
                Content = content
            };
            requestMessage.Headers.Add("apikey", mentorship.EvolutionApiKey);

            var response = await _httpClient.SendAsync(requestMessage);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent successfully to {PhoneNumber} via instance {InstanceName}", phoneNumber, mentorship.EvolutionInstanceName);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send message to {PhoneNumber} via instance {InstanceName}. Status: {StatusCode}, Error: {Error}", 
                    phoneNumber, mentorship.EvolutionInstanceName, response.StatusCode, errorContent);
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
            _logger.LogError(ex, "Error sending message to {PhoneNumber} for mentorship {MentorshipId}", phoneNumber, mentorshipId);
            return false;
        }
    }
}

