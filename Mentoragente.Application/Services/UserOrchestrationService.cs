using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IUserOrchestrationService
{
    Task<User> GetOrCreateUserAsync(string phoneNumber);
}

public class UserOrchestrationService : IUserOrchestrationService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserOrchestrationService> _logger;

    public UserOrchestrationService(
        IUserRepository userRepository,
        ILogger<UserOrchestrationService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserAsync(string phoneNumber)
    {
        var user = await _userRepository.GetUserByPhoneAsync(phoneNumber);
        
        if (user != null)
            return user;

        user = new User
        {
            PhoneNumber = phoneNumber,
            Name = "WhatsApp Client",
            Status = UserStatus.Active
        };

        user = await _userRepository.CreateUserAsync(user);
        _logger.LogInformation("Created new user {UserId} for phone {PhoneNumber}", user.Id, phoneNumber);
        
        return user;
    }
}

