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
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
    public void Validate_ShouldFail_WhenAssistantIdIsEmpty(string? assistantId)
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = assistantId,
            DurationDays = 30,
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
            WhatsAppProvider = "ZApi",
            InstanceCode = "test_instance"
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
    public void Validate_ShouldFail_WhenInstanceCodeIsEmpty(string? instanceCode)
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            InstanceCode = instanceCode
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InstanceCode");
    }

    [Fact]
    public void Validate_ShouldFail_WhenInstanceCodeExceedsMaxLength()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            InstanceCode = new string('a', 101)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InstanceCode");
    }

    [Theory]
    [InlineData("EvolutionAPI")]
    [InlineData("ZApi")]
    [InlineData("OfficialWhatsApp")]
    public void Validate_ShouldPass_WhenWhatsAppProviderIsValid(string provider)
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            WhatsAppProvider = provider,
            InstanceCode = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenWhatsAppProviderIsInvalid()
    {
        // Arrange
        var request = new CreateMentorshipRequestDto
        {
            MentorId = Guid.NewGuid(),
            Name = "Test Mentorship",
            AssistantId = "asst_TEST123",
            DurationDays = 30,
            WhatsAppProvider = "InvalidProvider",
            InstanceCode = "test_instance"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WhatsAppProvider");
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
            WhatsAppProvider = "ZApi",
            InstanceCode = "updated_instance"
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

