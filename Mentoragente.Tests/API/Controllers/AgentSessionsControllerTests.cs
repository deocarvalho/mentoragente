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
    public async Task CreateAgentSession_ShouldReturnConflictWhenSessionExists()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentoriaId = Guid.NewGuid()
        };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockAgentSessionService.Setup(x => x.CreateAgentSessionAsync(
            request.UserId, request.MentoriaId, request.AIContextId))
            .ThrowsAsync(new InvalidOperationException("Active session already exists"));

        // Act
        var result = await _controller.CreateAgentSession(request);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
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
}

