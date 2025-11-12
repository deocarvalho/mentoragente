using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Models;

namespace Mentoragente.Application.Adapters;

public class ZApiWebhookAdapter : IZApiWebhookAdapter
{
    public WhatsAppProvider Provider => WhatsAppProvider.ZApi;

    public bool CanHandle(object webhookPayload)
    {
        return webhookPayload is ZApiWebhookDto;
    }

    public WhatsAppMessage? Adapt(object webhookPayload)
    {
        if (webhookPayload is not ZApiWebhookDto dto)
            return null;

        // Ignore messages from self
        if (dto.FromMe || string.IsNullOrEmpty(dto.Phone))
            return null;

        // Only handle text messages for now
        if (dto.Type != "text" || dto.Text?.Message == null || string.IsNullOrEmpty(dto.Text.Message))
            return null;

        var phoneNumber = NormalizePhoneNumber(dto.Phone);
        if (string.IsNullOrEmpty(phoneNumber))
            return null;

        var timestamp = dto.Momment.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(dto.Momment.Value).DateTime
            : DateTime.UtcNow;

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

