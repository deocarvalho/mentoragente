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

public class UsersControllerAdditionalTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly Mock<IValidator<CreateUserRequestDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateUserRequestDto>> _mockUpdateValidator;
    private readonly UsersController _controller;

    public UsersControllerAdditionalTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _mockCreateValidator = new Mock<IValidator<CreateUserRequestDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateUserRequestDto>>();

        _controller = new UsersController(
            _mockUserService.Object,
            _mockLogger.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnPagedResult()
    {
        // Arrange
        var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), PhoneNumber = "5511999999999", Name = "User 1" }
        };
        var pagedResult = PagedResult<User>.Create(users, 1, 1, 10);

        _mockUserService.Setup(x => x.GetUsersAsync(1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetUsers(pagination);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as UserListResponseDto;
        response.Should().NotBeNull();
        response!.Users.Should().HaveCount(1);
        response.Total.Should().Be(1);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var request = new CreateUserRequestDto { PhoneNumber = "", Name = "" };
        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("PhoneNumber", "Phone number is required"));

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateUser_ShouldThrowExceptionWhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequestDto { Name = "New Name" };
        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Clear();

        _mockUpdateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockUserService.Setup(x => x.UpdateUserAsync(userId, "New Name", null, null))
            .ThrowsAsync(new InvalidOperationException($"User with ID {userId} not found"));

        // Act & Assert
        // Exception handling is now done by GlobalExceptionHandlingMiddleware
        // In unit tests, the exception will bubble up (integration tests verify middleware handles it)
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.UpdateUser(userId, request));
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Test User" };

        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUserByPhone_ShouldReturnUser()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var user = new User { PhoneNumber = phoneNumber, Name = "Test User" };

        _mockUserService.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUserByPhone(phoneNumber);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserByPhone_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var phoneNumber = "5511999999999";

        _mockUserService.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetUserByPhone(phoneNumber);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreatedWhenSuccessful()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = "Test User",
            Email = "test@example.com"
        };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var user = new User { Id = Guid.NewGuid(), PhoneNumber = request.PhoneNumber, Name = request.Name };

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockUserService.Setup(x => x.CreateUserAsync(request.PhoneNumber, request.Name, request.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateUser_ShouldThrowExceptionWhenUserExists()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = "Test User"
        };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockUserService.Setup(x => x.CreateUserAsync(request.PhoneNumber, request.Name, request.Email))
            .ThrowsAsync(new InvalidOperationException("User already exists"));

        // Act & Assert
        // Exception handling is now done by GlobalExceptionHandlingMiddleware
        // In unit tests, the exception will bubble up (integration tests verify middleware handles it)
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.CreateUser(request));
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequestDto { Name = "Updated Name" };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var user = new User { Id = userId, Name = "Updated Name" };

        _mockUpdateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockUserService.Setup(x => x.UpdateUserAsync(userId, request.Name, request.Email, null))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContentWhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService.Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService.Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}

