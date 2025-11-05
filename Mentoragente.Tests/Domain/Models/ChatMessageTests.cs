using FluentAssertions;
using Mentoragente.Domain.Models;
using Xunit;

namespace Mentoragente.Tests.Domain.Models;

public class ChatMessageTests
{
    [Fact]
    public void ChatMessage_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var message = new ChatMessage();

        // Assert
        message.Role.Should().BeEmpty();
        message.Content.Should().BeEmpty();
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ChatMessage_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var role = "user";
        var content = "Hello!";
        var createdAt = DateTime.UtcNow;

        // Act
        var message = new ChatMessage
        {
            Role = role,
            Content = content,
            CreatedAt = createdAt
        };

        // Assert
        message.Role.Should().Be(role);
        message.Content.Should().Be(content);
        message.CreatedAt.Should().Be(createdAt);
    }
}

