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
        var session = await _agentSessionService.GetAgentSessionByIdAsync(id);
        if (session == null)
            return NotFound(new { message = $"Agent session with ID {id} not found" });
        return Ok(session.ToDto());
    }

    [HttpGet("user/{userId}/mentorship/{mentorshipId}")]
    public async Task<ActionResult<AgentSessionResponseDto>> GetAgentSession(Guid userId, Guid mentorshipId)
    {
        var session = await _agentSessionService.GetAgentSessionAsync(userId, mentorshipId);
        if (session == null)
            return NotFound(new { message = $"Agent session not found" });
        return Ok(session.ToDto());
    }

    [HttpGet("user/{userId}/mentorship/{mentorshipId}/active")]
    public async Task<ActionResult<AgentSessionResponseDto>> GetActiveAgentSession(Guid userId, Guid mentorshipId)
    {
        var session = await _agentSessionService.GetActiveAgentSessionAsync(userId, mentorshipId);
        if (session == null)
            return NotFound(new { message = $"Active agent session not found" });
        return Ok(session.ToDto());
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<AgentSessionListResponseDto>> GetAgentSessionsByUserId(Guid userId, [FromQuery] PaginationRequestDto pagination)
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

    [HttpPost]
    public async Task<ActionResult<AgentSessionResponseDto>> CreateAgentSession([FromBody] CreateAgentSessionRequestDto request)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var session = await _agentSessionService.CreateAgentSessionAsync(
            request.UserId, request.MentorshipId, request.AIContextId);

        return CreatedAtAction(nameof(GetAgentSessionById), new { id = session.Id }, session.ToDto());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AgentSessionResponseDto>> UpdateAgentSession(Guid id, [FromBody] UpdateAgentSessionRequestDto request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var session = await _agentSessionService.UpdateAgentSessionAsync(
            id, EntityToDtoMappings.ParseAgentSessionStatus(request.Status),
            request.AIContextId, request.LastInteraction);

        return Ok(session.ToDto());
    }

    [HttpPost("{id}/expire")]
    public async Task<ActionResult> ExpireSession(Guid id)
    {
        var expired = await _agentSessionService.ExpireSessionAsync(id);
        if (!expired)
            return NotFound(new { message = $"Agent session not found" });
        return Ok(new { message = "Session expired successfully" });
    }

    [HttpPost("{id}/pause")]
    public async Task<ActionResult> PauseSession(Guid id)
    {
        var paused = await _agentSessionService.PauseSessionAsync(id);
        if (!paused)
            return NotFound(new { message = $"Agent session not found" });
        return Ok(new { message = "Session paused successfully" });
    }

    [HttpPost("{id}/resume")]
    public async Task<ActionResult> ResumeSession(Guid id)
    {
        var resumed = await _agentSessionService.ResumeSessionAsync(id);
        if (!resumed)
            return NotFound(new { message = $"Agent session not found" });
        return Ok(new { message = "Session resumed successfully" });
    }
}
