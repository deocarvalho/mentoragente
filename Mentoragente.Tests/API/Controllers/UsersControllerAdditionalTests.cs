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
    public async Task UpdateUser_ShouldReturnNotFoundWhenUserNotFound()
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

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}

