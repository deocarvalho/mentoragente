using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Adapters;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using System.Text.Json;

namespace Mentoragente.API.Controllers;

/// <summary>
/// Webhook controller for Z-API
/// </summary>
[ApiController]
[Route("api/webhooks/zapi")]
public class ZApiWebhookController : ControllerBase
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly IWhatsAppServiceFactory _whatsAppServiceFactory;
    private readonly IZApiWebhookAdapter _adapter;
    private readonly ILogger<ZApiWebhookController> _logger;

    public ZApiWebhookController(
        IMessageProcessor messageProcessor,
        IWhatsAppServiceFactory whatsAppServiceFactory,
        IZApiWebhookAdapter adapter,
        ILogger<ZApiWebhookController> logger)
    {
        _messageProcessor = messageProcessor;
        _whatsAppServiceFactory = whatsAppServiceFactory;
        _adapter = adapter;
        _logger = logger;
    }

    /// <summary>
    /// Receives and processes Z-API webhook messages
    /// </summary>
    /// <param name="webhook">The Z-API webhook payload</param>
    /// <param name="mentorshipId">The mentorship ID to process the message for (required query parameter)</param>
    /// <returns>Success response</returns>
    /// <response code="200">Message processed successfully or ignored</response>
    /// <response code="400">Invalid request or failed to send response</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveMessage(
        [FromBody] ZApiWebhookDto webhook,
        [FromQuery] Guid mentorshipId)
    {
        // Log the complete webhook payload for debugging
        var payloadJson = JsonSerializer.Serialize(webhook, new JsonSerializerOptions { WriteIndented = false });
        _logger.LogInformation("Received Z-API webhook payload for Mentorship {MentorshipId}: {Payload}", 
            mentorshipId, payloadJson);

        if (mentorshipId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "MentorshipId query parameter is required" });
        }

        // Adapt Z-API-specific DTO to generic model
        var genericMessage = _adapter.Adapt(webhook);
        if (genericMessage == null)
        {
            _logger.LogWarning("Z-API webhook message was ignored. FromMe={FromMe}, Type={Type}, Phone={Phone}, HasText={HasText}, Text={Text}", 
                webhook.FromMe, webhook.Type, webhook.Phone, webhook.Text?.Message != null, webhook.Text?.Message ?? "null");
            return Ok(new { success = true, message = "Message ignored" });
        }

        _logger.LogInformation("Processing Z-API message from {PhoneNumber}: {Message} for Mentorship {MentorshipId}", 
            genericMessage.PhoneNumber, genericMessage.MessageText, mentorshipId);

        // Process message (agnostic to provider)
        var result = await _messageProcessor.ProcessMessageAsync(
            genericMessage.PhoneNumber, 
            genericMessage.MessageText, 
            mentorshipId);

        // Get the correct service based on mentorship configuration
        var whatsAppService = _whatsAppServiceFactory.GetServiceForMentorship(result.Mentorship);
        var sent = await whatsAppService.SendMessageAsync(
            genericMessage.PhoneNumber, 
            result.Response, 
            result.Mentorship);

        if (!sent)
        {
            _logger.LogError("Failed to send response to {PhoneNumber}", genericMessage.PhoneNumber);
            return BadRequest(new { success = false, message = "Failed to send response" });
        }

        return Ok(new { success = true, message = "Message processed successfully" });
    }
}

