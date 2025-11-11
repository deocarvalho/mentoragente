using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Mentoragente.API;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Application.Models;
using System.Net;
using System.Net.Http.Json;
using Moq;

namespace Mentoragente.Tests.API.Integration;

public class UsersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly IntegrationTestHelper _helper;
    private readonly HttpClient _client;

    public UsersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _helper = new IntegrationTestHelper();
        _client = _helper.Client;
    }

    [Fact]
    public async Task GetUsers_ShouldReturnPagedResult()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                PhoneNumber = "5511999999999",
                Name = "User 1",
                Status = UserStatus.Active
            },
            new User
            {
                Id = Guid.NewGuid(),
                PhoneNumber = "5511888888888",
                Name = "User 2",
                Status = UserStatus.Active
            }
        };
        var pagedResult = PagedResult<User>.Create(users, 2, 1, 10);

        _helper.MockUserService
            .Setup(x => x.GetUsersAsync(1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var response = await _client.GetAsync("/api/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserListResponseDto>();
        result.Should().NotBeNull();
        result!.Users.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PhoneNumber = "5511999999999",
            Name = "Test User",
            Email = "test@example.com",
            Status = UserStatus.Active
        };

        _helper.MockUserService
            .Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be("Test User");
        result.PhoneNumber.Should().Be("5511999999999");
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _helper.MockUserService
            .Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserByPhone_ShouldReturnUser()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var user = new User
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            Name = "Test User",
            Status = UserStatus.Active
        };

        _helper.MockUserService
            .Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        // Act
        var response = await _client.GetAsync($"/api/users/phone/{phoneNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        result.Should().NotBeNull();
        result!.PhoneNumber.Should().Be(phoneNumber);
    }

    [Fact]
    public async Task GetUserByPhone_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var phoneNumber = "5511999999999";

        _helper.MockUserService
            .Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync((User?)null);

        // Act
        var response = await _client.GetAsync($"/api/users/phone/{phoneNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PhoneNumber = request.PhoneNumber,
            Name = request.Name,
            Email = request.Email,
            Status = UserStatus.Active
        };

        _helper.MockUserService
            .Setup(x => x.CreateUserAsync(request.PhoneNumber, request.Name, request.Email))
            .ReturnsAsync(user);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be(request.Name);
        result.PhoneNumber.Should().Be(request.PhoneNumber);
        
        _helper.MockUserService.Verify(
            x => x.CreateUserAsync(request.PhoneNumber, request.Name, request.Email),
            Times.Once);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "",
            Name = ""
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("PhoneNumber", "Phone number is required"));

        _helper.MockCreateUserValidator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateUserRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _helper.MockUserService.Verify(
            x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnConflictWhenUserExists()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            PhoneNumber = "5511999999999",
            Name = "Test User"
        };

        _helper.MockUserService
            .Setup(x => x.CreateUserAsync(request.PhoneNumber, request.Name, request.Email))
            .ThrowsAsync(new InvalidOperationException("User already exists"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequestDto
        {
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        var updatedUser = new User
        {
            Id = userId,
            PhoneNumber = "5511999999999",
            Name = request.Name,
            Email = request.Email,
            Status = UserStatus.Active
        };

        _helper.MockUserService
            .Setup(x => x.UpdateUserAsync(userId, request.Name, request.Email, null))
            .ReturnsAsync(updatedUser);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{userId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);
        result.Email.Should().Be(request.Email);
        
        _helper.MockUserService.Verify(
            x => x.UpdateUserAsync(userId, request.Name, request.Email, null),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequestDto
        {
            Name = "Updated Name"
        };

        _helper.MockUserService
            .Setup(x => x.UpdateUserAsync(userId, request.Name, request.Email, null))
            .ThrowsAsync(new InvalidOperationException($"User with ID {userId} not found"));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{userId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnBadRequestWhenValidationFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequestDto();

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Name", "Name cannot be empty"));

        _helper.MockUpdateUserValidator
            .Setup(x => x.ValidateAsync(It.IsAny<UpdateUserRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{userId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _helper.MockUserService.Verify(
            x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserStatus?>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContentWhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _helper.MockUserService
            .Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync($"/api/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        _helper.MockUserService.Verify(
            x => x.DeleteUserAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFoundWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _helper.MockUserService
            .Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(false);

        // Act
        var response = await _client.DeleteAsync($"/api/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _helper?.Dispose();
        _client?.Dispose();
    }
}

