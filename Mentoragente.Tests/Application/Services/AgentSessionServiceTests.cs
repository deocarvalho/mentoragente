using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Tests.Application.Services;

public class AgentSessionServiceTests
{
    private readonly Mock<IAgentSessionRepository> _mockAgentSessionRepository;
    private readonly Mock<IAgentSessionDataRepository> _mockAgentSessionDataRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMentorshipRepository> _mockMentorshipRepository;
    private readonly Mock<ILogger<AgentSessionService>> _mockLogger;
    private readonly AgentSessionService _agentSessionService;

    public AgentSessionServiceTests()
    {
        _mockAgentSessionRepository = new Mock<IAgentSessionRepository>();
        _mockAgentSessionDataRepository = new Mock<IAgentSessionDataRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMentorshipRepository = new Mock<IMentorshipRepository>();
        _mockLogger = new Mock<ILogger<AgentSessionService>>();
        _agentSessionService = new AgentSessionService(
            _mockAgentSessionRepository.Object,
            _mockAgentSessionDataRepository.Object,
            _mockUserRepository.Object,
            _mockMentorshipRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateAgentSessionAsync_ShouldThrowWhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _agentSessionService.Invoking(s => s.CreateAgentSessionAsync(userId, mentorshipId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*User with ID {userId} not found*");
    }

    [Fact]
    public async Task CreateAgentSessionAsync_ShouldCreateSessionAndData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var user = new User { Id = userId };
        var mentorship = new Mentorship { Id = mentorshipId, DurationDays = 30 };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync((AgentSession?)null);

        _mockAgentSessionRepository.Setup(x => x.CreateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        _mockAgentSessionDataRepository.Setup(x => x.CreateAgentSessionDataAsync(It.IsAny<AgentSessionData>()))
            .ReturnsAsync((AgentSessionData d) => d);

        // Act
        var result = await _agentSessionService.CreateAgentSessionAsync(userId, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.MentorshipId.Should().Be(mentorshipId);
        result.Status.Should().Be(AgentSessionStatus.Active);
        _mockAgentSessionRepository.Verify(x => x.CreateAgentSessionAsync(It.IsAny<AgentSession>()), Times.Once);
        _mockAgentSessionDataRepository.Verify(x => x.CreateAgentSessionDataAsync(It.IsAny<AgentSessionData>()), Times.Once);
    }

    [Fact]
    public async Task GetAgentSessionsByUserIdAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 1;
        var pageSize = 10;
        var sessions = new List<AgentSession>
        {
            new AgentSession { Id = Guid.NewGuid(), UserId = userId },
            new AgentSession { Id = Guid.NewGuid(), UserId = userId }
        };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionsByUserIdAsync(userId, 0, pageSize))
            .ReturnsAsync(sessions);
        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionsCountByUserIdAsync(userId))
            .ReturnsAsync(2);

        // Act
        var result = await _agentSessionService.GetAgentSessionsByUserIdAsync(userId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task ExpireSessionAsync_ShouldUpdateStatus()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AgentSession { Id = sessionId, Status = AgentSessionStatus.Active };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        // Act
        var result = await _agentSessionService.ExpireSessionAsync(sessionId);

        // Assert
        result.Should().BeTrue();
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(
            It.Is<AgentSession>(s => s.Status == AgentSessionStatus.Expired)), Times.Once);
    }

    [Fact]
    public async Task ExpireSessionAsync_ShouldReturnFalseWhenSessionNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync((AgentSession?)null);

        // Act
        var result = await _agentSessionService.ExpireSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAgentSessionByIdAsync_ShouldReturnSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AgentSession { Id = sessionId };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _agentSessionService.GetAgentSessionByIdAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
    }

    [Fact]
    public async Task GetAgentSessionAsync_ShouldReturnSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var session = new AgentSession { UserId = userId, MentorshipId = mentorshipId };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync(session);

        // Act
        var result = await _agentSessionService.GetAgentSessionAsync(userId, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.MentorshipId.Should().Be(mentorshipId);
    }

    [Fact]
    public async Task GetAgentSessionsByUserIdAsync_ShouldReturnList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<AgentSession>
        {
            new AgentSession { Id = Guid.NewGuid(), UserId = userId },
            new AgentSession { Id = Guid.NewGuid(), UserId = userId }
        };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionsByUserIdAsync(userId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _agentSessionService.GetAgentSessionsByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveAgentSessionAsync_ShouldReturnActiveSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var session = new AgentSession 
        { 
            UserId = userId, 
            MentorshipId = mentorshipId,
            Status = AgentSessionStatus.Active 
        };

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync(session);

        // Act
        var result = await _agentSessionService.GetActiveAgentSessionAsync(userId, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(AgentSessionStatus.Active);
    }

    [Fact]
    public async Task CreateAgentSessionAsync_ShouldThrowWhenMentorshipNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var user = new User { Id = userId };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync((Mentorship?)null);

        // Act & Assert
        await _agentSessionService.Invoking(s => s.CreateAgentSessionAsync(userId, mentorshipId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Mentorship with ID {mentorshipId} not found*");
    }

    [Fact]
    public async Task CreateAgentSessionAsync_ShouldThrowWhenActiveSessionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var user = new User { Id = userId };
        var mentorship = new Mentorship { Id = mentorshipId };
        var existingSession = new AgentSession { Id = Guid.NewGuid() };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync(existingSession);

        // Act & Assert
        await _agentSessionService.Invoking(s => s.CreateAgentSessionAsync(userId, mentorshipId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Active session already exists*");
    }

    [Fact]
    public async Task UpdateAgentSessionAsync_ShouldUpdateStatus()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AgentSession { Id = sessionId, Status = AgentSessionStatus.Active };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        // Act
        var result = await _agentSessionService.UpdateAgentSessionAsync(sessionId, AgentSessionStatus.Paused);

        // Assert
        result.Status.Should().Be(AgentSessionStatus.Paused);
    }

    [Fact]
    public async Task UpdateAgentSessionAsync_ShouldUpdateAIContextId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var newThreadId = "thread_NEW123";
        var session = new AgentSession { Id = sessionId };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        // Act
        var result = await _agentSessionService.UpdateAgentSessionAsync(sessionId, aiContextId: newThreadId);

        // Assert
        result.AIContextId.Should().Be(newThreadId);
    }

    [Fact]
    public async Task UpdateAgentSessionAsync_ShouldThrowWhenSessionNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync((AgentSession?)null);

        // Act & Assert
        await _agentSessionService.Invoking(s => s.UpdateAgentSessionAsync(sessionId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Agent session with ID {sessionId} not found*");
    }

    [Fact]
    public async Task PauseSessionAsync_ShouldUpdateStatus()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AgentSession { Id = sessionId, Status = AgentSessionStatus.Active };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        // Act
        var result = await _agentSessionService.PauseSessionAsync(sessionId);

        // Assert
        result.Should().BeTrue();
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(
            It.Is<AgentSession>(s => s.Status == AgentSessionStatus.Paused)), Times.Once);
    }

    [Fact]
    public async Task PauseSessionAsync_ShouldReturnFalseWhenSessionNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync((AgentSession?)null);

        // Act
        var result = await _agentSessionService.PauseSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResumeSessionAsync_ShouldUpdateStatus()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AgentSession { Id = sessionId, Status = AgentSessionStatus.Paused };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        // Act
        var result = await _agentSessionService.ResumeSessionAsync(sessionId);

        // Assert
        result.Should().BeTrue();
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(
            It.Is<AgentSession>(s => s.Status == AgentSessionStatus.Active)), Times.Once);
    }

    [Fact]
    public async Task ResumeSessionAsync_ShouldReturnFalseWhenSessionNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync((AgentSession?)null);

        // Act
        var result = await _agentSessionService.ResumeSessionAsync(sessionId);

        // Assert
        result.Should().BeFalse();
    }
}

