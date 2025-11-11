namespace Mentoragente.Domain.Models;

/// <summary>
/// Z-API webhook payload structure
/// </summary>
public class ZApiWebhookDto
{
    public bool WaitingMessage { get; set; }
    public bool IsGroup { get; set; }
    public string? InstanceId { get; set; }
    public string? MessageId { get; set; }
    public string? Phone { get; set; }
    public bool FromMe { get; set; }
    public long? Momment { get; set; }
    public string? Status { get; set; }
    public string? ChatName { get; set; }
    public string? SenderPhoto { get; set; }
    public string? SenderName { get; set; }
    public string? ParticipantPhone { get; set; }
    public string? ParticipantLid { get; set; }
    public string? Photo { get; set; }
    public bool Broadcast { get; set; }
    public string? Type { get; set; }
    public ZApiTextMessage? Text { get; set; }
}

public class ZApiTextMessage
{
    public string? Message { get; set; }
}

