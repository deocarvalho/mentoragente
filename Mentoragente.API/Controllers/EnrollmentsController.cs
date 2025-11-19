using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using FluentValidation;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAgentSessionService _agentSessionService;
    private readonly IMessageProcessor _messageProcessor;
    private readonly IMentorshipService _mentorshipService;
    private readonly ILogger<EnrollmentsController> _logger;
    private readonly IValidator<CreateEnrollmentRequestDto> _validator;
    private readonly ILogSanitizer _logSanitizer;

    public EnrollmentsController(
        IUserService userService,
        IAgentSessionService agentSessionService,
        IMessageProcessor messageProcessor,
        IMentorshipService mentorshipService,
        ILogger<EnrollmentsController> logger,
        IValidator<CreateEnrollmentRequestDto> validator,
        ILogSanitizer logSanitizer)
    {
        _userService = userService;
        _agentSessionService = agentSessionService;
        _messageProcessor = messageProcessor;
        _mentorshipService = mentorshipService;
        _logger = logger;
        _validator = validator;
        _logSanitizer = logSanitizer;
    }

    /// <summary>
    /// Create a new enrollment (purchase/enrollment of a mentee in a mentorship program)
    /// This will create the user (if needed), create an agent session, and send a welcome message
    /// 
    /// NOTE: Currently, enrollments are only created manually by the system administrator via API Key authentication.
    /// If in the future we need to support webhooks from payment platforms or multiple mentors,
    /// we should implement:
    /// - Webhook authentication (similar to Z-API Client-Token validation)
    /// - Idempotency (using orderId/transactionId to prevent duplicate enrollments)
    /// - Ownership validation (ensure mentor can only enroll in their own mentorships)
    /// - Payment verification (validate that the purchase actually occurred)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EnrollmentResponseDto>> CreateEnrollment([FromBody] CreateEnrollmentRequestDto request)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors });
        }

        _logger.LogInformation("Creating enrollment for phone {PhoneNumber} in mentorship {MentorshipId}", 
            _logSanitizer.MaskPhoneNumber(request.PhoneNumber), request.MentorshipId);

        // 0. Validate mentorship exists and is active
        var mentorship = await _mentorshipService.GetMentorshipByIdAsync(request.MentorshipId);
        if (mentorship == null)
        {
            _logger.LogWarning("Mentorship {MentorshipId} not found for enrollment", request.MentorshipId);
            return NotFound(new { 
                success = false, 
                message = $"Mentorship with ID {request.MentorshipId} not found" 
            });
        }

        if (mentorship.Status != MentorshipStatus.Active)
        {
            _logger.LogWarning("Mentorship {MentorshipId} is not active (Status: {Status}) for enrollment", 
                request.MentorshipId, mentorship.Status);
            return BadRequest(new { 
                success = false, 
                message = $"Mentorship with ID {request.MentorshipId} is not active. Current status: {mentorship.Status}" 
            });
        }

        // 1. Create or get User
        var user = await _userService.GetUserByPhoneAsync(request.PhoneNumber);
        if (user == null)
        {
            // Create new user (Name is required by validator)
            user = await _userService.CreateUserAsync(
                request.PhoneNumber,
                request.Name,
                request.Email);
            _logger.LogInformation("Created new user {UserId} for enrollment", user.Id);
        }
        else
        {
            // Update user info (Name is required, so always update)
            user = await _userService.UpdateUserAsync(
                user.Id,
                request.Name,
                request.Email);
            _logger.LogInformation("Updated user {UserId} information", user.Id);
        }

        // 2. Create AgentSession
        // Race condition protection: The database has a unique constraint (idx_agent_sessions_unique_active)
        // that prevents duplicate active sessions. If two requests arrive simultaneously, the second will
        // hit the constraint and the repository will automatically retrieve and return the existing session.
        AgentSession? session = null;
        try
        {
            session = await _agentSessionService.CreateAgentSessionAsync(
                user.Id,
                request.MentorshipId);
            _logger.LogInformation("Created agent session {SessionId} for enrollment", session.Id);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Active session already exists"))
        {
            // Session already exists (either from previous enrollment or race condition)
            // Try to retrieve it one more time
            session = await _agentSessionService.GetActiveAgentSessionAsync(user.Id, request.MentorshipId);
            if (session == null)
            {
                _logger.LogWarning("Active session already exists but could not retrieve it");
                return Conflict(new { 
                    success = false,
                    message = "Active session already exists for this user and mentorship" 
                });
            }
            _logger.LogInformation("Using existing agent session {SessionId} for enrollment", session.Id);
        }

        // 3. Send Welcome Message (business logic: don't fail enrollment if welcome message fails)
        var welcomeSent = false;
        try
        {
            welcomeSent = await _messageProcessor.SendWelcomeMessageAsync(
                request.PhoneNumber,
                request.MentorshipId,
                user.Name);
            
            if (welcomeSent)
            {
                _logger.LogInformation("Welcome message sent successfully for enrollment");
            }
            else
            {
                _logger.LogWarning("Failed to send welcome message for enrollment, but enrollment was created");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the enrollment (business requirement)
            _logger.LogError(ex, "Error sending welcome message for enrollment, but enrollment was created");
        }

        var response = new EnrollmentResponseDto
        {
            Success = true,
            SessionId = session!.Id,
            WelcomeMessageSent = welcomeSent,
            Message = welcomeSent 
                ? "Enrollment created successfully and welcome message sent" 
                : "Enrollment created successfully, but welcome message could not be sent"
        };

        return Ok(response);
    }
}

