using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;

namespace Mentoragente.Domain.Interfaces;

public interface IZApiService : IWhatsAppService
{
    Task<bool> SendMessageAsync(string phoneNumber, string message, Guid mentorshipId);
}

