using Mentoragente.Domain.Entities;

namespace Mentoragente.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByPhoneAsync(string phoneNumber);
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
}

