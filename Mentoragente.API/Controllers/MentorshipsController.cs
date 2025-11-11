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
        var mentorship = await _mentorshipService.GetMentorshipByIdAsync(id);
        if (mentorship == null)
            return NotFound(new { message = $"Mentorship with ID {id} not found" });
        return Ok(mentorship.ToDto());
    }

    [HttpGet("mentor/{mentorId}")]
    public async Task<ActionResult<MentorshipListResponseDto>> GetMentorshipsByMentorId(Guid mentorId, [FromQuery] PaginationRequestDto pagination)
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

    [HttpGet("active")]
    public async Task<ActionResult<MentorshipListResponseDto>> GetActiveMentorships([FromQuery] PaginationRequestDto pagination)
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

    [HttpPost]
    public async Task<ActionResult<MentorshipResponseDto>> CreateMentorship([FromBody] CreateMentorshipRequestDto request)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var whatsAppProvider = !string.IsNullOrEmpty(request.WhatsAppProvider)
            ? EntityToDtoMappings.ParseWhatsAppProvider(request.WhatsAppProvider)
            : null;

        var mentorship = await _mentorshipService.CreateMentorshipAsync(
            request.MentorId, 
            request.Name, 
            request.AssistantId, 
            request.DurationDays, 
            request.Description,
            whatsAppProvider,
            request.InstanceCode);

        return CreatedAtAction(nameof(GetMentorshipById), new { id = mentorship.Id }, mentorship.ToDto());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MentorshipResponseDto>> UpdateMentorship(Guid id, [FromBody] UpdateMentorshipRequestDto request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var whatsAppProvider = !string.IsNullOrEmpty(request.WhatsAppProvider)
            ? EntityToDtoMappings.ParseWhatsAppProvider(request.WhatsAppProvider)
            : null;

        var mentorship = await _mentorshipService.UpdateMentorshipAsync(
            id, 
            request.Name, 
            request.AssistantId, 
            request.DurationDays, 
            request.Description,
            EntityToDtoMappings.ParseMentorshipStatus(request.Status),
            whatsAppProvider,
            request.InstanceCode);

        return Ok(mentorship.ToDto());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMentorship(Guid id)
    {
        var deleted = await _mentorshipService.DeleteMentorshipAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Mentorship with ID {id} not found" });
        return NoContent();
    }
}

