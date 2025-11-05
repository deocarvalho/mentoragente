using FluentAssertions;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Xunit;

namespace Mentoragente.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void User_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().NotBeEmpty();
        user.PhoneNumber.Should().BeEmpty();
        user.Name.Should().BeEmpty();
        user.Email.Should().BeNull();
        user.Status.Should().Be(UserStatus.Active);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var phoneNumber = "5511999999999";
        var name = "João Silva";
        var email = "joao@example.com";
        var status = UserStatus.Active;

        // Act
        var user = new User
        {
            Id = id,
            PhoneNumber = phoneNumber,
            Name = name,
            Email = email,
            Status = status
        };

        // Assert
        user.Id.Should().Be(id);
        user.PhoneNumber.Should().Be(phoneNumber);
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.Status.Should().Be(status);
    }

    [Fact]
    public void User_ShouldAllowNullEmail()
    {
        // Arrange & Act
        var user = new User
        {
            PhoneNumber = "5511999999999",
            Name = "Maria",
            Email = null
        };

        // Assert
        user.Email.Should().BeNull();
    }
}

