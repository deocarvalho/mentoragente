using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Application.Models;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IMentorshipService
{
    Task<Mentorship?> GetMentorshipByIdAsync(Guid id);
    Task<List<Mentorship>> GetMentorshipsByMentorIdAsync(Guid mentorId);
    Task<PagedResult<Mentorship>> GetMentorshipsByMentorIdAsync(Guid mentorId, int page = 1, int pageSize = 10);
    Task<List<Mentorship>> GetActiveMentorshipsAsync();
    Task<PagedResult<Mentorship>> GetActiveMentorshipsAsync(int page = 1, int pageSize = 10);
    Task<Mentorship> CreateMentorshipAsync(
        Guid mentorId,
        string name,
        string assistantId,
        int durationDays,
        string? description = null,
        WhatsAppProvider? whatsAppProvider = null,
        string? instanceCode = null,
        string? instanceToken = null);
    Task<Mentorship> UpdateMentorshipAsync(
        Guid id,
        string? name = null,
        string? assistantId = null,
        int? durationDays = null,
        string? description = null,
        MentorshipStatus? status = null,
        WhatsAppProvider? whatsAppProvider = null,
        string? instanceCode = null,
        string? instanceToken = null);
    Task<bool> DeleteMentorshipAsync(Guid id);
}

public class MentorshipService : IMentorshipService
{
    private readonly IMentorshipRepository _mentorshipRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<MentorshipService> _logger;

    public MentorshipService(
        IMentorshipRepository mentorshipRepository,
        IUserRepository userRepository,
        ILogger<MentorshipService> logger)
    {
        _mentorshipRepository = mentorshipRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Mentorship?> GetMentorshipByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting mentorship by ID: {MentorshipId}", id);
        return await _mentorshipRepository.GetMentorshipByIdAsync(id);
    }

    public async Task<List<Mentorship>> GetMentorshipsByMentorIdAsync(Guid mentorId)
    {
        _logger.LogInformation("Getting mentorships for mentor: {MentorId}", mentorId);
        return await _mentorshipRepository.GetMentorshipsByMentorIdAsync(mentorId);
    }

    public async Task<PagedResult<Mentorship>> GetMentorshipsByMentorIdAsync(Guid mentorId, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting mentorships for mentor: {MentorId}, Page: {Page}, PageSize: {PageSize}", mentorId, page, pageSize);
        var skip = (page - 1) * pageSize;
        var mentorships = await _mentorshipRepository.GetMentorshipsByMentorIdAsync(mentorId, skip, pageSize);
        var total = await _mentorshipRepository.GetMentorshipsCountByMentorIdAsync(mentorId);
        return PagedResult<Mentorship>.Create(mentorships, total, page, pageSize);
    }

    public async Task<List<Mentorship>> GetActiveMentorshipsAsync()
    {
        _logger.LogInformation("Getting active mentorships");
        var allMentorships = await _mentorshipRepository.GetAllMentorshipsAsync();
        return allMentorships.Where(m => m.Status == MentorshipStatus.Active).ToList();
    }

    public async Task<PagedResult<Mentorship>> GetActiveMentorshipsAsync(int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting active mentorships - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        var skip = (page - 1) * pageSize;
        var mentorships = await _mentorshipRepository.GetActiveMentorshipsAsync(skip, pageSize);
        var total = await _mentorshipRepository.GetActiveMentorshipsCountAsync();
        return PagedResult<Mentorship>.Create(mentorships, total, page, pageSize);
    }

    public async Task<Mentorship> CreateMentorshipAsync(
        Guid mentorId,
        string name,
        string assistantId,
        int durationDays,
        string? description = null,
        WhatsAppProvider? whatsAppProvider = null,
        string? instanceCode = null,
        string? instanceToken = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(assistantId))
            throw new ArgumentException("Assistant ID is required", nameof(assistantId));

        if (durationDays <= 0)
            throw new ArgumentException("Duration in days must be greater than 0", nameof(durationDays));

        if (string.IsNullOrWhiteSpace(instanceCode))
            throw new ArgumentException("Instance code is required", nameof(instanceCode));

        // Verify mentor exists
        var mentor = await _userRepository.GetUserByIdAsync(mentorId);
        if (mentor == null)
        {
            _logger.LogError("Mentor {MentorId} not found", mentorId);
            throw new InvalidOperationException($"Mentor with ID {mentorId} not found");
        }

        var mentorship = new Mentorship
        {
            MentorId = mentorId,
            Name = name,
            AssistantId = assistantId,
            DurationDays = durationDays,
            Description = description,
            WhatsAppProvider = whatsAppProvider ?? WhatsAppProvider.ZApi,
            InstanceCode = instanceCode,
            InstanceToken = instanceToken,
            Status = MentorshipStatus.Active
        };

        _logger.LogInformation("Creating new mentorship: {Name}, Mentor: {MentorId}", name, mentorId);
        return await _mentorshipRepository.CreateMentorshipAsync(mentorship);
    }

    public async Task<Mentorship> UpdateMentorshipAsync(
        Guid id,
        string? name = null,
        string? assistantId = null,
        int? durationDays = null,
        string? description = null,
        MentorshipStatus? status = null,
        WhatsAppProvider? whatsAppProvider = null,
        string? instanceCode = null,
        string? instanceToken = null)
    {
        var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(id);
        if (mentorship == null)
        {
            _logger.LogError("Mentorship {MentorshipId} not found for update", id);
            throw new InvalidOperationException($"Mentorship with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(name))
            mentorship.Name = name;

        if (!string.IsNullOrWhiteSpace(assistantId))
            mentorship.AssistantId = assistantId;

        if (durationDays.HasValue && durationDays.Value > 0)
            mentorship.DurationDays = durationDays.Value;

        if (description != null)
            mentorship.Description = description;

        if (status.HasValue)
            mentorship.Status = status.Value;

        if (whatsAppProvider.HasValue)
            mentorship.WhatsAppProvider = whatsAppProvider.Value;

        if (!string.IsNullOrWhiteSpace(instanceCode))
            mentorship.InstanceCode = instanceCode;

        if (instanceToken != null)
            mentorship.InstanceToken = instanceToken;

        _logger.LogInformation("Updating mentorship {MentorshipId}", id);
        return await _mentorshipRepository.UpdateMentorshipAsync(mentorship);
    }

    public async Task<bool> DeleteMentorshipAsync(Guid id)
    {
        var mentorship = await _mentorshipRepository.GetMentorshipByIdAsync(id);
        if (mentorship == null)
        {
            _logger.LogWarning("Mentorship {MentorshipId} not found for deletion", id);
            return false;
        }

        // Soft delete: mark as Archived
        mentorship.Status = MentorshipStatus.Archived;
        await _mentorshipRepository.UpdateMentorshipAsync(mentorship);
        
        _logger.LogInformation("Soft deleted mentorship {MentorshipId}", id);
        return true;
    }
}

