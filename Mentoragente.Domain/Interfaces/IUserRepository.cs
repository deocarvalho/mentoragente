using Mentoragente.Domain.Entities;

namespace Mentoragente.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByPhoneAsync(string phoneNumber);
    Task<User?> GetUserByIdAsync(Guid id);
    Task<List<User>> GetAllUsersAsync(int skip = 0, int take = 10);
    Task<int> GetTotalUsersCountAsync();
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
}

