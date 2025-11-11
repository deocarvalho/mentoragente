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

public class MentorshipsControllerTests
{
    private readonly Mock<IMentorshipService> _mockMentorshipService;
    private readonly Mock<ILogger<MentorshipsController>> _mockLogger;
    private readonly Mock<IValidator<CreateMentorshipRequestDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateMentorshipRequestDto>> _mockUpdateValidator;
    private readonly MentorshipsController _controller;

    public MentorshipsControllerTests()
    {
        _mockMentorshipService = new Mock<IMentorshipService>();
        _mockLogger = new Mock<ILogger<MentorshipsController>>();
        _mockCreateValidator = new Mock<IValidator<CreateMentorshipRequestDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateMentorshipRequestDto>>();

        _controller = new MentorshipsController(
            _mockMentorshipService.Object,
            _mockLogger.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object);
    }

    [Fact]
    public async Task GetMentorshipsByMentorId_ShouldReturnPagedResult()
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
        var mentorships = new List<Mentorship>
        {
            new Mentorship { Id = Guid.NewGuid(), Name = "Mentorship 1", MentorId = mentorId }
        };
        var pagedResult = PagedResult<Mentorship>.Create(mentorships, 1, 1, 10);

        _mockMentorshipService.Setup(x => x.GetMentorshipsByMentorIdAsync(mentorId, 1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetMentorshipsByMentorId(mentorId, pagination);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateMentorship_ShouldReturnCreatedWhenSuccessful()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_123",
            DurationDays = 30,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var mentorship = new Mentorship
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            MentorId = request.MentorId,
            AssistantId = request.AssistantId,
            DurationDays = request.DurationDays,
            EvolutionApiKey = request.EvolutionApiKey,
            EvolutionInstanceName = request.EvolutionInstanceName
        };

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.CreateMentorshipAsync(
            request.MentorId, request.Name, request.AssistantId, request.DurationDays, 
            request.Description, request.EvolutionApiKey, request.EvolutionInstanceName))
            .ReturnsAsync(mentorship);

        // Act
        var result = await _controller.CreateMentorship(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task DeleteMentorship_ShouldReturnNoContentWhenSuccessful()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _mockMentorshipService.Setup(x => x.DeleteMentorshipAsync(mentorshipId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteMentorship(mentorshipId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetMentorshipById_ShouldReturnMentorship()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship { Id = mentorshipId, Name = "Test Mentorship" };

        _mockMentorshipService.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // Act
        var result = await _controller.GetMentorshipById(mentorshipId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMentorshipById_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _mockMentorshipService.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync((Mentorship?)null);

        // Act
        var result = await _controller.GetMentorshipById(mentorshipId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetActiveMentorships_ShouldReturnPagedResult()
    {
        // Arrange
        var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
        var mentorships = new List<Mentorship>
        {
            new Mentorship { Id = Guid.NewGuid(), Status = MentorshipStatus.Active }
        };
        var pagedResult = PagedResult<Mentorship>.Create(mentorships, 1, 1, 10);

        _mockMentorshipService.Setup(x => x.GetActiveMentorshipsAsync(1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetActiveMentorships(pagination);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateMentorship_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto();
        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Name", "Name is required"));

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.CreateMentorship(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateMentorship_ShouldThrowExceptionWhenMentorNotFound()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test",
            AssistantId = "asst_123",
            DurationDays = 30,
            EvolutionApiKey = "api_key",
            EvolutionInstanceName = "instance"
        };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.CreateMentorshipAsync(
            request.MentorId, request.Name, request.AssistantId, request.DurationDays,
            request.Description, request.EvolutionApiKey, request.EvolutionInstanceName))
            .ThrowsAsync(new InvalidOperationException("Mentor not found"));

        // Act & Assert
        // Exception handling is now done by GlobalExceptionHandlingMiddleware
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.CreateMentorship(request));
    }

    [Fact]
    public async Task CreateMentorship_ShouldThrowExceptionWhenArgumentException()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "",
            AssistantId = "asst_123",
            DurationDays = 30,
            EvolutionApiKey = "api_key",
            EvolutionInstanceName = "instance"
        };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.CreateMentorshipAsync(
            request.MentorId, request.Name, request.AssistantId, request.DurationDays,
            request.Description, request.EvolutionApiKey, request.EvolutionInstanceName))
            .ThrowsAsync(new ArgumentException("Name is required"));

        // Act & Assert
        // Exception handling is now done by GlobalExceptionHandlingMiddleware
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _controller.CreateMentorship(request));
    }

    [Fact]
    public async Task UpdateMentorship_ShouldReturnUpdatedMentorship()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var request = new UpdateMentorshipRequestDto { Name = "Updated Name" };
        var mentorship = new Mentorship { Id = mentorshipId, Name = "Updated Name" };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockUpdateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.UpdateMentorshipAsync(
            mentorshipId, request.Name, request.AssistantId, request.DurationDays,
            request.Description, null, request.EvolutionApiKey, request.EvolutionInstanceName))
            .ReturnsAsync(mentorship);

        // Act
        var result = await _controller.UpdateMentorship(mentorshipId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateMentorship_ShouldThrowExceptionWhenNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var request = new UpdateMentorshipRequestDto();
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockUpdateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.UpdateMentorshipAsync(
            mentorshipId, request.Name, request.AssistantId, request.DurationDays,
            request.Description, null, request.EvolutionApiKey, request.EvolutionInstanceName))
            .ThrowsAsync(new InvalidOperationException("Mentorship not found"));

        // Act & Assert
        // Exception handling is now done by GlobalExceptionHandlingMiddleware
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.UpdateMentorship(mentorshipId, request));
    }

    [Fact]
    public async Task DeleteMentorship_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _mockMentorshipService.Setup(x => x.DeleteMentorshipAsync(mentorshipId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteMentorship(mentorshipId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}

