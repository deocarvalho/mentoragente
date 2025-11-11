using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Models;

namespace Mentoragente.Application.Adapters;

/// <summary>
/// Interface for adapting provider-specific webhook DTOs to generic WhatsAppMessage
/// </summary>
public interface IWhatsAppWebhookAdapter
{
    /// <summary>
    /// The provider this adapter handles
    /// </summary>
    WhatsAppProvider Provider { get; }
    
    /// <summary>
    /// Checks if this adapter can handle the given webhook payload
    /// </summary>
    bool CanHandle(object webhookPayload);
    
    /// <summary>
    /// Converts provider-specific webhook DTO to generic WhatsAppMessage
    /// Returns null if the webhook should be ignored (e.g., message from self, invalid format)
    /// </summary>
    WhatsAppMessage? Adapt(object webhookPayload);
}

