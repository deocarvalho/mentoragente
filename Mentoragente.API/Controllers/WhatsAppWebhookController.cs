using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly IEvolutionAPIService _evolutionAPIService;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IMessageProcessor messageProcessor,
        IEvolutionAPIService evolutionAPIService,
        ILogger<WhatsAppWebhookController> logger)
    {
        _messageProcessor = messageProcessor;
        _evolutionAPIService = evolutionAPIService;
        _logger = logger;
    }

    /// <summary>
    /// Receives and processes WhatsApp webhook messages
    /// </summary>
    /// <param name="webhook">The WhatsApp webhook payload</param>
    /// <param name="mentorshipId">The mentorship ID to process the message for (required query parameter)</param>
    /// <returns>Success response</returns>
    /// <response code="200">Message processed successfully</response>
    /// <response code="400">Failed to send response or invalid request</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveMessage(
        [FromBody] WhatsAppWebhookDto webhook,
        [FromQuery] Guid mentorshipId)
    {
        if (mentorshipId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "MentorshipId query parameter is required" });
        }

        if (!TryExtractMessage(webhook, out var phoneNumber, out var messageText))
        {
            return Ok(new { success = true, message = "Message ignored" });
        }

        _logger.LogInformation("Processing message from {PhoneNumber}: {Message} for Mentorship {MentorshipId}", phoneNumber, messageText, mentorshipId);

        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);
        var sent = await _evolutionAPIService.SendMessageAsync(phoneNumber, result.Response, result.Mentorship);

        if (!sent)
        {
            _logger.LogError("Failed to send response to {PhoneNumber}", phoneNumber);
            return BadRequest(new { success = false, message = "Failed to send response" });
        }

        return Ok(new { success = true, message = "Message processed successfully" });
    }

    private bool TryExtractMessage(WhatsAppWebhookDto webhook, out string phoneNumber, out string messageText)
    {
        phoneNumber = string.Empty;
        messageText = string.Empty;

        if (!IsValidWebhook(webhook))
            return false;

        if (webhook.Data!.Key?.FromMe == true)
        {
            _logger.LogInformation("Ignoring message from self");
            return false;
        }

        messageText = webhook.Data.Message?.Conversation ?? string.Empty;
        if (string.IsNullOrEmpty(messageText))
        {
            _logger.LogWarning("Received empty message from {RemoteJid}", webhook.Data.Key?.RemoteJid);
            return false;
        }

        phoneNumber = ExtractPhoneNumber(webhook.Data.Key?.RemoteJid ?? string.Empty);
        if (string.IsNullOrEmpty(phoneNumber))
        {
            _logger.LogWarning("Could not extract phone number from {RemoteJid}", webhook.Data.Key?.RemoteJid);
            return false;
        }

        return true;
    }

    private static bool IsValidWebhook(WhatsAppWebhookDto webhook) =>
        webhook.Event == "messages.upsert" && webhook.Data != null;

    private static string ExtractPhoneNumber(string remoteJid)
    {
        if (string.IsNullOrWhiteSpace(remoteJid))
            return string.Empty;

        var phonePart = remoteJid.Split('@')[0];
        if (phonePart.Contains(':'))
            phonePart = phonePart.Split(':')[0];

        return new string(phonePart.Where(char.IsDigit).ToArray());
    }

}

