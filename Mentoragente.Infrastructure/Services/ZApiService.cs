using System.Text;
using System.Text.Json;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Infrastructure.Services;

public class ZApiService : IZApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly ILogger<ZApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _clientToken; // Global Client-Token for all instances
    private readonly JsonSerializerOptions _jsonOptions;

    public WhatsAppProvider Provider => WhatsAppProvider.ZApi;

    public ZApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMentorshipRepository mentorshipRepository,
        ILogger<ZApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _mentorshipRepository = mentorshipRepository;
        _logger = logger;
        _baseUrl = _configuration["ZApi:BaseUrl"] ?? throw new InvalidOperationException("Z-API base URL not configured");
        _clientToken = _configuration["ZApi:Client-Token"] ?? throw new InvalidOperationException("Z-API Client-Token not configured");

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
            if (string.IsNullOrWhiteSpace(mentorship.InstanceCode))
            {
                _logger.LogError("Instance code not configured for mentorship {MentorshipId}", mentorship.Id);
                throw new InvalidOperationException($"Instance code not configured for mentorship {mentorship.Id}");
            }

            if (string.IsNullOrWhiteSpace(mentorship.InstanceToken))
            {
                _logger.LogError("Instance token not configured for mentorship {MentorshipId}", mentorship.Id);
                throw new InvalidOperationException($"Instance token not configured for mentorship {mentorship.Id}");
            }

            var requestBody = new
            {
                phone = phoneNumber,
                message = message
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Z-API endpoint format: /instances/{instanceId}/token/{instanceToken}/send-text
            // Client-Token goes in the header (global for all instances)
            var endpoint = $"{_baseUrl}/instances/{mentorship.InstanceCode}/token/{mentorship.InstanceToken}/send-text";
            
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            requestMessage.Headers.Add("Client-Token", _clientToken);
            
            var response = await _httpClient.SendAsync(requestMessage);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent successfully to {PhoneNumber} via Z-API instance {InstanceCode}", phoneNumber, mentorship.InstanceCode);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send message to {PhoneNumber} via Z-API instance {InstanceCode}. Status: {StatusCode}, Error: {Error}", 
                    phoneNumber, mentorship.InstanceCode, response.StatusCode, errorContent);
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

