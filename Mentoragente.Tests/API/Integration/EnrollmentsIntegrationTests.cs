using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Mentoragente.API;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using Moq;

namespace Mentoragente.Tests.API.Integration;

public class EnrollmentsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly IntegrationTestHelper _helper;
    private readonly HttpClient _client;

    public EnrollmentsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _helper = new IntegrationTestHelper();
        _client = _helper.Client;
    }

    [Fact]
    public async Task CreateEnrollment_ShouldReturnOkWhenSuccessful()
    {
        // Arrange
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511999999999",
            MentorshipId = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com"
        };

        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PhoneNumber = request.PhoneNumber,
            Name = request.Name,
            Email = request.Email,
            Status = UserStatus.Active
        };

        var session = new AgentSession
        {
            Id = sessionId,
            UserId = userId,
            MentorshipId = request.MentorshipId,
            Status = AgentSessionStatus.Active
        };

        _helper.MockUserService
            .Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber))
            .ReturnsAsync((User?)null);

        _helper.MockUserService
            .Setup(x => x.CreateUserAsync(request.PhoneNumber, request.Name, request.Email))
            .ReturnsAsync(user);

        _helper.MockAgentSessionService
            .Setup(x => x.CreateAgentSessionAsync(userId, request.MentorshipId, null))
            .ReturnsAsync(session);

        _helper.MockMessageProcessor
            .Setup(x => x.SendWelcomeMessageAsync(request.PhoneNumber, request.MentorshipId, request.Name))
            .ReturnsAsync(true);

        // Act
        var response = await _client.PostAsJsonAsync("/api/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EnrollmentResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        result.WelcomeMessageSent.Should().BeTrue();

        _helper.MockUserService.Verify(x => x.CreateUserAsync(request.PhoneNumber, request.Name, request.Email), Times.Once);
        _helper.MockAgentSessionService.Verify(x => x.CreateAgentSessionAsync(userId, request.MentorshipId, null), Times.Once);
        _helper.MockMessageProcessor.Verify(x => x.SendWelcomeMessageAsync(request.PhoneNumber, request.MentorshipId, request.Name), Times.Once);
    }

    [Fact]
    public async Task CreateEnrollment_ShouldUseExistingUserWhenFound()
    {
        // Arrange
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511999999999",
            MentorshipId = Guid.NewGuid(),
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = userId,
            PhoneNumber = request.PhoneNumber,
            Name = "Existing User",
            Status = UserStatus.Active
        };

        var updatedUser = new User
        {
            Id = userId,
            PhoneNumber = request.PhoneNumber,
            Name = request.Name,
            Email = request.Email,
            Status = UserStatus.Active
        };

        var session = new AgentSession
        {
            Id = sessionId,
            UserId = userId,
            MentorshipId = request.MentorshipId,
            Status = AgentSessionStatus.Active
        };

        _helper.MockUserService
            .Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber))
            .ReturnsAsync(existingUser);

        _helper.MockUserService
            .Setup(x => x.UpdateUserAsync(userId, request.Name, request.Email, null))
            .ReturnsAsync(updatedUser);

        _helper.MockAgentSessionService
            .Setup(x => x.CreateAgentSessionAsync(userId, request.MentorshipId, null))
            .ReturnsAsync(session);

        _helper.MockMessageProcessor
            .Setup(x => x.SendWelcomeMessageAsync(request.PhoneNumber, request.MentorshipId, request.Name))
            .ReturnsAsync(true);

        // Act
        var response = await _client.PostAsJsonAsync("/api/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EnrollmentResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        _helper.MockUserService.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _helper.MockUserService.Verify(x => x.UpdateUserAsync(userId, request.Name, request.Email, null), Times.Once);
    }

    [Fact]
    public async Task CreateEnrollment_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "invalid",
            MentorshipId = Guid.NewGuid()
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("PhoneNumber", "Invalid phone number"));

        _helper.MockEnrollmentValidator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateEnrollmentRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var response = await _client.PostAsJsonAsync("/api/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _helper.MockUserService.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateEnrollment_ShouldHandleWelcomeMessageFailure()
    {
        // Arrange
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511999999999",
            MentorshipId = Guid.NewGuid(),
            Name = "Test User"
        };

        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PhoneNumber = request.PhoneNumber,
            Name = request.Name,
            Status = UserStatus.Active
        };

        var session = new AgentSession
        {
            Id = sessionId,
            UserId = userId,
            MentorshipId = request.MentorshipId,
            Status = AgentSessionStatus.Active
        };

        _helper.MockUserService
            .Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber))
            .ReturnsAsync((User?)null);

        _helper.MockUserService
            .Setup(x => x.CreateUserAsync(request.PhoneNumber, request.Name, null))
            .ReturnsAsync(user);

        _helper.MockAgentSessionService
            .Setup(x => x.CreateAgentSessionAsync(userId, request.MentorshipId, null))
            .ReturnsAsync(session);

        _helper.MockMessageProcessor
            .Setup(x => x.SendWelcomeMessageAsync(request.PhoneNumber, request.MentorshipId, request.Name))
            .ReturnsAsync(false);

        // Act
        var response = await _client.PostAsJsonAsync("/api/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EnrollmentResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.WelcomeMessageSent.Should().BeFalse();
        result.Message.Should().Contain("welcome message could not be sent");
    }

    public void Dispose()
    {
        _helper?.Dispose();
        _client?.Dispose();
    }
}

