namespace Mentoragente.Domain.Models;

/// <summary>
/// Evolution API webhook payload structure
/// </summary>
public class EvolutionWebhookDto
{
    public string Event { get; set; } = string.Empty;
    public EvolutionWebhookData? Data { get; set; }
}

public class EvolutionWebhookData
{
    public EvolutionWebhookKey? Key { get; set; }
    public EvolutionWebhookMessage? Message { get; set; }
}

public class EvolutionWebhookKey
{
    public string RemoteJid { get; set; } = string.Empty;
    public bool FromMe { get; set; }
}

public class EvolutionWebhookMessage
{
    public string Conversation { get; set; } = string.Empty;
}

