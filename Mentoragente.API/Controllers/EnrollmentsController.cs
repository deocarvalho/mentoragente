using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using FluentValidation;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAgentSessionService _agentSessionService;
    private readonly IMessageProcessor _messageProcessor;
    private readonly ILogger<EnrollmentsController> _logger;
    private readonly IValidator<CreateEnrollmentRequestDto> _validator;

    public EnrollmentsController(
        IUserService userService,
        IAgentSessionService agentSessionService,
        IMessageProcessor messageProcessor,
        ILogger<EnrollmentsController> logger,
        IValidator<CreateEnrollmentRequestDto> validator)
    {
        _userService = userService;
        _agentSessionService = agentSessionService;
        _messageProcessor = messageProcessor;
        _logger = logger;
        _validator = validator;
    }

    /// <summary>
    /// Create a new enrollment (purchase/enrollment of a mentee in a mentorship program)
    /// This will create the user (if needed), create an agent session, and send a welcome message
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
            request.PhoneNumber, request.MentorshipId);

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

        // 2. Create AgentSession (will throw if already exists - we can handle that)
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
            // Session already exists, get it
            session = await _agentSessionService.GetActiveAgentSessionAsync(user.Id, request.MentorshipId);
            if (session == null)
            {
                _logger.LogWarning("Active session already exists but could not retrieve it");
                return Conflict(new { message = "Active session already exists for this user and mentorship" });
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

