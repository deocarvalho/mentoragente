using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Models;

namespace Mentoragente.Application.Adapters;

public class EvolutionWebhookAdapter : IWhatsAppWebhookAdapter
{
    public WhatsAppProvider Provider => WhatsAppProvider.EvolutionAPI;

    public bool CanHandle(object webhookPayload)
    {
        return webhookPayload is EvolutionWebhookDto;
    }

    public WhatsAppMessage? Adapt(object webhookPayload)
    {
        if (webhookPayload is not EvolutionWebhookDto dto)
            return null;

        // Only process messages.upsert events
        if (dto.Event != "messages.upsert" || dto.Data == null)
            return null;

        // Ignore messages from self
        if (dto.Data.Key?.FromMe == true)
            return null;

        var messageText = dto.Data.Message?.Conversation ?? string.Empty;
        if (string.IsNullOrEmpty(messageText))
            return null;

        var phoneNumber = ExtractPhoneNumber(dto.Data.Key?.RemoteJid ?? string.Empty);
        if (string.IsNullOrEmpty(phoneNumber))
            return null;

        return new WhatsAppMessage
        {
            PhoneNumber = phoneNumber,
            MessageText = messageText,
            FromMe = dto.Data.Key!.FromMe,
            MessageId = dto.Data.Key.RemoteJid,
            Timestamp = DateTime.UtcNow
        };
    }

    private static string ExtractPhoneNumber(string remoteJid)
    {
        if (string.IsNullOrWhiteSpace(remoteJid))
            return string.Empty;

        var phonePart = remoteJid.Split('@')[0];
        if (phonePart.Contains(':'))
            phonePart = phonePart.Split(':')[0];

        return new string(phonePart.Where(char.IsDigit).ToArray());
    }
}

