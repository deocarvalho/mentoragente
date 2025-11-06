using Mentoragente.Domain.Entities;

namespace Mentoragente.Domain.Interfaces;

public interface IMentorshipRepository
{
    Task<Mentorship?> GetMentorshipByIdAsync(Guid id);
    Task<List<Mentorship>> GetMentorshipsByMentorIdAsync(Guid mentorId);
    Task<List<Mentorship>> GetMentorshipsByMentorIdAsync(Guid mentorId, int skip, int take);
    Task<int> GetMentorshipsCountByMentorIdAsync(Guid mentorId);
    Task<List<Mentorship>> GetAllMentorshipsAsync();
    Task<List<Mentorship>> GetActiveMentorshipsAsync(int skip, int take);
    Task<int> GetActiveMentorshipsCountAsync();
    Task<Mentorship> CreateMentorshipAsync(Mentorship mentorship);
    Task<Mentorship> UpdateMentorshipAsync(Mentorship mentorship);
}

