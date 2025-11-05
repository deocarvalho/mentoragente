using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User?> GetUserByPhoneAsync(string phoneNumber);
    Task<User> CreateUserAsync(string phoneNumber, string name, string? email = null);
    Task<User> UpdateUserAsync(Guid id, string? name = null, string? email = null, UserStatus? status = null);
    Task<bool> DeleteUserAsync(Guid id);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting user by ID: {UserId}", id);
        return await _userRepository.GetUserByIdAsync(id);
    }

    public async Task<User?> GetUserByPhoneAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            _logger.LogWarning("GetUserByPhoneAsync called with empty phone number");
            return null;
        }

        _logger.LogInformation("Getting user by phone: {PhoneNumber}", phoneNumber);
        return await _userRepository.GetUserByPhoneAsync(phoneNumber);
    }

    public async Task<User> CreateUserAsync(string phoneNumber, string name, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        // Verificar se já existe usuário com esse telefone
        var existingUser = await _userRepository.GetUserByPhoneAsync(phoneNumber);
        if (existingUser != null)
        {
            _logger.LogWarning("User with phone {PhoneNumber} already exists: {UserId}", phoneNumber, existingUser.Id);
            throw new InvalidOperationException($"User with phone number {phoneNumber} already exists");
        }

        var user = new User
        {
            PhoneNumber = phoneNumber,
            Name = name,
            Email = email,
            Status = UserStatus.Active
        };

        _logger.LogInformation("Creating new user: {PhoneNumber}, {Name}", phoneNumber, name);
        return await _userRepository.CreateUserAsync(user);
    }

    public async Task<User> UpdateUserAsync(Guid id, string? name = null, string? email = null, UserStatus? status = null)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            _logger.LogError("User {UserId} not found for update", id);
            throw new InvalidOperationException($"User with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(name))
            user.Name = name;

        if (email != null) // Permite remover email também
            user.Email = email;

        if (status.HasValue)
            user.Status = status.Value;

        _logger.LogInformation("Updating user {UserId}", id);
        return await _userRepository.UpdateUserAsync(user);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for deletion", id);
            return false;
        }

        // Soft delete: marcar como Inactive
        user.Status = UserStatus.Inactive;
        await _userRepository.UpdateUserAsync(user);
        
        _logger.LogInformation("Soft deleted user {UserId}", id);
        return true;
    }
}

