using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Interfaces;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace Mentoragente.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IConfiguration configuration, ILogger<UserRepository> logger)
    {
        var supabaseUrl = configuration["Supabase:Url"];
        var supabaseKey = configuration["Supabase:ServiceRoleKey"];

        if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey))
        {
            throw new InvalidOperationException("Supabase URL and ServiceRoleKey must be configured");
        }

        var options = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
        _logger = logger;
    }

    public async Task<User?> GetUserByPhoneAsync(string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogWarning("GetUserByPhoneAsync called with empty phone number");
                return null;
            }

            var response = await _supabaseClient
                .From<User>()
                .Select("*")
                .Filter("phone_number", Operator.Equals, phoneNumber)
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving user for {PhoneNumber}", phoneNumber);
            throw new InvalidOperationException($"Failed to retrieve user: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving user for {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<User>()
                .Select("*")
                .Filter("id", Operator.Equals, id.ToString())
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving user {UserId}", id);
            throw new InvalidOperationException($"Failed to retrieve user: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving user {UserId}", id);
            throw;
        }
    }

    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                throw new ArgumentException("Phone number cannot be empty", nameof(user));
            }

            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<User>()
                .Insert(user);

            var created = response.Models.FirstOrDefault() ?? user;
            _logger.LogInformation("Created user {UserId} with phone {PhoneNumber}", created.Id, user.PhoneNumber);
            return created;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while creating user for {PhoneNumber}", user.PhoneNumber);
            throw new InvalidOperationException($"Failed to create user: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating user for {PhoneNumber}", user.PhoneNumber);
            throw;
        }
    }

    public async Task<List<User>> GetAllUsersAsync(int skip = 0, int take = 10)
    {
        try
        {
            var response = await _supabaseClient
                .From<User>()
                .Select("*")
                .Order("created_at", Ordering.Descending)
                .Range(skip, skip + take - 1)
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving users");
            throw new InvalidOperationException($"Failed to retrieve users: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving users");
            throw;
        }
    }

    public async Task<int> GetTotalUsersCountAsync()
    {
        try
        {
            // Note: Supabase Postgrest doesn't support count directly in the same way
            // We'll get all users and count them, or use a raw query
            // For now, we'll get a limited set and estimate
            var response = await _supabaseClient
                .From<User>()
                .Select("*")
                .Get();

            return response.Models.Count;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while counting users");
            throw new InvalidOperationException($"Failed to count users: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while counting users");
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;

            await _supabaseClient
                .From<User>()
                .Update(user);

            _logger.LogInformation("Updated user {UserId}", user.Id);
            return user;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while updating user {UserId}", user.Id);
            throw new InvalidOperationException($"Failed to update user: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating user {UserId}", user.Id);
            throw;
        }
    }
}

