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
using Moq;

namespace Mentoragente.Tests.API.Integration;

public class MentorshipsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly IntegrationTestHelper _helper;
    private readonly HttpClient _client;

    public MentorshipsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _helper = new IntegrationTestHelper();
        _client = _helper.Client;
    }

    [Fact]
    public async Task GetMentorshipById_ShouldReturnMentorship()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship
        {
            Id = mentorshipId,
            Name = "Test Mentorship",
            MentorId = Guid.NewGuid(),
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            Status = MentorshipStatus.Active,
            WhatsAppProvider = WhatsAppProvider.ZApi,
            InstanceCode = "test_instance"
        };

        _helper.MockMentorshipService
            .Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // Act
        var response = await _client.GetAsync($"/api/mentorships/{mentorshipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MentorshipResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(mentorshipId);
        result.Name.Should().Be("Test Mentorship");
        result.AssistantId.Should().Be("asst_TEST123");
    }

    [Fact]
    public async Task GetMentorshipById_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _helper.MockMentorshipService
            .Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync((Mentorship?)null);

        // Act
        var response = await _client.GetAsync($"/api/mentorships/{mentorshipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMentorshipsByMentorId_ShouldReturnPagedResult()
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var mentorships = new List<Mentorship>
        {
            new Mentorship
            {
                Id = Guid.NewGuid(),
                Name = "Mentorship 1",
                MentorId = mentorId,
                Status = MentorshipStatus.Active
            },
            new Mentorship
            {
                Id = Guid.NewGuid(),
                Name = "Mentorship 2",
                MentorId = mentorId,
                Status = MentorshipStatus.Active
            }
        };
        var pagedResult = PagedResult<Mentorship>.Create(mentorships, 2, 1, 10);

        _helper.MockMentorshipService
            .Setup(x => x.GetMentorshipsByMentorIdAsync(mentorId, 1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var response = await _client.GetAsync($"/api/mentorships/mentor/{mentorId}?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MentorshipListResponseDto>();
        result.Should().NotBeNull();
        result!.Mentorships.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetActiveMentorships_ShouldReturnPagedResult()
    {
        // Arrange
        var mentorships = new List<Mentorship>
        {
            new Mentorship
            {
                Id = Guid.NewGuid(),
                Name = "Active Mentorship 1",
                Status = MentorshipStatus.Active
            },
            new Mentorship
            {
                Id = Guid.NewGuid(),
                Name = "Active Mentorship 2",
                Status = MentorshipStatus.Active
            }
        };
        var pagedResult = PagedResult<Mentorship>.Create(mentorships, 2, 1, 10);

        _helper.MockMentorshipService
            .Setup(x => x.GetActiveMentorshipsAsync(1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var response = await _client.GetAsync("/api/mentorships/active?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MentorshipListResponseDto>();
        result.Should().NotBeNull();
        result!.Mentorships.Should().HaveCount(2);
        result.Mentorships.All(m => m.Status == "Active").Should().BeTrue();
    }

    [Fact]
    public async Task CreateMentorship_ShouldReturnCreatedWhenSuccessful()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            Description = "Test description",
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
        };

        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship
        {
            Id = mentorshipId,
            Name = request.Name,
            MentorId = request.MentorId,
            AssistantId = request.AssistantId,
            DurationDays = request.DurationDays,
            Description = request.Description,
            WhatsAppProvider = WhatsAppProvider.ZApi,
            InstanceCode = request.InstanceCode,
            Status = MentorshipStatus.Active
        };

        _helper.MockMentorshipService
            .Setup(x => x.CreateMentorshipAsync(
                request.MentorId,
                request.Name,
                request.AssistantId,
                request.DurationDays,
                request.Description,
                It.IsAny<WhatsAppProvider?>(),
                request.InstanceCode))
            .ReturnsAsync(mentorship);

        // Act
        var response = await _client.PostAsJsonAsync("/api/mentorships", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<MentorshipResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(mentorshipId);
        result.Name.Should().Be(request.Name);
        
        _helper.MockMentorshipService.Verify(
            x => x.CreateMentorshipAsync(
                request.MentorId,
                request.Name,
                request.AssistantId,
                request.DurationDays,
                request.Description,
                It.IsAny<WhatsAppProvider?>(),
                request.InstanceCode),
            Times.Once);
    }

    [Fact]
    public async Task CreateMentorship_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.Empty,
            Name = ""
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Name", "Name is required"));

        _helper.MockCreateMentorshipValidator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateMentorshipRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var response = await _client.PostAsJsonAsync("/api/mentorships", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _helper.MockMentorshipService.Verify(
            x => x.CreateMentorshipAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<WhatsAppProvider?>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateMentorship_ShouldReturnNotFoundWhenMentorNotFound()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            InstanceCode = "test_instance"
        };

        _helper.MockMentorshipService
            .Setup(x => x.CreateMentorshipAsync(
                request.MentorId,
                request.Name,
                request.AssistantId,
                request.DurationDays,
                request.Description,
                It.IsAny<WhatsAppProvider?>(),
                request.InstanceCode))
            .ThrowsAsync(new InvalidOperationException("Mentor not found"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/mentorships", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateMentorship_ShouldReturnBadRequestWhenArgumentException()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            InstanceCode = "test_instance"
        };

        _helper.MockMentorshipService
            .Setup(x => x.CreateMentorshipAsync(
                request.MentorId,
                request.Name,
                request.AssistantId,
                request.DurationDays,
                request.Description,
                It.IsAny<WhatsAppProvider?>(),
                request.InstanceCode))
            .ThrowsAsync(new ArgumentException("Name is required"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/mentorships", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMentorship_ShouldReturnUpdatedMentorship()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var request = new UpdateMentorshipRequestDto
        {
            Name = "Updated Mentorship",
            Description = "Updated description"
        };

        var updatedMentorship = new Mentorship
        {
            Id = mentorshipId,
            Name = request.Name,
            Description = request.Description,
            Status = MentorshipStatus.Active
        };

        _helper.MockMentorshipService
            .Setup(x => x.UpdateMentorshipAsync(
                mentorshipId,
                request.Name,
                request.AssistantId,
                request.DurationDays,
                request.Description,
                null,
                It.IsAny<WhatsAppProvider?>(),
                request.InstanceCode))
            .ReturnsAsync(updatedMentorship);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/mentorships/{mentorshipId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MentorshipResponseDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        
        _helper.MockMentorshipService.Verify(
            x => x.UpdateMentorshipAsync(
                mentorshipId,
                request.Name,
                request.AssistantId,
                request.DurationDays,
                request.Description,
                null,
                It.IsAny<WhatsAppProvider?>(),
                request.InstanceCode),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMentorship_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var request = new UpdateMentorshipRequestDto
        {
            Name = "Updated Name"
        };

        _helper.MockMentorshipService
            .Setup(x => x.UpdateMentorshipAsync(
                mentorshipId,
                request.Name,
                request.AssistantId,
                request.DurationDays,
                request.Description,
                null,
                It.IsAny<WhatsAppProvider?>(),
                request.InstanceCode))
            .ThrowsAsync(new InvalidOperationException("Mentorship not found"));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/mentorships/{mentorshipId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateMentorship_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var request = new UpdateMentorshipRequestDto();

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Name", "Name cannot be empty"));

        _helper.MockUpdateMentorshipValidator
            .Setup(x => x.ValidateAsync(It.IsAny<UpdateMentorshipRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/mentorships/{mentorshipId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _helper.MockMentorshipService.Verify(
            x => x.UpdateMentorshipAsync(
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<MentorshipStatus?>(),
                It.IsAny<WhatsAppProvider?>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteMentorship_ShouldReturnNoContentWhenSuccessful()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _helper.MockMentorshipService
            .Setup(x => x.DeleteMentorshipAsync(mentorshipId))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync($"/api/mentorships/{mentorshipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        _helper.MockMentorshipService.Verify(
            x => x.DeleteMentorshipAsync(mentorshipId),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMentorship_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _helper.MockMentorshipService
            .Setup(x => x.DeleteMentorshipAsync(mentorshipId))
            .ReturnsAsync(false);

        // Act
        var response = await _client.DeleteAsync($"/api/mentorships/{mentorshipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _helper?.Dispose();
        _client?.Dispose();
    }
}

