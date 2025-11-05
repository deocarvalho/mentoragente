using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.DTOs;
using Mentoragente.Application.Mappings;
using FluentValidation;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Uncomment to require API key authentication
public class MentoriasController : ControllerBase
{
    private readonly IMentoriaService _mentoriaService;
    private readonly ILogger<MentoriasController> _logger;
    private readonly IValidator<CreateMentoriaRequestDto> _createValidator;
    private readonly IValidator<UpdateMentoriaRequestDto> _updateValidator;

    public MentoriasController(
        IMentoriaService mentoriaService,
        ILogger<MentoriasController> logger,
        IValidator<CreateMentoriaRequestDto> createValidator,
        IValidator<UpdateMentoriaRequestDto> updateValidator)
    {
        _mentoriaService = mentoriaService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MentoriaResponseDto>> GetMentoriaById(Guid id)
    {
        try
        {
            var mentoria = await _mentoriaService.GetMentoriaByIdAsync(id);
            if (mentoria == null)
                return NotFound(new { message = $"Mentoria with ID {id} not found" });
            return Ok(mentoria.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentoria {MentoriaId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("mentor/{mentorId}")]
    public async Task<ActionResult<MentoriaListResponseDto>> GetMentoriasByMentorId(Guid mentorId, [FromQuery] PaginationRequestDto pagination)
    {
        try
        {
            var validator = new Mentoragente.Application.Validators.PaginationRequestValidator();
            var validationResult = await validator.ValidateAsync(pagination);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _mentoriaService.GetMentoriasByMentorIdAsync(mentorId, pagination.Page, pagination.PageSize);
            
            var response = new MentoriaListResponseDto
            {
                Mentorias = result.Items.Select(m => m.ToDto()).ToList(),
                Total = result.Total,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentorias for mentor {MentorId}", mentorId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<MentoriaListResponseDto>> GetActiveMentorias([FromQuery] PaginationRequestDto pagination)
    {
        try
        {
            var validator = new Mentoragente.Application.Validators.PaginationRequestValidator();
            var validationResult = await validator.ValidateAsync(pagination);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _mentoriaService.GetActiveMentoriasAsync(pagination.Page, pagination.PageSize);
            
            var response = new MentoriaListResponseDto
            {
                Mentorias = result.Items.Select(m => m.ToDto()).ToList(),
                Total = result.Total,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active mentorias");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<MentoriaResponseDto>> CreateMentoria([FromBody] CreateMentoriaRequestDto request)
    {
        try
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var mentoria = await _mentoriaService.CreateMentoriaAsync(
                request.MentorId, request.Nome, request.AssistantId, request.DuracaoDias, request.Descricao);

            return CreatedAtAction(nameof(GetMentoriaById), new { id = mentoria.Id }, mentoria.ToDto());
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
            _logger.LogError(ex, "Error creating mentoria");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MentoriaResponseDto>> UpdateMentoria(Guid id, [FromBody] UpdateMentoriaRequestDto request)
    {
        try
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var mentoria = await _mentoriaService.UpdateMentoriaAsync(
                id, request.Nome, request.AssistantId, request.DuracaoDias, request.Descricao,
                EntityToDtoMappings.ParseMentoriaStatus(request.Status));

            return Ok(mentoria.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating mentoria {MentoriaId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMentoria(Guid id)
    {
        try
        {
            var deleted = await _mentoriaService.DeleteMentoriaAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Mentoria with ID {id} not found" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mentoria {MentoriaId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
