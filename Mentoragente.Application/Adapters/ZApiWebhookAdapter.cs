using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Adapters;

public class ZApiWebhookAdapter : IZApiWebhookAdapter
{
    private readonly ILogger<ZApiWebhookAdapter> _logger;

    public ZApiWebhookAdapter(ILogger<ZApiWebhookAdapter> logger)
    {
        _logger = logger;
    }

    public WhatsAppProvider Provider => WhatsAppProvider.ZApi;

    public bool CanHandle(object webhookPayload)
    {
        return webhookPayload is ZApiWebhookDto;
    }

    public WhatsAppMessage? Adapt(object webhookPayload)
    {
        if (webhookPayload is not ZApiWebhookDto dto)
        {
            _logger.LogWarning("Webhook payload is not ZApiWebhookDto. Type: {Type}", webhookPayload?.GetType().Name ?? "null");
            return null;
        }

        // Ignore messages from self
        if (dto.FromMe)
        {
            _logger.LogDebug("Ignoring message from self (FromMe=true)");
            return null;
        }

        if (string.IsNullOrEmpty(dto.Phone))
        {
            _logger.LogWarning("Ignoring message with empty Phone");
            return null;
        }

        // Only handle text messages for now
        if (dto.Type != "text")
        {
            _logger.LogDebug("Ignoring non-text message. Type={Type}", dto.Type);
            return null;
        }

        if (dto.Text?.Message == null || string.IsNullOrEmpty(dto.Text.Message))
        {
            _logger.LogWarning("Ignoring message with empty text. HasText={HasText}", dto.Text != null);
            return null;
        }

        var phoneNumber = NormalizePhoneNumber(dto.Phone);
        if (string.IsNullOrEmpty(phoneNumber))
        {
            _logger.LogWarning("Ignoring message with invalid phone number. Original={Phone}", dto.Phone);
            return null;
        }

        var timestamp = dto.Momment.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(dto.Momment.Value).DateTime
            : DateTime.UtcNow;

        _logger.LogDebug("Successfully adapted Z-API message from {Phone} to {PhoneNumber}", dto.Phone, phoneNumber);

        return new WhatsAppMessage
        {
            PhoneNumber = phoneNumber,
            MessageText = dto.Text.Message,
            FromMe = dto.FromMe,
            MessageId = dto.MessageId,
            Timestamp = timestamp,
            ContactName = dto.SenderName,
            IsGroup = dto.IsGroup
        };
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove non-digit characters
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}

