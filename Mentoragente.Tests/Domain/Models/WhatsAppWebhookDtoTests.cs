using FluentAssertions;
using Mentoragente.Domain.Models;
using Xunit;

namespace Mentoragente.Tests.Domain.Models;

public class WhatsAppWebhookDtoTests
{
    [Fact]
    public void WhatsAppWebhookDto_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var dto = new WhatsAppWebhookDto();

        // Assert
        dto.Event.Should().BeEmpty();
        dto.Data.Should().BeNull();
    }

    [Fact]
    public void WhatsAppWebhookDto_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var eventName = "messages.upsert";
        var data = new WebhookData
        {
            Key = new WebhookKey
            {
                RemoteJid = "5511999999999@s.whatsapp.net",
                FromMe = false
            },
            Message = new WebhookMessage
            {
                Conversation = "Hello!"
            }
        };

        // Act
        var dto = new WhatsAppWebhookDto
        {
            Event = eventName,
            Data = data
        };

        // Assert
        dto.Event.Should().Be(eventName);
        dto.Data.Should().Be(data);
        dto.Data.Key.RemoteJid.Should().Be("5511999999999@s.whatsapp.net");
        dto.Data.Key.FromMe.Should().BeFalse();
        dto.Data.Message.Conversation.Should().Be("Hello!");
    }
}

