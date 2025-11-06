using FluentAssertions;
using Xunit;
using Mentoragente.Application.Validators;
using Mentoragente.Domain.DTOs;

namespace Mentoragente.Tests.Application.Validators;

public class CreateAgentSessionRequestValidatorTests
{
    private readonly CreateAgentSessionRequestValidator _validator;

    public CreateAgentSessionRequestValidatorTests()
    {
        _validator = new CreateAgentSessionRequestValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.NewGuid(),
            AIContextId = "thread_ABC123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAIContextIdIsNull()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.NewGuid(),
            AIContextId = null
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.Empty,
            MentorshipId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenMentorshipIdIsEmpty()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.Empty
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MentorshipId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAIContextIdExceedsMaxLength()
    {
        // Arrange
        var request = new CreateAgentSessionRequestDto
        {
            UserId = Guid.NewGuid(),
            MentorshipId = Guid.NewGuid(),
            AIContextId = new string('a', 201)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AIContextId");
    }
}

public class UpdateAgentSessionRequestValidatorTests
{
    private readonly UpdateAgentSessionRequestValidator _validator;

    public UpdateAgentSessionRequestValidatorTests()
    {
        _validator = new UpdateAgentSessionRequestValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new UpdateAgentSessionRequestDto
        {
            Status = "Active",
            AIContextId = "thread_ABC123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreNull()
    {
        // Arrange
        var request = new UpdateAgentSessionRequestDto();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Expired")]
    [InlineData("Paused")]
    [InlineData("Completed")]
    public void Validate_ShouldPass_WhenStatusIsValid(string status)
    {
        // Arrange
        var request = new UpdateAgentSessionRequestDto { Status = status };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStatusIsInvalid()
    {
        // Arrange
        var request = new UpdateAgentSessionRequestDto { Status = "InvalidStatus" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAIContextIdExceedsMaxLength()
    {
        // Arrange
        var request = new UpdateAgentSessionRequestDto
        {
            AIContextId = new string('a', 201)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AIContextId");
    }
}

