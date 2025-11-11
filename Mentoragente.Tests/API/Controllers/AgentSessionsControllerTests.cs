using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mentoragente.API.Controllers;
using Mentoragente.Application.Services;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Application.Models;
using FluentValidation;

namespace Mentoragente.Tests.API.Controllers;

public class AgentSessionsControllerTests
{
    private readonly Mock<IAgentSessionService> _mockAgentSessionService;
    private readonly Mock<ILogger<AgentSessionsController>> _mockLogger;
    private readonly Mock<IValidator<CreateAgentSessionRequestDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateAgentSessionRequestDto>> _mockUpdateValidator;
    private readonly AgentSessionsController _controller;

    public AgentSessionsControllerTests()
    {
        _mockAgentSessionService = new Mock<IAgentSessionService>();
        _mockLogger = new Mock<ILogger<AgentSessionsController>>();
        _mockCreateValidator = new Mock<IValidator<CreateAgentSessionRequestDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateAgentSessionRequestDto>>();

        _controller = new AgentSessionsController(
            _mockAgentSessionService.Object,
            _mockLogger.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object);
    }

    [Fact]
    public async Task GetAgentSessionsByUserId_ShouldReturnPagedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
        var sessions = new List<AgentSession>
        {
            new AgentSession { Id = Guid.NewGuid(), UserId = userId }
        };
        var pagedResult = PagedResult<AgentSession>.Create(sessions, 1, 1, 10);

        _mockAgentSessionService.Setup(x => x.GetAgentSessionsByUserIdAsync(userId, 1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAgentSessionsByUserId(userId, pagination);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateAgentSession_ShouldThrowExceptionWhenSessionExists()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.NewGuid()
        };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockAgentSessionService.Setup(x => x.CreateAgentSessionAsync(
            request.UserId, request.MentorshipId, request.AIContextId))
            .ThrowsAsync(new InvalidOperationException("Active session already exists"));

        // Act & Assert
        // Exception handling is now done by GlobalExceptionHandlingMiddleware
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.CreateAgentSession(request));
    }

    [Fact]
    public async Task ExpireSession_ShouldReturnOkWhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionService.Setup(x => x.ExpireSessionAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ExpireSession(sessionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAgentSessionById_ShouldReturnSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AgentSession { Id = sessionId };

        _mockAgentSessionService.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetAgentSessionById(sessionId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAgentSessionById_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionService.Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync((AgentSession?)null);

        // Act
        var result = await _controller.GetAgentSessionById(sessionId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAgentSession_ShouldReturnSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var session = new AgentSession { UserId = userId, MentorshipId = mentorshipId };

        _mockAgentSessionService.Setup(x => x.GetAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetAgentSession(userId, mentorshipId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActiveAgentSession_ShouldReturnSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var session = new AgentSession { UserId = userId, MentorshipId = mentorshipId, Status = AgentSessionStatus.Active };

        _mockAgentSessionService.Setup(x => x.GetActiveAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetActiveAgentSession(userId, mentorshipId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateAgentSession_ShouldReturnCreatedWhenSuccessful()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.NewGuid()
        };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var session = new AgentSession { Id = Guid.NewGuid(), UserId = request.UserId, MentorshipId = request.MentorshipId };

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockAgentSessionService.Setup(x => x.CreateAgentSessionAsync(
            request.UserId, request.MentorshipId, request.AIContextId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.CreateAgentSession(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateAgentSession_ShouldReturnUpdatedSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateAgentSessionRequestDto { Status = "Paused" };
        var session = new AgentSession { Id = sessionId, Status = AgentSessionStatus.Paused };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockUpdateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockAgentSessionService.Setup(x => x.UpdateAgentSessionAsync(
            sessionId, It.IsAny<AgentSessionStatus?>(), request.AIContextId, request.LastInteraction))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.UpdateAgentSession(sessionId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateAgentSession_ShouldThrowExceptionWhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateAgentSessionRequestDto();
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockUpdateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockAgentSessionService.Setup(x => x.UpdateAgentSessionAsync(
            sessionId, It.IsAny<AgentSessionStatus?>(), request.AIContextId, request.LastInteraction))
            .ThrowsAsync(new InvalidOperationException("Session not found"));

        // Act & Assert
        // Exception handling is now done by GlobalExceptionHandlingMiddleware
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.UpdateAgentSession(sessionId, request));
    }

    [Fact]
    public async Task PauseSession_ShouldReturnOkWhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionService.Setup(x => x.PauseSessionAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.PauseSession(sessionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResumeSession_ShouldReturnOkWhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionService.Setup(x => x.ResumeSessionAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ResumeSession(sessionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ExpireSession_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockAgentSessionService.Setup(x => x.ExpireSessionAsync(sessionId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ExpireSession(sessionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}

