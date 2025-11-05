using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
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
    public async Task GetUsersAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), PhoneNumber = "5511999999999", Name = "User 1" },
            new User { Id = Guid.NewGuid(), PhoneNumber = "5511888888888", Name = "User 2" }
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
        result.Page.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowWhenPhoneNumberExists()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var existingUser = new User { Id = Guid.NewGuid(), PhoneNumber = phoneNumber };

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await _userService.Invoking(s => s.CreateUserAsync(phoneNumber, "Test User"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*User with phone number {phoneNumber} already exists*");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var name = "Test User";

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        // Act
        var result = await _userService.CreateUserAsync(phoneNumber, name);

        // Assert
        result.Should().NotBeNull();
        result.PhoneNumber.Should().Be(phoneNumber);
        result.Name.Should().Be(name);
        result.Status.Should().Be(UserStatus.Active);
        _mockUserRepository.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowWhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _userService.Invoking(s => s.UpdateUserAsync(userId, "New Name"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*User with ID {userId} not found*");
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateUserSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Old Name", Status = UserStatus.Active };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _userService.UpdateUserAsync(userId, "New Name");

        // Assert
        result.Name.Should().Be("New Name");
        _mockUserRepository.Verify(x => x.UpdateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnFalseWhenUserNotFound()
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

    [Fact]
    public async Task DeleteUserAsync_ShouldSoftDeleteUser()
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
}

