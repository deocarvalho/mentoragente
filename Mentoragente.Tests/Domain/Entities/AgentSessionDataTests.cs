using FluentAssertions;
using Mentoragente.Domain.Entities;
using Xunit;

namespace Mentoragente.Tests.Domain.Entities;

public class AgentSessionDataTests
{
    [Fact]
    public void AgentSessionData_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var data = new AgentSessionData();

        // Assert
        data.AgentSessionId.Should().BeEmpty();
        data.ProgressPercentage.Should().Be(0);
        data.ReportGenerated.Should().BeFalse();
        data.ReportGeneratedAt.Should().BeNull();
        data.AdminNotes.Should().BeNull();
        data.CustomPropertiesJson.Should().BeNull();
        data.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        data.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AgentSessionData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var agentSessionId = Guid.NewGuid();
        var accessStartDate = DateTime.UtcNow.AddDays(-10);
        var accessEndDate = DateTime.UtcNow.AddDays(20);
        var progressPercentage = 50;
        var reportGenerated = true;
        var reportGeneratedAt = DateTime.UtcNow;
        var adminNotes = "Test notes";

        // Act
        var data = new AgentSessionData
        {
            AgentSessionId = agentSessionId,
            AccessStartDate = accessStartDate,
            AccessEndDate = accessEndDate,
            ProgressPercentage = progressPercentage,
            ReportGenerated = reportGenerated,
            ReportGeneratedAt = reportGeneratedAt,
            AdminNotes = adminNotes
        };

        // Assert
        data.AgentSessionId.Should().Be(agentSessionId);
        data.AccessStartDate.Should().Be(accessStartDate);
        data.AccessEndDate.Should().Be(accessEndDate);
        data.ProgressPercentage.Should().Be(progressPercentage);
        data.ReportGenerated.Should().Be(reportGenerated);
        data.ReportGeneratedAt.Should().Be(reportGeneratedAt);
        data.AdminNotes.Should().Be(adminNotes);
    }
}

