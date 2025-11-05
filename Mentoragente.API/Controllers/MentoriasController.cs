using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MentoriasController : ControllerBase
{
    private readonly IMentoriaService _mentoriaService;
    private readonly ILogger<MentoriasController> _logger;

    public MentoriasController(
        IMentoriaService mentoriaService,
        ILogger<MentoriasController> logger)
    {
        _mentoriaService = mentoriaService;
        _logger = logger;
    }

    /// <summary>
    /// Get mentoria by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Mentoria>> GetMentoriaById(Guid id)
    {
        try
        {
            var mentoria = await _mentoriaService.GetMentoriaByIdAsync(id);
            if (mentoria == null)
            {
                return NotFound(new { message = $"Mentoria with ID {id} not found" });
            }

            return Ok(mentoria);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentoria {MentoriaId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all mentorias by mentor ID
    /// </summary>
    [HttpGet("mentor/{mentorId}")]
    public async Task<ActionResult<List<Mentoria>>> GetMentoriasByMentorId(Guid mentorId)
    {
        try
        {
            var mentorias = await _mentoriaService.GetMentoriasByMentorIdAsync(mentorId);
            return Ok(mentorias);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentorias for mentor {MentorId}", mentorId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all active mentorias
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<Mentoria>>> GetActiveMentorias()
    {
        try
        {
            var mentorias = await _mentoriaService.GetActiveMentoriasAsync();
            return Ok(mentorias);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active mentorias");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new mentoria
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Mentoria>> CreateMentoria([FromBody] CreateMentoriaRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var mentoria = await _mentoriaService.CreateMentoriaAsync(
                request.MentorId,
                request.Nome,
                request.AssistantId,
                request.DuracaoDias,
                request.Descricao);

            return CreatedAtAction(nameof(GetMentoriaById), new { id = mentoria.Id }, mentoria);
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

    /// <summary>
    /// Update mentoria
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Mentoria>> UpdateMentoria(Guid id, [FromBody] UpdateMentoriaRequest request)
    {
        try
        {
            var mentoria = await _mentoriaService.UpdateMentoriaAsync(
                id,
                request.Nome,
                request.AssistantId,
                request.DuracaoDias,
                request.Descricao,
                request.Status);

            return Ok(mentoria);
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

    /// <summary>
    /// Delete mentoria (soft delete - marks as Archived)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMentoria(Guid id)
    {
        try
        {
            var deleted = await _mentoriaService.DeleteMentoriaAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Mentoria with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mentoria {MentoriaId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CreateMentoriaRequest
{
    public Guid MentorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string AssistantId { get; set; } = string.Empty;
    public int DuracaoDias { get; set; }
    public string? Descricao { get; set; }
}

public class UpdateMentoriaRequest
{
    public string? Nome { get; set; }
    public string? AssistantId { get; set; }
    public int? DuracaoDias { get; set; }
    public string? Descricao { get; set; }
    public MentoriaStatus? Status { get; set; }
}

