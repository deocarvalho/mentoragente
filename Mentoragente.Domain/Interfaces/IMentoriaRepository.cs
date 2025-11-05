using Mentoragente.Domain.Entities;

namespace Mentoragente.Domain.Interfaces;

public interface IMentoriaRepository
{
    Task<Mentoria?> GetMentoriaByIdAsync(Guid id);
    Task<List<Mentoria>> GetMentoriasByMentorIdAsync(Guid mentorId);
    Task<Mentoria> CreateMentoriaAsync(Mentoria mentoria);
    Task<Mentoria> UpdateMentoriaAsync(Mentoria mentoria);
}

