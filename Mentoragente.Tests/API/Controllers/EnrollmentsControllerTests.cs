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
using FluentValidation;

namespace Mentoragente.Tests.API.Controllers;

public class EnrollmentsControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IAgentSessionService> _mockAgentSessionService;
    private readonly Mock<IMessageProcessor> _mockMessageProcessor;
    private readonly Mock<IMentorshipService> _mockMentorshipService;
    private readonly Mock<ILogger<EnrollmentsController>> _mockLogger;
    private readonly Mock<IValidator<CreateEnrollmentRequestDto>> _mockValidator;
    private readonly Mock<ILogSanitizer> _mockLogSanitizer;
    private readonly EnrollmentsController _controller;

    public EnrollmentsControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockAgentSessionService = new Mock<IAgentSessionService>();
        _mockMessageProcessor = new Mock<IMessageProcessor>();
        _mockMentorshipService = new Mock<IMentorshipService>();
        _mockLogger = new Mock<ILogger<EnrollmentsController>>();
        _mockValidator = new Mock<IValidator<CreateEnrollmentRequestDto>>();
        _mockLogSanitizer = new Mock<ILogSanitizer>();
        
        // Setup default log sanitizer behavior (return input as-is for tests)
        _mockLogSanitizer.Setup(x => x.MaskPhoneNumber(It.IsAny<string>())).Returns<string?>(s => s ?? "***");
        _mockLogSanitizer.Setup(x => x.MaskToken(It.IsAny<string>())).Returns<string?>(s => s ?? "***");
        _mockLogSanitizer.Setup(x => x.SanitizeMessageText(It.IsAny<string>(), It.IsAny<int>())).Returns<string?, int>((s, _) => s ?? "***");
        _mockLogSanitizer.Setup(x => x.SanitizeJsonPayload(It.IsAny<string>())).Returns<string?>(s => s ?? "{}");

        _controller = new EnrollmentsController(
            _mockUserService.Object,
            _mockAgentSessionService.Object,
            _mockMessageProcessor.Object,
            _mockMentorshipService.Object,
            _mockLogger.Object,
            _mockValidator.Object,
            _mockLogSanitizer.Object);
    }

    [Fact]
    public async Task CreateEnrollment_ShouldReturnCreatedWhenSuccessful()
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
        var user = new User { Id = userId, PhoneNumber = request.PhoneNumber, Name = request.Name };
        var session = new AgentSession { Id = sessionId, UserId = userId, MentorshipId = request.MentorshipId };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var mentorship = new Mentorship 
        { 
            Id = request.MentorshipId, 
            Status = MentorshipStatus.Active,
            DurationDays = 30
        };

        _mockValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.GetMentorshipByIdAsync(request.MentorshipId))
            .ReturnsAsync(mentorship);

        _mockUserService.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber))
            .ReturnsAsync((User?)null);

        _mockUserService.Setup(x => x.CreateUserAsync(request.PhoneNumber, request.Name, null))
            .ReturnsAsync(user);

        _mockAgentSessionService.Setup(x => x.CreateAgentSessionAsync(userId, request.MentorshipId, null))
            .ReturnsAsync(session);

        _mockMessageProcessor.Setup(x => x.SendWelcomeMessageAsync(request.PhoneNumber, request.MentorshipId, request.Name))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CreateEnrollment(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _mockUserService.Verify(x => x.CreateUserAsync(request.PhoneNumber, request.Name, null), Times.Once);
        _mockAgentSessionService.Verify(x => x.CreateAgentSessionAsync(userId, request.MentorshipId, null), Times.Once);
        _mockMessageProcessor.Verify(x => x.SendWelcomeMessageAsync(request.PhoneNumber, request.MentorshipId, request.Name), Times.Once);
    }

    [Fact]
    public async Task CreateEnrollment_ShouldUseExistingUserWhenFound()
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
        var user = new User { Id = userId, PhoneNumber = request.PhoneNumber, Name = "Existing User" };
        var session = new AgentSession { Id = sessionId, UserId = userId, MentorshipId = request.MentorshipId };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var mentorship = new Mentorship 
        { 
            Id = request.MentorshipId, 
            Status = MentorshipStatus.Active,
            DurationDays = 30
        };

        _mockValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.GetMentorshipByIdAsync(request.MentorshipId))
            .ReturnsAsync(mentorship);

        _mockUserService.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber))
            .ReturnsAsync(user);

        var updatedUser = new User { Id = userId, PhoneNumber = request.PhoneNumber, Name = request.Name };
        _mockUserService.Setup(x => x.UpdateUserAsync(userId, request.Name, request.Email, null))
            .ReturnsAsync(updatedUser);

        _mockAgentSessionService.Setup(x => x.CreateAgentSessionAsync(userId, request.MentorshipId, null))
            .ReturnsAsync(session);

        _mockMessageProcessor.Setup(x => x.SendWelcomeMessageAsync(request.PhoneNumber, request.MentorshipId, request.Name))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CreateEnrollment(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

        _mockValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.CreateEnrollment(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
        var user = new User { Id = userId, PhoneNumber = request.PhoneNumber };
        var session = new AgentSession { Id = sessionId, UserId = userId, MentorshipId = request.MentorshipId };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var mentorship = new Mentorship 
        { 
            Id = request.MentorshipId, 
            Status = MentorshipStatus.Active,
            DurationDays = 30
        };

        _mockValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.GetMentorshipByIdAsync(request.MentorshipId))
            .ReturnsAsync(mentorship);

        _mockUserService.Setup(x => x.GetUserByPhoneAsync(request.PhoneNumber))
            .ReturnsAsync((User?)null);

        _mockUserService.Setup(x => x.CreateUserAsync(request.PhoneNumber, request.Name, null))
            .ReturnsAsync(user);

        _mockAgentSessionService.Setup(x => x.CreateAgentSessionAsync(userId, request.MentorshipId, null))
            .ReturnsAsync(session);

        _mockMessageProcessor.Setup(x => x.SendWelcomeMessageAsync(request.PhoneNumber, request.MentorshipId, request.Name))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateEnrollment(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send welcome message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateEnrollment_ShouldReturnNotFoundWhenMentorshipDoesNotExist()
    {
        // Arrange
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511999999999",
            MentorshipId = Guid.NewGuid(),
            Name = "Test User"
        };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.GetMentorshipByIdAsync(request.MentorshipId))
            .ReturnsAsync((Mentorship?)null);

        // Act
        var result = await _controller.CreateEnrollment(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockAgentSessionService.Verify(x => x.CreateAgentSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateEnrollment_ShouldReturnBadRequestWhenMentorshipIsNotActive()
    {
        // Arrange
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511999999999",
            MentorshipId = Guid.NewGuid(),
            Name = "Test User"
        };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var mentorship = new Mentorship 
        { 
            Id = request.MentorshipId, 
            Status = MentorshipStatus.Inactive,
            DurationDays = 30
        };

        _mockValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentorshipService.Setup(x => x.GetMentorshipByIdAsync(request.MentorshipId))
            .ReturnsAsync(mentorship);

        // Act
        var result = await _controller.CreateEnrollment(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockAgentSessionService.Verify(x => x.CreateAgentSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }
}

