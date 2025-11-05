namespace Mentoragente.Domain.Models;

public class WhatsAppWebhookDto
{
    public string Event { get; set; } = string.Empty;
    public WebhookData? Data { get; set; }
}

public class WebhookData
{
    public WebhookKey? Key { get; set; }
    public WebhookMessage? Message { get; set; }
}

public class WebhookKey
{
    public string RemoteJid { get; set; } = string.Empty;
    public bool FromMe { get; set; }
}

public class WebhookMessage
{
    public string Conversation { get; set; } = string.Empty;
}

