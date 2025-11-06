using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Mentoragente.Domain.Entities;
using System.Text.Json;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly IEvolutionAPIService _evolutionAPIService;
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IMessageProcessor messageProcessor,
        IEvolutionAPIService evolutionAPIService,
        IMentorshipRepository mentorshipRepository,
        ILogger<WhatsAppWebhookController> logger)
    {
        _messageProcessor = messageProcessor;
        _evolutionAPIService = evolutionAPIService;
        _mentorshipRepository = mentorshipRepository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveMessage([FromBody] WhatsAppWebhookDto webhook)
    {
        try
        {
            _logger.LogInformation("Received webhook: {webhook}", JsonSerializer.Serialize(webhook));

            if (webhook.Event == "messages.upsert" && webhook.Data != null)
            {
                // Ignorar mensagens enviadas por você
                if (webhook.Data.Key?.FromMe == true)
                {
                    _logger.LogInformation("Ignoring message from self");
                    return Ok(new { success = true, message = "Message from self ignored" });
                }

                var remoteJid = webhook.Data.Key?.RemoteJid ?? string.Empty;
                var messageText = webhook.Data.Message?.Conversation ?? string.Empty;

                if (string.IsNullOrEmpty(messageText))
                {
                    _logger.LogWarning("Received empty message from {RemoteJid}", remoteJid);
                    return Ok(new { success = true, message = "Empty message ignored" });
                }

                // Extrair phone number do JID
                var phoneNumber = ExtractPhoneNumber(remoteJid);
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    _logger.LogWarning("Could not extract phone number from {RemoteJid}", remoteJid);
                    return Ok(new { success = true, message = "Invalid phone number format" });
                }

                _logger.LogInformation("Processing message from {PhoneNumber}: {Message}", phoneNumber, messageText);

                // Get mentorship (via query param or first active)
                var mentorshipId = await GetMentorshipIdFromRequestAsync();

                // Process message using MessageProcessor
                var chatResponse = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

                // Send response back via WhatsApp
                var success = await _evolutionAPIService.SendMessageAsync(phoneNumber, chatResponse, mentorshipId);

                if (success)
                {
                    _logger.LogInformation("Successfully processed message from {PhoneNumber}", phoneNumber);
                    return Ok(new { success = true, message = "Message processed successfully" });
                }
                else
                {
                    _logger.LogError("Failed to send response to {PhoneNumber}", phoneNumber);
                    return BadRequest(new { success = false, message = "Failed to send response" });
                }
            }

            _logger.LogInformation("Webhook received but no message to process");
            return Ok(new { success = true, message = "Webhook received" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private string ExtractPhoneNumber(string remoteJid)
    {
        if (string.IsNullOrWhiteSpace(remoteJid))
            return string.Empty;

        // Formato: "5511999999999@s.whatsapp.net" ou "5511999999999:5511999999999@s.whatsapp.net"
        var phonePart = remoteJid.Split('@')[0];
        
        // Se tiver dois pontos, pegar a primeira parte
        if (phonePart.Contains(':'))
        {
            phonePart = phonePart.Split(':')[0];
        }

        // Remover tudo exceto dígitos
        return new string(phonePart.Where(char.IsDigit).ToArray());
    }

    private async Task<Guid> GetMentorshipIdFromRequestAsync()
    {
        // 1. Try to get via query parameter
        var mentorshipIdParam = Request.Query["mentorshipId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(mentorshipIdParam) && Guid.TryParse(mentorshipIdParam, out var mentorshipId))
        {
            var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(mentorshipId);
            if (mentorship != null)
            {
                return mentorshipId;
            }
        }

        // 2. If not provided, get first active mentorship (fallback)
        // TODO: Improve this to search based on configuration or mapping
        // For now, return error if not found
        throw new InvalidOperationException(
            "MentorshipId must be provided via query parameter '?mentorshipId=xxx'. " +
            "Multiple mentorships support coming soon.");
    }
}

