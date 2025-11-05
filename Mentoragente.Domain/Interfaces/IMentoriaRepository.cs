using Mentoragente.Domain.Entities;

namespace Mentoragente.Domain.Interfaces;

public interface IMentoriaRepository
{
    Task<Mentoria?> GetMentoriaByIdAsync(Guid id);
    Task<List<Mentoria>> GetMentoriasByMentorIdAsync(Guid mentorId);
    Task<List<Mentoria>> GetMentoriasByMentorIdAsync(Guid mentorId, int skip, int take);
    Task<int> GetMentoriasCountByMentorIdAsync(Guid mentorId);
    Task<List<Mentoria>> GetAllMentoriasAsync();
    Task<List<Mentoria>> GetActiveMentoriasAsync(int skip, int take);
    Task<int> GetActiveMentoriasCountAsync();
    Task<Mentoria> CreateMentoriaAsync(Mentoria mentoria);
    Task<Mentoria> UpdateMentoriaAsync(Mentoria mentoria);
}

