using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.DTOs;
using Mentoragente.Application.Mappings;
using FluentValidation;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Uncomment to require API key authentication
public class MentorshipsController : ControllerBase
{
    private readonly IMentorshipService _mentorshipService;
    private readonly ILogger<MentorshipsController> _logger;
    private readonly IValidator<CreateMentorshipRequestDto> _createValidator;
    private readonly IValidator<UpdateMentorshipRequestDto> _updateValidator;

    public MentorshipsController(
        IMentorshipService mentorshipService,
        ILogger<MentorshipsController> logger,
        IValidator<CreateMentorshipRequestDto> createValidator,
        IValidator<UpdateMentorshipRequestDto> updateValidator)
    {
        _mentorshipService = mentorshipService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MentorshipResponseDto>> GetMentorshipById(Guid id)
    {
        try
        {
            var mentorship = await _mentorshipService.GetMentorshipByIdAsync(id);
            if (mentorship == null)
                return NotFound(new { message = $"Mentorship with ID {id} not found" });
            return Ok(mentorship.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentorship {MentorshipId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("mentor/{mentorId}")]
    public async Task<ActionResult<MentorshipListResponseDto>> GetMentorshipsByMentorId(Guid mentorId, [FromQuery] PaginationRequestDto pagination)
    {
        try
        {
            var validator = new Mentoragente.Application.Validators.PaginationRequestValidator();
            var validationResult = await validator.ValidateAsync(pagination);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _mentorshipService.GetMentorshipsByMentorIdAsync(mentorId, pagination.Page, pagination.PageSize);
            
            var response = new MentorshipListResponseDto
            {
                Mentorships = result.Items.Select(m => m.ToDto()).ToList(),
                Total = result.Total,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentorships for mentor {MentorId}", mentorId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<MentorshipListResponseDto>> GetActiveMentorships([FromQuery] PaginationRequestDto pagination)
    {
        try
        {
            var validator = new Mentoragente.Application.Validators.PaginationRequestValidator();
            var validationResult = await validator.ValidateAsync(pagination);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _mentorshipService.GetActiveMentorshipsAsync(pagination.Page, pagination.PageSize);
            
            var response = new MentorshipListResponseDto
            {
                Mentorships = result.Items.Select(m => m.ToDto()).ToList(),
                Total = result.Total,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active mentorships");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<MentorshipResponseDto>> CreateMentorship([FromBody] CreateMentorshipRequestDto request)
    {
        try
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var mentorship = await _mentorshipService.CreateMentorshipAsync(
                request.MentorId, 
                request.Name, 
                request.AssistantId, 
                request.DurationDays, 
                request.Description,
                request.EvolutionApiKey,
                request.EvolutionInstanceName);

            return CreatedAtAction(nameof(GetMentorshipById), new { id = mentorship.Id }, mentorship.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mentorship");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MentorshipResponseDto>> UpdateMentorship(Guid id, [FromBody] UpdateMentorshipRequestDto request)
    {
        try
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var mentorship = await _mentorshipService.UpdateMentorshipAsync(
                id, 
                request.Name, 
                request.AssistantId, 
                request.DurationDays, 
                request.Description,
                EntityToDtoMappings.ParseMentorshipStatus(request.Status),
                request.EvolutionApiKey,
                request.EvolutionInstanceName);

            return Ok(mentorship.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating mentorship {MentorshipId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMentorship(Guid id)
    {
        try
        {
            var deleted = await _mentorshipService.DeleteMentorshipAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Mentorship with ID {id} not found" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mentorship {MentorshipId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

