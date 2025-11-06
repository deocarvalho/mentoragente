using FluentAssertions;
using Xunit;
using Mentoragente.Application.Validators;
using Mentoragente.Domain.DTOs;

namespace Mentoragente.Tests.Application.Validators;

public class CreateMentorshipRequestValidatorTests
{
    private readonly CreateMentorshipRequestValidator _validator;

    public CreateMentorshipRequestValidatorTests()
    {
        _validator = new CreateMentorshipRequestValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            Description = "Test description",
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenMentorIdIsEmpty()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.Empty,
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MentorId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("a")]
    public void Validate_ShouldFail_WhenNameIsTooShort(string name)
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = name,
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceedsMaxLength()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = new string('a', 201),
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenAssistantIdIsEmpty(string assistantId)
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = assistantId,
            DurationDays = 30,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AssistantId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAssistantIdExceedsMaxLength()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = new string('a', 101),
            DurationDays = 30,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AssistantId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenDurationDaysIsInvalid(int days)
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = days,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DurationDays");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDurationDaysExceeds365()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 366,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DurationDays");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            Description = new string('a', 1001),
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenEvolutionApiKeyIsEmpty(string apiKey)
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            EvolutionApiKey = apiKey,
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EvolutionApiKey");
    }

    [Fact]
    public void Validate_ShouldFail_WhenEvolutionApiKeyExceedsMaxLength()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            EvolutionApiKey = new string('a', 501),
            EvolutionInstanceName = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EvolutionApiKey");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenEvolutionInstanceNameIsEmpty(string instanceName)
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = instanceName
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EvolutionInstanceName");
    }

    [Fact]
    public void Validate_ShouldFail_WhenEvolutionInstanceNameExceedsMaxLength()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            EvolutionApiKey = "test_api_key",
            EvolutionInstanceName = new string('a', 101)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EvolutionInstanceName");
    }
}

public class UpdateMentorshipRequestValidatorTests
{
    private readonly UpdateMentorshipRequestValidator _validator;

    public UpdateMentorshipRequestValidatorTests()
    {
        _validator = new UpdateMentorshipRequestValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new UpdateMentorshipRequestDto
        {
            Name = "Updated Mentorship",
            AssistantId = "asst_UPDATED",
            DurationDays = 60,
            Description = "Updated description",
            Status = "Active",
            EvolutionApiKey = "updated_key",
            EvolutionInstanceName = "updated_instance"
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
        var request = new UpdateMentorshipRequestDto();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Validate_ShouldFail_WhenNameIsTooShort(string name)
    {
        // Arrange
        var request = new UpdateMentorshipRequestDto { Name = name };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Inactive")]
    [InlineData("Archived")]
    public void Validate_ShouldPass_WhenStatusIsValid(string status)
    {
        // Arrange
        var request = new UpdateMentorshipRequestDto { Status = status };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStatusIsInvalid()
    {
        // Arrange
        var request = new UpdateMentorshipRequestDto { Status = "InvalidStatus" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }
}

