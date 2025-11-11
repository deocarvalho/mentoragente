using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Interfaces;

namespace Mentoragente.Application.Services;

/// <summary>
/// Factory for getting the appropriate WhatsApp service based on mentorship configuration
/// </summary>
public interface IWhatsAppServiceFactory
{
    /// <summary>
    /// Gets the WhatsApp service for a specific provider
    /// </summary>
    IWhatsAppService GetService(Domain.Enums.WhatsAppProvider provider);
    
    /// <summary>
    /// Gets the WhatsApp service based on the mentorship's provider configuration
    /// </summary>
    IWhatsAppService GetServiceForMentorship(Mentorship mentorship);
}

