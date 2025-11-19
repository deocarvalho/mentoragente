using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mentoragente.Application.Adapters;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace Mentoragente.API.Controllers;

/// <summary>
/// Webhook controller for Z-API
/// </summary>
[ApiController]
[Route("api/webhooks/zapi")]
[EnableRateLimiting("WebhookPolicy")]
public class ZApiWebhookController : ControllerBase
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly IWhatsAppServiceFactory _whatsAppServiceFactory;
    private readonly IZApiWebhookAdapter _adapter;
    private readonly IUserOrchestrationService _userOrchestrationService;
    private readonly IAgentSessionService _agentSessionService;
    private readonly IMentorshipCacheService _mentorshipCacheService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZApiWebhookController> _logger;
    private readonly ILogSanitizer _logSanitizer;
    private const string DefaultUnauthorizedMessage = "Sinto muito, mas este número de telefone não tem acesso a este contato. Se você acredita que houve um engano, por favor, entre em contato com a pessoa gestora do seu contrato.";
    
    // In-memory cache for deduplication (MessageId -> timestamp)
    // Messages older than 5 minutes are automatically removed
    private static readonly ConcurrentDictionary<string, DateTime> _processedMessageIds = new();

    public ZApiWebhookController(
        IMessageProcessor messageProcessor,
        IWhatsAppServiceFactory whatsAppServiceFactory,
        IZApiWebhookAdapter adapter,
        IUserOrchestrationService userOrchestrationService,
        IAgentSessionService agentSessionService,
        IMentorshipCacheService mentorshipCacheService,
        IConfiguration configuration,
        ILogger<ZApiWebhookController> logger,
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
    /// Receives and processes Z-API webhook messages
    /// </summary>
    /// <param name="webhook">The Z-API webhook payload</param>
    /// <param name="mentorshipId">The mentorship ID to process the message for (optional - will auto-detect if not provided)</param>
    /// <returns>Success response</returns>
    /// <response code="200">Message processed successfully or ignored</response>
    /// <response code="400">Invalid request, user not enrolled, or failed to send response</response>
    /// <response code="401">Invalid or missing Client-Token header</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Receives Z-API webhook messages",
        Description = "This endpoint receives webhook messages from Z-API. Requires Client-Token header for authentication. Click the 'Authorize' button in Swagger UI to add your Client-Token.",
        OperationId = "ReceiveZApiWebhook"
    )]
    [SwaggerResponse(200, "Message processed successfully or ignored")]
    [SwaggerResponse(400, "Invalid request, user not enrolled, or failed to send response")]
    [SwaggerResponse(401, "Invalid or missing Client-Token header")]
    public async Task<IActionResult> ReceiveMessage(
        [FromBody] ZApiWebhookDto webhook,
        [FromQuery] Guid? mentorshipId = null)
    {
        // Validate Client-Token header
        if (!Request.Headers.TryGetValue("Client-Token", out var clientTokenHeader) || 
            string.IsNullOrWhiteSpace(clientTokenHeader))
        {
            _logger.LogWarning("Z-API webhook rejected: Missing Client-Token header");
            return Unauthorized(new { success = false, message = "Missing or invalid Client-Token header" });
        }

        var expectedClientToken = _configuration["ZApi:Client-Token"];
        if (string.IsNullOrWhiteSpace(expectedClientToken))
        {
            _logger.LogError("Z-API Client-Token not configured in appsettings.json");
            return StatusCode(500, new { success = false, message = "Server configuration error" });
        }

        if (clientTokenHeader.ToString() != expectedClientToken)
        {
            _logger.LogWarning("Z-API webhook rejected: Invalid Client-Token. Received: {ReceivedToken}", 
                _logSanitizer.MaskToken(clientTokenHeader.ToString()));
            return Unauthorized(new { success = false, message = "Invalid Client-Token" });
        }

        // Log the sanitized webhook payload for debugging (sensitive data masked)
        var payloadJson = JsonSerializer.Serialize(webhook, new JsonSerializerOptions { WriteIndented = false });
        var sanitizedPayload = _logSanitizer.SanitizeJsonPayload(payloadJson);
        _logger.LogInformation("Received Z-API webhook payload: {Payload}", sanitizedPayload);

        // Adapt Z-API-specific DTO to generic model
        var genericMessage = _adapter.Adapt(webhook);
        if (genericMessage == null)
        {
            _logger.LogWarning("Z-API webhook message was ignored. FromMe={FromMe}, Type={Type}, Phone={Phone}, HasText={HasText}, Text={Text}",
                webhook.FromMe,
                webhook.Type ?? "unknown",
                _logSanitizer.MaskPhoneNumber(webhook.Phone),
                webhook.Text?.Message != null,
                _logSanitizer.SanitizeMessageText(webhook.Text?.Message));
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

        // Deduplication: Check if we've already processed this message
        if (!string.IsNullOrEmpty(webhook.MessageId))
        {
            CleanupOldMessageIds();
            
            if (_processedMessageIds.ContainsKey(webhook.MessageId))
            {
                _logger.LogWarning("Duplicate webhook message detected and ignored. MessageId: {MessageId}", webhook.MessageId);
                return Ok(new { success = true, message = "Duplicate message ignored" });
            }
            
            _processedMessageIds.TryAdd(webhook.MessageId, DateTime.UtcNow);
        }

        _logger.LogInformation("Processing Z-API message from {PhoneNumber}: {Message} for Mentorship {MentorshipId}", 
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

            // Send response in parallel (fire-and-forget) to avoid blocking webhook response
            // This improves perceived latency - webhook returns immediately while message is sent in background
            _ = Task.Run(async () =>
            {
                try
                {
        var whatsAppService = _whatsAppServiceFactory.GetServiceForMentorship(result.Mentorship);
        var sent = await whatsAppService.SendMessageAsync(
            genericMessage.PhoneNumber, 
            result.Response, 
            result.Mentorship);

        if (!sent)
        {
                        _logger.LogError("Failed to send response to {PhoneNumber} in background", genericMessage.PhoneNumber);
                    }
                    else
                    {
                        _logger.LogDebug("Response sent successfully to {PhoneNumber} in background", genericMessage.PhoneNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending response to {PhoneNumber} in background", genericMessage.PhoneNumber);
                }
            });

            // Return immediately - message sending happens in background
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
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var whatsAppService = _whatsAppServiceFactory.GetServiceForMentorship(mentorship);
                            await whatsAppService.SendMessageAsync(
                                genericMessage.PhoneNumber, 
                                _configuration["Messages:UnauthorizedAccess"] ?? DefaultUnauthorizedMessage, 
                                mentorship);
                        }
                        catch { /* Ignore errors when sending error message */ }
                    });
                }
            }
            catch { /* Ignore errors */ }
            
            return BadRequest(new { success = false, message = "Error processing message" });
        }
    }

    private static void CleanupOldMessageIds()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var keysToRemove = _processedMessageIds
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _processedMessageIds.TryRemove(key, out _);
        }
    }
}

