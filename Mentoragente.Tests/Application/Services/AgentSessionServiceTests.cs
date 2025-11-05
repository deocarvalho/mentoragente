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
    private readonly Mock<IMentoriaRepository> _mockMentoriaRepository;
    private readonly Mock<ILogger<AgentSessionService>> _mockLogger;
    private readonly AgentSessionService _agentSessionService;

    public AgentSessionServiceTests()
    {
        _mockAgentSessionRepository = new Mock<IAgentSessionRepository>();
        _mockAgentSessionDataRepository = new Mock<IAgentSessionDataRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMentoriaRepository = new Mock<IMentoriaRepository>();
        _mockLogger = new Mock<ILogger<AgentSessionService>>();
        _agentSessionService = new AgentSessionService(
            _mockAgentSessionRepository.Object,
            _mockAgentSessionDataRepository.Object,
            _mockUserRepository.Object,
            _mockMentoriaRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateAgentSessionAsync_ShouldThrowWhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentoriaId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _agentSessionService.Invoking(s => s.CreateAgentSessionAsync(userId, mentoriaId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*User with ID {userId} not found*");
    }

    [Fact]
    public async Task CreateAgentSessionAsync_ShouldCreateSessionAndData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentoriaId = Guid.NewGuid();
        var user = new User { Id = userId };
        var mentoria = new Mentoria { Id = mentoriaId, DuracaoDias = 30 };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockMentoriaRepository.Setup(x => x.GetMentoriaByIdAsync(mentoriaId))
            .ReturnsAsync(mentoria);

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionAsync(userId, mentoriaId))
            .ReturnsAsync((AgentSession?)null);

        _mockAgentSessionRepository.Setup(x => x.CreateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        _mockAgentSessionDataRepository.Setup(x => x.CreateAgentSessionDataAsync(It.IsAny<AgentSessionData>()))
            .ReturnsAsync((AgentSessionData d) => d);

        // Act
        var result = await _agentSessionService.CreateAgentSessionAsync(userId, mentoriaId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.MentoriaId.Should().Be(mentoriaId);
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
}

