using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Application.Models;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IMentoriaService
{
    Task<Mentoria?> GetMentoriaByIdAsync(Guid id);
    Task<List<Mentoria>> GetMentoriasByMentorIdAsync(Guid mentorId);
    Task<PagedResult<Mentoria>> GetMentoriasByMentorIdAsync(Guid mentorId, int page = 1, int pageSize = 10);
    Task<List<Mentoria>> GetActiveMentoriasAsync();
    Task<PagedResult<Mentoria>> GetActiveMentoriasAsync(int page = 1, int pageSize = 10);
    Task<Mentoria> CreateMentoriaAsync(
        Guid mentorId,
        string nome,
        string assistantId,
        int duracaoDias,
        string? descricao = null);
    Task<Mentoria> UpdateMentoriaAsync(
        Guid id,
        string? nome = null,
        string? assistantId = null,
        int? duracaoDias = null,
        string? descricao = null,
        MentoriaStatus? status = null);
    Task<bool> DeleteMentoriaAsync(Guid id);
}

public class MentoriaService : IMentoriaService
{
    private readonly IMentoriaRepository _mentoriaRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<MentoriaService> _logger;

    public MentoriaService(
        IMentoriaRepository mentoriaRepository,
        IUserRepository userRepository,
        ILogger<MentoriaService> logger)
    {
        _mentoriaRepository = mentoriaRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Mentoria?> GetMentoriaByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting mentoria by ID: {MentoriaId}", id);
        return await _mentoriaRepository.GetMentoriaByIdAsync(id);
    }

    public async Task<List<Mentoria>> GetMentoriasByMentorIdAsync(Guid mentorId)
    {
        _logger.LogInformation("Getting mentorias for mentor: {MentorId}", mentorId);
        return await _mentoriaRepository.GetMentoriasByMentorIdAsync(mentorId);
    }

    public async Task<PagedResult<Mentoria>> GetMentoriasByMentorIdAsync(Guid mentorId, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting mentorias for mentor: {MentorId}, Page: {Page}, PageSize: {PageSize}", mentorId, page, pageSize);
        var skip = (page - 1) * pageSize;
        var mentorias = await _mentoriaRepository.GetMentoriasByMentorIdAsync(mentorId, skip, pageSize);
        var total = await _mentoriaRepository.GetMentoriasCountByMentorIdAsync(mentorId);
        return PagedResult<Mentoria>.Create(mentorias, total, page, pageSize);
    }

    public async Task<List<Mentoria>> GetActiveMentoriasAsync()
    {
        _logger.LogInformation("Getting active mentorias");
        var allMentorias = await _mentoriaRepository.GetAllMentoriasAsync();
        return allMentorias.Where(m => m.Status == MentoriaStatus.Active).ToList();
    }

    public async Task<PagedResult<Mentoria>> GetActiveMentoriasAsync(int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting active mentorias - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        var skip = (page - 1) * pageSize;
        var mentorias = await _mentoriaRepository.GetActiveMentoriasAsync(skip, pageSize);
        var total = await _mentoriaRepository.GetActiveMentoriasCountAsync();
        return PagedResult<Mentoria>.Create(mentorias, total, page, pageSize);
    }

    public async Task<Mentoria> CreateMentoriaAsync(
        Guid mentorId,
        string nome,
        string assistantId,
        int duracaoDias,
        string? descricao = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required", nameof(nome));

        if (string.IsNullOrWhiteSpace(assistantId))
            throw new ArgumentException("Assistant ID is required", nameof(assistantId));

        if (duracaoDias <= 0)
            throw new ArgumentException("Duração em dias must be greater than 0", nameof(duracaoDias));

        // Verificar se o mentor existe
        var mentor = await _userRepository.GetUserByIdAsync(mentorId);
        if (mentor == null)
        {
            _logger.LogError("Mentor {MentorId} not found", mentorId);
            throw new InvalidOperationException($"Mentor with ID {mentorId} not found");
        }

        var mentoria = new Mentoria
        {
            MentorId = mentorId,
            Nome = nome,
            AssistantId = assistantId,
            DuracaoDias = duracaoDias,
            Descricao = descricao,
            Status = MentoriaStatus.Active
        };

        _logger.LogInformation("Creating new mentoria: {Nome}, Mentor: {MentorId}", nome, mentorId);
        return await _mentoriaRepository.CreateMentoriaAsync(mentoria);
    }

    public async Task<Mentoria> UpdateMentoriaAsync(
        Guid id,
        string? nome = null,
        string? assistantId = null,
        int? duracaoDias = null,
        string? descricao = null,
        MentoriaStatus? status = null)
    {
        var mentoria = await _mentoriaRepository.GetMentoriaByIdAsync(id);
        if (mentoria == null)
        {
            _logger.LogError("Mentoria {MentoriaId} not found for update", id);
            throw new InvalidOperationException($"Mentoria with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(nome))
            mentoria.Nome = nome;

        if (!string.IsNullOrWhiteSpace(assistantId))
            mentoria.AssistantId = assistantId;

        if (duracaoDias.HasValue && duracaoDias.Value > 0)
            mentoria.DuracaoDias = duracaoDias.Value;

        if (descricao != null)
            mentoria.Descricao = descricao;

        if (status.HasValue)
            mentoria.Status = status.Value;

        _logger.LogInformation("Updating mentoria {MentoriaId}", id);
        return await _mentoriaRepository.UpdateMentoriaAsync(mentoria);
    }

    public async Task<bool> DeleteMentoriaAsync(Guid id)
    {
        var mentoria = await _mentoriaRepository.GetMentoriaByIdAsync(id);
        if (mentoria == null)
        {
            _logger.LogWarning("Mentoria {MentoriaId} not found for deletion", id);
            return false;
        }

        // Soft delete: marcar como Archived
        mentoria.Status = MentoriaStatus.Archived;
        await _mentoriaRepository.UpdateMentoriaAsync(mentoria);
        
        _logger.LogInformation("Soft deleted mentoria {MentoriaId}", id);
        return true;
    }
}

