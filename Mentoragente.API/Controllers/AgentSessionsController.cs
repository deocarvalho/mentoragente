using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentSessionsController : ControllerBase
{
    private readonly IAgentSessionService _agentSessionService;
    private readonly ILogger<AgentSessionsController> _logger;

    public AgentSessionsController(
        IAgentSessionService agentSessionService,
        ILogger<AgentSessionsController> logger)
    {
        _agentSessionService = agentSessionService;
        _logger = logger;
    }

    /// <summary>
    /// Get agent session by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AgentSession>> GetAgentSessionById(Guid id)
    {
        try
        {
            var session = await _agentSessionService.GetAgentSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound(new { message = $"Agent session with ID {id} not found" });
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get agent session by user ID and mentoria ID
    /// </summary>
    [HttpGet("user/{userId}/mentoria/{mentoriaId}")]
    public async Task<ActionResult<AgentSession>> GetAgentSession(Guid userId, Guid mentoriaId)
    {
        try
        {
            var session = await _agentSessionService.GetAgentSessionAsync(userId, mentoriaId);
            if (session == null)
            {
                return NotFound(new { message = $"Agent session not found for user {userId} and mentoria {mentoriaId}" });
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent session for user {UserId} and mentoria {MentoriaId}", userId, mentoriaId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get active agent session by user ID and mentoria ID
    /// </summary>
    [HttpGet("user/{userId}/mentoria/{mentoriaId}/active")]
    public async Task<ActionResult<AgentSession>> GetActiveAgentSession(Guid userId, Guid mentoriaId)
    {
        try
        {
            var session = await _agentSessionService.GetActiveAgentSessionAsync(userId, mentoriaId);
            if (session == null)
            {
                return NotFound(new { message = $"Active agent session not found for user {userId} and mentoria {mentoriaId}" });
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active agent session for user {UserId} and mentoria {MentoriaId}", userId, mentoriaId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all agent sessions by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<AgentSession>>> GetAgentSessionsByUserId(Guid userId)
    {
        try
        {
            var sessions = await _agentSessionService.GetAgentSessionsByUserIdAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent sessions for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new agent session
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AgentSession>> CreateAgentSession([FromBody] CreateAgentSessionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var session = await _agentSessionService.CreateAgentSessionAsync(
                request.UserId,
                request.MentoriaId,
                request.AIContextId);

            return CreatedAtAction(nameof(GetAgentSessionById), new { id = session.Id }, session);
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

    /// <summary>
    /// Update agent session
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AgentSession>> UpdateAgentSession(Guid id, [FromBody] UpdateAgentSessionRequest request)
    {
        try
        {
            var session = await _agentSessionService.UpdateAgentSessionAsync(
                id,
                request.Status,
                request.AIContextId,
                request.LastInteraction);

            return Ok(session);
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

    /// <summary>
    /// Expire agent session
    /// </summary>
    [HttpPost("{id}/expire")]
    public async Task<ActionResult> ExpireSession(Guid id)
    {
        try
        {
            var expired = await _agentSessionService.ExpireSessionAsync(id);
            if (!expired)
            {
                return NotFound(new { message = $"Agent session with ID {id} not found" });
            }

            return Ok(new { message = "Session expired successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Pause agent session
    /// </summary>
    [HttpPost("{id}/pause")]
    public async Task<ActionResult> PauseSession(Guid id)
    {
        try
        {
            var paused = await _agentSessionService.PauseSessionAsync(id);
            if (!paused)
            {
                return NotFound(new { message = $"Agent session with ID {id} not found" });
            }

            return Ok(new { message = "Session paused successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Resume agent session
    /// </summary>
    [HttpPost("{id}/resume")]
    public async Task<ActionResult> ResumeSession(Guid id)
    {
        try
        {
            var resumed = await _agentSessionService.ResumeSessionAsync(id);
            if (!resumed)
            {
                return NotFound(new { message = $"Agent session with ID {id} not found" });
            }

            return Ok(new { message = "Session resumed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming agent session {SessionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CreateAgentSessionRequest
{
    public Guid UserId { get; set; }
    public Guid MentoriaId { get; set; }
    public string? AIContextId { get; set; }
}

public class UpdateAgentSessionRequest
{
    public AgentSessionStatus? Status { get; set; }
    public string? AIContextId { get; set; }
    public DateTime? LastInteraction { get; set; }
}

