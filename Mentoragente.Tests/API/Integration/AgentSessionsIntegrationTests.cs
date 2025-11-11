using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Mentoragente.API;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Application.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;

namespace Mentoragente.Tests.API.Integration;

public class AgentSessionsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly IntegrationTestHelper _helper;
    private readonly HttpClient _client;

    public AgentSessionsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _helper = new IntegrationTestHelper();
        _client = _helper.Client;
    }

    [Fact]
    public async Task GetAgentSessionById_ShouldReturnSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AgentSession
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.NewGuid(),
            Status = AgentSessionStatus.Active,
            AIContextId = "thread_123"
        };

        _helper.MockAgentSessionService
            .Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var response = await _client.GetAsync($"/api/agentsessions/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentSessionResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.Status.Should().Be("Active");
        result.AIContextId.Should().Be("thread_123");
    }

    [Fact]
    public async Task GetAgentSessionById_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.GetAgentSessionByIdAsync(sessionId))
            .ReturnsAsync((AgentSession?)null);

        // Act
        var response = await _client.GetAsync($"/api/agentsessions/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAgentSession_ShouldReturnSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var session = new AgentSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MentorshipId = mentorshipId,
            Status = AgentSessionStatus.Active
        };

        _helper.MockAgentSessionService
            .Setup(x => x.GetAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync(session);

        // Act
        var response = await _client.GetAsync($"/api/agentsessions/user/{userId}/mentorship/{mentorshipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentSessionResponseDto>();
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.MentorshipId.Should().Be(mentorshipId);
    }

    [Fact]
    public async Task GetAgentSession_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.GetAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync((AgentSession?)null);

        // Act
        var response = await _client.GetAsync($"/api/agentsessions/user/{userId}/mentorship/{mentorshipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActiveAgentSession_ShouldReturnSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();
        var session = new AgentSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MentorshipId = mentorshipId,
            Status = AgentSessionStatus.Active
        };

        _helper.MockAgentSessionService
            .Setup(x => x.GetActiveAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync(session);

        // Act
        var response = await _client.GetAsync($"/api/agentsessions/user/{userId}/mentorship/{mentorshipId}/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentSessionResponseDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetActiveAgentSession_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mentorshipId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.GetActiveAgentSessionAsync(userId, mentorshipId))
            .ReturnsAsync((AgentSession?)null);

        // Act
        var response = await _client.GetAsync($"/api/agentsessions/user/{userId}/mentorship/{mentorshipId}/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAgentSessionsByUserId_ShouldReturnPagedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<AgentSession>
        {
            new AgentSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MentorshipId = Guid.NewGuid(),
                Status = AgentSessionStatus.Active
            },
            new AgentSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MentorshipId = Guid.NewGuid(),
                Status = AgentSessionStatus.Paused
            }
        };
        var pagedResult = PagedResult<AgentSession>.Create(sessions, 2, 1, 10);

        _helper.MockAgentSessionService
            .Setup(x => x.GetAgentSessionsByUserIdAsync(userId, 1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var response = await _client.GetAsync($"/api/agentsessions/user/{userId}?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentSessionListResponseDto>();
        result.Should().NotBeNull();
        result!.Sessions.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task CreateAgentSession_ShouldReturnCreatedWhenSuccessful()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.NewGuid(),
            AIContextId = "thread_123"
        };

        var sessionId = Guid.NewGuid();
        var session = new AgentSession
        {
            Id = sessionId,
            UserId = request.UserId,
            MentorshipId = request.MentorshipId,
            AIContextId = request.AIContextId,
            Status = AgentSessionStatus.Active
        };

        _helper.MockAgentSessionService
            .Setup(x => x.CreateAgentSessionAsync(request.UserId, request.MentorshipId, request.AIContextId))
            .ReturnsAsync(session);

        // Act
        var response = await _client.PostAsJsonAsync("/api/agentsessions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AgentSessionResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.UserId.Should().Be(request.UserId);
        result.MentorshipId.Should().Be(request.MentorshipId);
        
        _helper.MockAgentSessionService.Verify(
            x => x.CreateAgentSessionAsync(request.UserId, request.MentorshipId, request.AIContextId),
            Times.Once);
    }

    [Fact]
    public async Task CreateAgentSession_ShouldReturnConflictWhenSessionExists()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.NewGuid()
        };

        _helper.MockAgentSessionService
            .Setup(x => x.CreateAgentSessionAsync(request.UserId, request.MentorshipId, request.AIContextId))
            .ThrowsAsync(new InvalidOperationException("Active session already exists"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/agentsessions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateAgentSession_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.Empty,
            MentorshipId = Guid.Empty
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("UserId", "User ID is required"));

        _helper.MockCreateAgentSessionValidator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateAgentSessionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var response = await _client.PostAsJsonAsync("/api/agentsessions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _helper.MockAgentSessionService.Verify(
            x => x.CreateAgentSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAgentSession_ShouldReturnUpdatedSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateAgentSessionRequestDto
        {
            Status = "Paused",
            AIContextId = "thread_updated",
            LastInteraction = DateTime.UtcNow
        };

        var updatedSession = new AgentSession
        {
            Id = sessionId,
            Status = AgentSessionStatus.Paused,
            AIContextId = request.AIContextId,
            LastInteraction = request.LastInteraction
        };

        _helper.MockAgentSessionService
            .Setup(x => x.UpdateAgentSessionAsync(
                sessionId,
                It.IsAny<AgentSessionStatus?>(),
                request.AIContextId,
                request.LastInteraction))
            .ReturnsAsync(updatedSession);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/agentsessions/{sessionId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AgentSessionResponseDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Paused");
        result.AIContextId.Should().Be(request.AIContextId);
        
        _helper.MockAgentSessionService.Verify(
            x => x.UpdateAgentSessionAsync(
                sessionId,
                It.IsAny<AgentSessionStatus?>(),
                request.AIContextId,
                request.LastInteraction),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAgentSession_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateAgentSessionRequestDto
        {
            Status = "Paused"
        };

        _helper.MockAgentSessionService
            .Setup(x => x.UpdateAgentSessionAsync(
                sessionId,
                It.IsAny<AgentSessionStatus?>(),
                request.AIContextId,
                request.LastInteraction))
            .ThrowsAsync(new InvalidOperationException("Session not found"));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/agentsessions/{sessionId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAgentSession_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateAgentSessionRequestDto();

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Status", "Invalid status"));

        _helper.MockUpdateAgentSessionValidator
            .Setup(x => x.ValidateAsync(It.IsAny<UpdateAgentSessionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/agentsessions/{sessionId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _helper.MockAgentSessionService.Verify(
            x => x.UpdateAgentSessionAsync(
                It.IsAny<Guid>(),
                It.IsAny<AgentSessionStatus?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>()),
            Times.Never);
    }

    [Fact]
    public async Task ExpireSession_ShouldReturnOkWhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.ExpireSessionAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        var response = await _client.PostAsync($"/api/agentsessions/{sessionId}/expire", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("message").GetString().Should().Contain("expired");
        
        _helper.MockAgentSessionService.Verify(
            x => x.ExpireSessionAsync(sessionId),
            Times.Once);
    }

    [Fact]
    public async Task ExpireSession_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.ExpireSessionAsync(sessionId))
            .ReturnsAsync(false);

        // Act
        var response = await _client.PostAsync($"/api/agentsessions/{sessionId}/expire", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PauseSession_ShouldReturnOkWhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.PauseSessionAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        var response = await _client.PostAsync($"/api/agentsessions/{sessionId}/pause", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("message").GetString().Should().Contain("paused");
        
        _helper.MockAgentSessionService.Verify(
            x => x.PauseSessionAsync(sessionId),
            Times.Once);
    }

    [Fact]
    public async Task PauseSession_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.PauseSessionAsync(sessionId))
            .ReturnsAsync(false);

        // Act
        var response = await _client.PostAsync($"/api/agentsessions/{sessionId}/pause", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResumeSession_ShouldReturnOkWhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.ResumeSessionAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        var response = await _client.PostAsync($"/api/agentsessions/{sessionId}/resume", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("message").GetString().Should().Contain("resumed");
        
        _helper.MockAgentSessionService.Verify(
            x => x.ResumeSessionAsync(sessionId),
            Times.Once);
    }

    [Fact]
    public async Task ResumeSession_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _helper.MockAgentSessionService
            .Setup(x => x.ResumeSessionAsync(sessionId))
            .ReturnsAsync(false);

        // Act
        var response = await _client.PostAsync($"/api/agentsessions/{sessionId}/resume", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _helper?.Dispose();
        _client?.Dispose();
    }
}

