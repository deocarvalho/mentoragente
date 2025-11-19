using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mentoragente.Application.Adapters;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Mentoragente.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZApiWebhookController> _logger;
    private readonly ILogSanitizer _logSanitizer;
    private readonly IWebHostEnvironment _environment;
    private readonly IServiceScopeFactory _serviceScopeFactory;
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
        IMentorshipRepository mentorshipRepository,
        IConfiguration configuration,
        ILogger<ZApiWebhookController> logger,
        ILogSanitizer logSanitizer,
        IWebHostEnvironment environment,
        IServiceScopeFactory serviceScopeFactory)
    {
        _messageProcessor = messageProcessor;
        _whatsAppServiceFactory = whatsAppServiceFactory;
        _adapter = adapter;
        _userOrchestrationService = userOrchestrationService;
        _agentSessionService = agentSessionService;
        _mentorshipCacheService = mentorshipCacheService;
        _mentorshipRepository = mentorshipRepository;
        _configuration = configuration;
        _logger = logger;
        _logSanitizer = logSanitizer;
        _environment = environment;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// Receives and processes Z-API webhook messages
    /// </summary>
    /// <param name="webhook">The Z-API webhook payload</param>
    /// <param name="mentorshipId">The mentorship ID to process the message for (optional - will auto-detect if not provided)</param>
    /// <returns>Success response</returns>
    /// <response code="200">Message processed successfully or ignored</response>
    /// <response code="400">Invalid request, user not enrolled, or failed to send response</response>
    /// <response code="401">Invalid or missing Z-Api-Token header</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Receives Z-API webhook messages",
        Description = "This endpoint receives webhook messages from Z-API. Z-API automatically sends Z-Api-Token header for authentication. Click the 'Authorize' button in Swagger UI to add your Z-Api-Token for testing.",
        OperationId = "ReceiveZApiWebhook"
    )]
    [SwaggerResponse(200, "Message processed successfully or ignored")]
    [SwaggerResponse(400, "Invalid request, user not enrolled, or failed to send response")]
    [SwaggerResponse(401, "Invalid or missing Z-Api-Token header")]
    public async Task<IActionResult> ReceiveMessage(
        [FromBody] ZApiWebhookDto webhook,
        [FromQuery] Guid? mentorshipId = null)
    {
        // Log complete request details in Development environment only
        if (_environment.IsDevelopment())
        {
            LogCompleteRequest(webhook);
        }

        // Validate Z-Api-Token header (Z-API sends this header in webhooks)
        // This token corresponds to instance_token in the mentorships table
        if (!Request.Headers.TryGetValue("Z-Api-Token", out var zApiTokenHeader) || 
            string.IsNullOrWhiteSpace(zApiTokenHeader))
        {
            _logger.LogWarning("Z-API webhook rejected: Missing Z-Api-Token header");
            return Unauthorized(new { success = false, message = "Missing or invalid Z-Api-Token header" });
        }

        var instanceToken = zApiTokenHeader.ToString();
        
        // Find mentorship by instance_token (Z-Api-Token from header)
        var mentorshipByToken = await _mentorshipRepository.GetMentorshipByInstanceTokenAsync(instanceToken);
        
        if (mentorshipByToken == null)
        {
            _logger.LogWarning("Z-API webhook rejected: No active mentorship found for instance token. Received: {ReceivedToken}", 
                _logSanitizer.MaskToken(instanceToken));
            return Unauthorized(new { success = false, message = "Invalid Z-Api-Token: No active mentorship found for this instance token" });
        }

        // If mentorshipId is provided in query, validate it matches the token's mentorship
        if (mentorshipId.HasValue && mentorshipId.Value != mentorshipByToken.Id)
        {
            _logger.LogWarning("Z-API webhook rejected: Provided mentorshipId {ProvidedId} does not match the mentorship for instance token {Token}", 
                mentorshipId.Value, _logSanitizer.MaskToken(instanceToken));
            return BadRequest(new { success = false, message = "MentorshipId does not match the instance token" });
        }

        // Use the mentorship found by token (or override with query param if provided and matches)
        mentorshipId = mentorshipByToken.Id;
        _logger.LogInformation("Z-API webhook authenticated. Mentorship: {MentorshipId}, Instance Token: {Token}", 
            mentorshipId, _logSanitizer.MaskToken(instanceToken));

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

        // mentorshipId is now guaranteed to be set from the Z-Api-Token validation above

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

            // Capture variables needed for background task (before request scope is disposed)
            var phoneNumber = genericMessage.PhoneNumber;
            var response = result.Response;
            var mentorshipForSend = new Mentorship
            {
                Id = result.Mentorship.Id,
                InstanceCode = result.Mentorship.InstanceCode,
                InstanceToken = result.Mentorship.InstanceToken,
                WhatsAppProvider = result.Mentorship.WhatsAppProvider
            };

            // Send response in parallel (fire-and-forget) to avoid blocking webhook response
            // This improves perceived latency - webhook returns immediately while message is sent in background
            // IMPORTANT: Create a new service scope for background task to avoid ObjectDisposedException
            _ = Task.Run(async () =>
            {
                // Create a new service scope for the background task
                using var scope = _serviceScopeFactory.CreateScope();
                try
                {
                    // Get services from the new scope
                    var whatsAppServiceFactory = scope.ServiceProvider.GetRequiredService<IWhatsAppServiceFactory>();
                    var whatsAppService = whatsAppServiceFactory.GetServiceForMentorship(mentorshipForSend);
                    
                    var sent = await whatsAppService.SendMessageAsync(
                        phoneNumber, 
                        response, 
                        mentorshipForSend);

                    if (!sent)
                    {
                        _logger.LogError("Failed to send response to {PhoneNumber} in background", phoneNumber);
                    }
                    else
                    {
                        _logger.LogDebug("Response sent successfully to {PhoneNumber} in background", phoneNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending response to {PhoneNumber} in background", phoneNumber);
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
                    // Capture variables needed for background task
                    var phoneNumber = genericMessage.PhoneNumber;
                    var errorMessage = _configuration["Messages:UnauthorizedAccess"] ?? DefaultUnauthorizedMessage;
                    var mentorshipForError = new Mentorship
                    {
                        Id = mentorship.Id,
                        InstanceCode = mentorship.InstanceCode,
                        InstanceToken = mentorship.InstanceToken,
                        WhatsAppProvider = mentorship.WhatsAppProvider
                    };

                    // Create a new service scope for the background task
                    _ = Task.Run(async () =>
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        try
                        {
                            var whatsAppServiceFactory = scope.ServiceProvider.GetRequiredService<IWhatsAppServiceFactory>();
                            var whatsAppService = whatsAppServiceFactory.GetServiceForMentorship(mentorshipForError);
                            await whatsAppService.SendMessageAsync(
                                phoneNumber, 
                                errorMessage, 
                                mentorshipForError);
                        }
                        catch { /* Ignore errors when sending error message */ }
                    });
                }
            }
            catch { /* Ignore errors */ }
            
            return BadRequest(new { success = false, message = "Error processing message" });
        }
    }

    /// <summary>
    /// Logs complete request details (headers, body, query params) - Development only
    /// </summary>
    private void LogCompleteRequest(ZApiWebhookDto? webhook = null)
    {
        try
        {
            // Serialize body from the already-deserialized object (since model binding already consumed the stream)
            string bodyJson = webhook != null 
                ? JsonSerializer.Serialize(webhook, new JsonSerializerOptions { WriteIndented = true })
                : "[Body not available - model binding may have failed]";

            var requestDetails = new
            {
                Method = Request.Method,
                Path = Request.Path.Value,
                QueryString = Request.QueryString.Value,
                Headers = Request.Headers.ToDictionary(
                    h => h.Key,
                    h => h.Value.Count == 1 ? (object)h.Value.ToString() : h.Value.ToArray()
                ),
                ContentType = Request.ContentType,
                ContentLength = Request.ContentLength,
                Scheme = Request.Scheme,
                Host = Request.Host.Value,
                Protocol = Request.Protocol,
                RemoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                Body = bodyJson
            };

            var requestJson = JsonSerializer.Serialize(requestDetails, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            _logger.LogInformation("🔍 [DEVELOPMENT ONLY] Complete Webhook Request:\n{RequestDetails}", requestJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log complete request details");
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

