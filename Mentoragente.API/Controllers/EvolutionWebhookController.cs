using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Adapters;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;

namespace Mentoragente.API.Controllers;

/// <summary>
/// Webhook controller for Evolution API
/// </summary>
[ApiController]
[Route("api/webhooks/evolution")]
public class EvolutionWebhookController : ControllerBase
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly IWhatsAppServiceFactory _whatsAppServiceFactory;
    private readonly WhatsAppWebhookAdapterFactory _adapterFactory;
    private readonly ILogger<EvolutionWebhookController> _logger;

    public EvolutionWebhookController(
        IMessageProcessor messageProcessor,
        IWhatsAppServiceFactory whatsAppServiceFactory,
        WhatsAppWebhookAdapterFactory adapterFactory,
        ILogger<EvolutionWebhookController> logger)
    {
        _messageProcessor = messageProcessor;
        _whatsAppServiceFactory = whatsAppServiceFactory;
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    /// <summary>
    /// Receives and processes Evolution API webhook messages
    /// </summary>
    /// <param name="webhook">The Evolution API webhook payload</param>
    /// <param name="mentorshipId">The mentorship ID to process the message for (required query parameter)</param>
    /// <returns>Success response</returns>
    /// <response code="200">Message processed successfully or ignored</response>
    /// <response code="400">Invalid request or failed to send response</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveMessage(
        [FromBody] EvolutionWebhookDto webhook,
        [FromQuery] Guid mentorshipId)
    {
        if (mentorshipId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "MentorshipId query parameter is required" });
        }

        // Adapt Evolution-specific DTO to generic model
        var adapter = _adapterFactory.GetAdapter(webhook);
        if (adapter == null)
        {
            _logger.LogWarning("No adapter found for Evolution webhook");
            return BadRequest(new { success = false, message = "Invalid webhook format" });
        }

        var genericMessage = adapter.Adapt(webhook);
        if (genericMessage == null)
        {
            return Ok(new { success = true, message = "Message ignored" });
        }

        _logger.LogInformation("Processing Evolution API message from {PhoneNumber}: {Message} for Mentorship {MentorshipId}", 
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

