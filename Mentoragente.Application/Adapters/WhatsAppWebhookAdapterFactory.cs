namespace Mentoragente.Application.Adapters;

/// <summary>
/// Factory for getting the appropriate webhook adapter based on payload type
/// </summary>
public class WhatsAppWebhookAdapterFactory
{
    private readonly IEnumerable<IWhatsAppWebhookAdapter> _adapters;

    public WhatsAppWebhookAdapterFactory(IEnumerable<IWhatsAppWebhookAdapter> adapters)
    {
        _adapters = adapters;
    }

    /// <summary>
    /// Gets the adapter that can handle the given webhook payload
    /// </summary>
    public IWhatsAppWebhookAdapter? GetAdapter(object webhookPayload)
    {
        return _adapters.FirstOrDefault(a => a.CanHandle(webhookPayload));
    }

    /// <summary>
    /// Gets the adapter for a specific provider
    /// </summary>
    public IWhatsAppWebhookAdapter GetAdapter(Domain.Enums.WhatsAppProvider provider)
    {
        return _adapters.FirstOrDefault(a => a.Provider == provider)
            ?? throw new NotSupportedException($"Provider {provider} is not supported");
    }
}

