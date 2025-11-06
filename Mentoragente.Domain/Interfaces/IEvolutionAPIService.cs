namespace Mentoragente.Domain.Interfaces;

public interface IEvolutionAPIService
{
    Task<bool> SendMessageAsync(string phoneNumber, string message, Guid mentorshipId);
}

