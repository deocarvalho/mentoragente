using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mentoragente.Application.Adapters;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace Mentoragente.API.Controllers;

/// <summary>
/// Webhook controller for Evolution API
/// </summary>
[ApiController]
[Route("api/webhooks/evolution")]
[EnableRateLimiting("WebhookPolicy")]
public class EvolutionWebhookController : ControllerBase
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly IWhatsAppServiceFactory _whatsAppServiceFactory;
    private readonly IEvolutionWebhookAdapter _adapter;
    private readonly IUserOrchestrationService _userOrchestrationService;
    private readonly IAgentSessionService _agentSessionService;
    private readonly IMentorshipCacheService _mentorshipCacheService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EvolutionWebhookController> _logger;
    private readonly ILogSanitizer _logSanitizer;
    private const string DefaultUnauthorizedMessage = "Sinto muito, mas este número de telefone não tem acesso a este contato. Se você acredita que houve um engano, por favor, entre em contato com a pessoa gestora do seu contrato.";

    public EvolutionWebhookController(
        IMessageProcessor messageProcessor,
        IWhatsAppServiceFactory whatsAppServiceFactory,
        IEvolutionWebhookAdapter adapter,
        IUserOrchestrationService userOrchestrationService,
        IAgentSessionService agentSessionService,
        IMentorshipCacheService mentorshipCacheService,
        IConfiguration configuration,
        ILogger<EvolutionWebhookController> logger,
        ILogSanitizer logSanitizer)
    {
        _messageProcessor = messageProcessor;
        _whatsAppServiceFactory = whatsAppServiceFactory;
        _adapter = adapter;
        _userOrchestrationService = userOrchestrationService;
        _agentSessionService = agentSessionService;
        _mentorshipCacheService = mentorshipCacheService;
        _configuration = configuration;
        _logger = logger;
        _logSanitizer = logSanitizer;
    }

    /// <summary>
    /// Receives and processes Evolution API webhook messages
    /// </summary>
    /// <param name="webhook">The Evolution API webhook payload</param>
    /// <param name="mentorshipId">The mentorship ID to process the message for (optional - will auto-detect if not provided)</param>
    /// <returns>Success response</returns>
    /// <response code="200">Message processed successfully or ignored</response>
    /// <response code="400">Invalid request, user not enrolled, or failed to send response</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveMessage(
        [FromBody] EvolutionWebhookDto webhook,
        [FromQuery] Guid? mentorshipId = null)
    {
        // Adapt Evolution-specific DTO to generic model
        var genericMessage = _adapter.Adapt(webhook);
        if (genericMessage == null)
        {
            return Ok(new { success = true, message = "Message ignored" });
        }

        // Auto-detect mentorship if not provided
        if (!mentorshipId.HasValue || mentorshipId.Value == Guid.Empty)
        {
            _logger.LogInformation("MentorshipId not provided, auto-detecting for phone {PhoneNumber}", 
                _logSanitizer.MaskPhoneNumber(genericMessage.PhoneNumber));
            
            var user = await _userOrchestrationService.GetUserAsync(genericMessage.PhoneNumber);
            if (user == null)
            {
                _logger.LogWarning("User not found for phone {PhoneNumber} - user not enrolled", 
                    _logSanitizer.MaskPhoneNumber(genericMessage.PhoneNumber));
                return BadRequest(new { 
                    success = false, 
                    message = "You are not enrolled in any active mentorship. Please enroll first before sending messages." 
                });
            }
            
            var detectedMentorshipId = await _agentSessionService.AutoDetectMentorshipIdAsync(user.Id);
            
            if (!detectedMentorshipId.HasValue)
            {
                _logger.LogWarning("No active session found for user {UserId} (phone {PhoneNumber}) - user may not be enrolled in any mentorship", 
                    user.Id, _logSanitizer.MaskPhoneNumber(genericMessage.PhoneNumber));
                return BadRequest(new { 
                    success = false, 
                    message = "You are not enrolled in any active mentorship. Please enroll first before sending messages." 
                });
            }
            
            mentorshipId = detectedMentorshipId.Value;
            _logger.LogInformation("Auto-detected mentorship {MentorshipId} for phone {PhoneNumber}", 
                mentorshipId, _logSanitizer.MaskPhoneNumber(genericMessage.PhoneNumber));
        }

        _logger.LogInformation("Processing Evolution API message from {PhoneNumber}: {Message} for Mentorship {MentorshipId}", 
            _logSanitizer.MaskPhoneNumber(genericMessage.PhoneNumber),
            _logSanitizer.SanitizeMessageText(genericMessage.MessageText),
            mentorshipId);

        try
        {
        // Process message (agnostic to provider)
        var result = await _messageProcessor.ProcessMessageAsync(
            genericMessage.PhoneNumber, 
            genericMessage.MessageText, 
                mentorshipId.Value);

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
        catch (InvalidOperationException ex) when (ex.Message.Contains("Mentorship") && ex.Message.Contains("not found"))
        {
            _logger.LogError(ex, "Mentorship not found for message from {PhoneNumber}", genericMessage.PhoneNumber);
            return BadRequest(new { success = false, message = "Mentorship not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message from {PhoneNumber}", genericMessage.PhoneNumber);
            // Try to send error message to user if possible
            try
            {
                var mentorship = await _mentorshipCacheService.GetMentorshipAsync(mentorshipId.Value);
                if (mentorship != null)
                {
                    var whatsAppService = _whatsAppServiceFactory.GetServiceForMentorship(mentorship);
                    await whatsAppService.SendMessageAsync(
                        genericMessage.PhoneNumber, 
                        _configuration["Messages:UnauthorizedAccess"] ?? DefaultUnauthorizedMessage, 
                        mentorship);
                }
            }
            catch { /* Ignore errors when sending error message */ }
            
            return BadRequest(new { success = false, message = "Error processing message" });
        }
    }
}

