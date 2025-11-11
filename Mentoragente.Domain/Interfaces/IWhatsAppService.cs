using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;

namespace Mentoragente.Domain.Interfaces;

/// <summary>
/// Generic interface for WhatsApp service providers
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Sends a message via the WhatsApp provider
    /// </summary>
    Task<bool> SendMessageAsync(string phoneNumber, string message, Mentorship mentorship);
    
    /// <summary>
    /// The provider this service implements
    /// </summary>
    WhatsAppProvider Provider { get; }
}

