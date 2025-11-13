using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Application.Models;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Tests.Application.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_mockUserRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Test User" };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserByPhoneAsync_ShouldReturnUser()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var user = new User { PhoneNumber = phoneNumber, Name = "Test User" };

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByPhoneAsync(phoneNumber);

        // Assert
        result.Should().NotBeNull();
        result!.PhoneNumber.Should().Be(phoneNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task GetUserByPhoneAsync_ShouldReturnNullWhenPhoneNumberIsEmpty(string? phoneNumber)
    {
        // Act
        var result = await _userService.GetUserByPhoneAsync(phoneNumber);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "User 1" },
            new User { Id = Guid.NewGuid(), Name = "User 2" }
        };

        _mockUserRepository.Setup(x => x.GetAllUsersAsync(0, pageSize))
            .ReturnsAsync(users);
        _mockUserRepository.Setup(x => x.GetTotalUsersCountAsync())
            .ReturnsAsync(2);

        // Act
        var result = await _userService.GetUsersAsync(page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var name = "Test User";
        var email = "test@example.com";

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _userService.CreateUserAsync(phoneNumber, name, email);

        // Assert
        result.Should().NotBeNull();
        result.PhoneNumber.Should().Be(phoneNumber);
        result.Name.Should().Be(name);
        result.Email.Should().Be(email);
        result.Status.Should().Be(UserStatus.Active);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateUserAsync_ShouldThrowWhenPhoneNumberIsEmpty(string? phoneNumber)
    {
        // Act & Assert
        await _userService.Invoking(s => s.CreateUserAsync(phoneNumber, "Test", null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Phone number is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateUserAsync_ShouldThrowWhenNameIsEmpty(string? name)
    {
        // Act & Assert
        await _userService.Invoking(s => s.CreateUserAsync("5511999999999", name, null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Name is required*");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowWhenUserExists()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var existingUser = new User { Id = Guid.NewGuid(), PhoneNumber = phoneNumber };

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await _userService.Invoking(s => s.CreateUserAsync(phoneNumber, "Test", null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*User with phone number {phoneNumber} already exists*");
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Old Name" };
        var newName = "New Name";

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _userService.UpdateUserAsync(userId, name: newName);

        // Assert
        result.Name.Should().Be(newName);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "old@example.com" };
        var newEmail = "new@example.com";

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _userService.UpdateUserAsync(userId, email: newEmail);

        // Assert
        result.Email.Should().Be(newEmail);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Status = UserStatus.Active };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _userService.UpdateUserAsync(userId, status: UserStatus.Blocked);

        // Assert
        result.Status.Should().Be(UserStatus.Blocked);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowWhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _userService.Invoking(s => s.UpdateUserAsync(userId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*User with ID {userId} not found*");
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldSoftDelete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Status = UserStatus.Active };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mockUserRepository.Verify(x => x.UpdateUserAsync(
            It.Is<User>(u => u.Status == UserStatus.Inactive)), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnFalseWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeFalse();
    }
}
