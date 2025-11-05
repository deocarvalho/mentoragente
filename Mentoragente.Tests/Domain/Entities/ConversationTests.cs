using FluentAssertions;
using Mentoragente.Domain.Entities;
using Xunit;

namespace Mentoragente.Tests.Domain.Entities;

public class ConversationTests
{
    [Fact]
    public void Conversation_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var conversation = new Conversation();

        // Assert
        conversation.Id.Should().NotBeEmpty();
        conversation.AgentSessionId.Should().BeEmpty();
        conversation.Sender.Should().BeEmpty();
        conversation.Message.Should().BeEmpty();
        conversation.MessageType.Should().Be("text");
        conversation.TokensUsed.Should().BeNull();
        conversation.ResponseTimeMs.Should().BeNull();
        conversation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Conversation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        var sender = "user";
        var message = "Hello!";
        var messageType = "text";
        var tokensUsed = 100;
        var responseTimeMs = 500;

        // Act
        var conversation = new Conversation
        {
            Id = id,
            AgentSessionId = agentSessionId,
            Sender = sender,
            Message = message,
            MessageType = messageType,
            TokensUsed = tokensUsed,
            ResponseTimeMs = responseTimeMs
        };

        // Assert
        conversation.Id.Should().Be(id);
        conversation.AgentSessionId.Should().Be(agentSessionId);
        conversation.Sender.Should().Be(sender);
        conversation.Message.Should().Be(message);
        conversation.MessageType.Should().Be(messageType);
        conversation.TokensUsed.Should().Be(tokensUsed);
        conversation.ResponseTimeMs.Should().Be(responseTimeMs);
    }
}

