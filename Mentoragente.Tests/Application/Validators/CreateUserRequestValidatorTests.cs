using FluentAssertions;
using Xunit;
using Mentoragente.Application.Validators;
using Mentoragente.Domain.DTOs;

namespace Mentoragente.Tests.Application.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator;

    public CreateUserRequestValidatorTests()
    {
        _validator = new CreateUserRequestValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenEmailIsNull()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = "John Doe",
            Email = null
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenPhoneNumberIsEmpty(string? phoneNumber)
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = phoneNumber,
            Name = "John Doe"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Theory]
    [InlineData("123456789")] // Too short
    [InlineData("1234567890123456")] // Too long
    [InlineData("abc123456789")] // Contains letters
    [InlineData("12-345-6789")] // Contains dashes
    public void Validate_ShouldFail_WhenPhoneNumberIsInvalid(string phoneNumber)
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = phoneNumber,
            Name = "John Doe"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("a")]
    public void Validate_ShouldFail_WhenNameIsInvalid(string? name)
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = name
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
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = new string('a', 101)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    public void Validate_ShouldFail_WhenEmailIsInvalid(string email)
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = "John Doe",
            Email = email
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailExceedsMaxLength()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = "John Doe",
            Email = new string('a', 250) + "@example.com"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}

public class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _validator;

    public UpdateUserRequestValidatorTests()
    {
        _validator = new UpdateUserRequestValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new UpdateUserRequestDto
        {
            Name = "John Doe Updated",
            Email = "john.updated@example.com",
            Status = "Active"
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
        var request = new UpdateUserRequestDto();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Inactive")]
    [InlineData("Blocked")]
    public void Validate_ShouldPass_WhenStatusIsValid(string status)
    {
        // Arrange
        var request = new UpdateUserRequestDto { Status = status };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStatusIsInvalid()
    {
        // Arrange
        var request = new UpdateUserRequestDto { Status = "InvalidStatus" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsTooShort()
    {
        // Arrange
        var request = new UpdateUserRequestDto { Name = "a" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}

