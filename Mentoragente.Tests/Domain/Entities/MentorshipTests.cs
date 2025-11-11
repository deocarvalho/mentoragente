using FluentAssertions;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Xunit;

namespace Mentoragente.Tests.Domain.Entities;

public class MentorshipTests
{
    [Fact]
    public void Mentorship_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var mentorship = new Mentorship();

        // Assert
        mentorship.Id.Should().NotBeEmpty();
        mentorship.Name.Should().BeEmpty();
        mentorship.MentorId.Should().BeEmpty();
        mentorship.AssistantId.Should().BeEmpty();
        mentorship.DurationDays.Should().Be(0);
        mentorship.Description.Should().BeNull();
        mentorship.Status.Should().Be(MentorshipStatus.Active);
        mentorship.WhatsAppProvider.Should().Be(WhatsAppProvider.ZApi);
        mentorship.InstanceCode.Should().BeEmpty();
        mentorship.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        mentorship.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Mentorship_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mentorId = Guid.NewGuid();
        var name = "Nina - Mentorship Offer Discovery";
        var assistantId = "asst_ABC123";
        var durationDays = 30;
        var description = "30-day program";
        var status = MentorshipStatus.Active;
        var whatsAppProvider = WhatsAppProvider.ZApi;
        var instanceCode = "test_instance";

        // Act
        var mentorship = new Mentorship
        {
            Id = id,
            MentorId = mentorId,
            Name = name,
            AssistantId = assistantId,
            DurationDays = durationDays,
            Description = description,
            Status = status,
            WhatsAppProvider = whatsAppProvider,
            InstanceCode = instanceCode
        };

        // Assert
        mentorship.Id.Should().Be(id);
        mentorship.MentorId.Should().Be(mentorId);
        mentorship.Name.Should().Be(name);
        mentorship.AssistantId.Should().Be(assistantId);
        mentorship.DurationDays.Should().Be(durationDays);
        mentorship.Description.Should().Be(description);
        mentorship.Status.Should().Be(status);
        mentorship.WhatsAppProvider.Should().Be(whatsAppProvider);
        mentorship.InstanceCode.Should().Be(instanceCode);
    }
}

