using FluentAssertions;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Xunit;

namespace Mentoragente.Tests.Domain.Entities;

public class AgentSessionTests
{
    [Fact]
    public void AgentSession_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var session = new AgentSession();

        // Assert
        session.Id.Should().NotBeEmpty();
        session.UserId.Should().BeEmpty();
        session.MentoriaId.Should().BeEmpty();
        session.AIProvider.Should().Be(AIProvider.OpenAI);
        session.AIContextId.Should().BeNull();
        session.Status.Should().Be(AgentSessionStatus.Active);
        session.LastInteraction.Should().BeNull();
        session.TotalMessages.Should().Be(0);
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        session.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AgentSession_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mentoriaId = Guid.NewGuid();
        var aiContextId = "thread_ABC123";
        var status = AgentSessionStatus.Active;
        var lastInteraction = DateTime.UtcNow.AddMinutes(-5);
        var totalMessages = 10;

        // Act
        var session = new AgentSession
        {
            Id = id,
            UserId = userId,
            MentoriaId = mentoriaId,
            AIContextId = aiContextId,
            Status = status,
            LastInteraction = lastInteraction,
            TotalMessages = totalMessages
        };

        // Assert
        session.Id.Should().Be(id);
        session.UserId.Should().Be(userId);
        session.MentoriaId.Should().Be(mentoriaId);
        session.AIContextId.Should().Be(aiContextId);
        session.Status.Should().Be(status);
        session.LastInteraction.Should().Be(lastInteraction);
        session.TotalMessages.Should().Be(totalMessages);
    }
}

