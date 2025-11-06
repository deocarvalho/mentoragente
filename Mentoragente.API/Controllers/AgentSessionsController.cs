using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.DTOs;
using Mentoragente.Application.Mappings;
using FluentValidation;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Uncomment to require API key authentication
public class AgentSessionsController : ControllerBase
{
    private readonly IAgentSessionService _agentSessionService;
    private readonly ILogger<AgentSessionsController> _logger;
    private readonly IValidator<CreateAgentSessionRequestDto> _createValidator;
    private readonly IValidator<UpdateAgentSessionRequestDto> _updateValidator;

    public AgentSessionsController(
        IAgentSessionService agentSessionService,
        ILogger<AgentSessionsController> logger,
        IValidator<CreateAgentSessionRequestDto> createValidator,
        IValidator<UpdateAgentSessionRequestDto> updateValidator)
    {
        _agentSessionService = agentSessionService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AgentSessionResponseDto>> GetAgentSessionById(Guid id)
    {
        try
        {
            var session = await _agentSessionService.GetAgentSessionByIdAsync(id);
            if (session == null)
                return NotFound(new { message = $"Agent session with ID {id} not found" });
            return Ok(session.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("user/{userId}/mentorship/{mentorshipId}")]
    public async Task<ActionResult<AgentSessionResponseDto>> GetAgentSession(Guid userId, Guid mentorshipId)
    {
        try
        {
            var session = await _agentSessionService.GetAgentSessionAsync(userId, mentorshipId);
            if (session == null)
                return NotFound(new { message = $"Agent session not found" });
            return Ok(session.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent session");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("user/{userId}/mentorship/{mentorshipId}/active")]
    public async Task<ActionResult<AgentSessionResponseDto>> GetActiveAgentSession(Guid userId, Guid mentorshipId)
    {
        try
        {
            var session = await _agentSessionService.GetActiveAgentSessionAsync(userId, mentorshipId);
            if (session == null)
                return NotFound(new { message = $"Active agent session not found" });
            return Ok(session.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active agent session");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<AgentSessionListResponseDto>> GetAgentSessionsByUserId(Guid userId, [FromQuery] PaginationRequestDto pagination)
    {
        try
        {
            var validator = new Mentoragente.Application.Validators.PaginationRequestValidator();
            var validationResult = await validator.ValidateAsync(pagination);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _agentSessionService.GetAgentSessionsByUserIdAsync(userId, pagination.Page, pagination.PageSize);
            
            var response = new AgentSessionListResponseDto
            {
                Sessions = result.Items.Select(s => s.ToDto()).ToList(),
                Total = result.Total,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent sessions for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<AgentSessionResponseDto>> CreateAgentSession([FromBody] CreateAgentSessionRequestDto request)
    {
        try
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var session = await _agentSessionService.CreateAgentSessionAsync(
                request.UserId, request.MentorshipId, request.AIContextId);

            return CreatedAtAction(nameof(GetAgentSessionById), new { id = session.Id }, session.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agent session");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AgentSessionResponseDto>> UpdateAgentSession(Guid id, [FromBody] UpdateAgentSessionRequestDto request)
    {
        try
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var session = await _agentSessionService.UpdateAgentSessionAsync(
                id, EntityToDtoMappings.ParseAgentSessionStatus(request.Status),
                request.AIContextId, request.LastInteraction);

            return Ok(session.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{id}/expire")]
    public async Task<ActionResult> ExpireSession(Guid id)
    {
        try
        {
            var expired = await _agentSessionService.ExpireSessionAsync(id);
            if (!expired)
                return NotFound(new { message = $"Agent session not found" });
            return Ok(new { message = "Session expired successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{id}/pause")]
    public async Task<ActionResult> PauseSession(Guid id)
    {
        try
        {
            var paused = await _agentSessionService.PauseSessionAsync(id);
            if (!paused)
                return NotFound(new { message = $"Agent session not found" });
            return Ok(new { message = "Session paused successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{id}/resume")]
    public async Task<ActionResult> ResumeSession(Guid id)
    {
        try
        {
            var resumed = await _agentSessionService.ResumeSessionAsync(id);
            if (!resumed)
                return NotFound(new { message = $"Agent session not found" });
            return Ok(new { message = "Session resumed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
