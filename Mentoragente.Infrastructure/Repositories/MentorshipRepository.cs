using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Interfaces;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace Mentoragente.Infrastructure.Repositories;

public class MentorshipRepository : IMentorshipRepository
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<MentorshipRepository> _logger;

    public MentorshipRepository(IConfiguration configuration, ILogger<MentorshipRepository> logger)
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

    public async Task<Mentorship?> GetMentorshipByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentorship>()
                .Select("*")
                .Filter("id", Operator.Equals, id.ToString())
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving mentorship {MentorshipId}", id);
            throw new InvalidOperationException($"Failed to retrieve mentorship: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving mentorship {MentorshipId}", id);
            throw;
        }
    }

    public async Task<List<Mentorship>> GetMentorshipsByMentorIdAsync(Guid mentorId)
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentorship>()
                .Select("*")
                .Filter("mentor_id", Operator.Equals, mentorId.ToString())
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving mentorships for mentor {MentorId}", mentorId);
            throw new InvalidOperationException($"Failed to retrieve mentorships: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving mentorships for mentor {MentorId}", mentorId);
            throw;
        }
    }

    public async Task<List<Mentorship>> GetMentorshipsByMentorIdAsync(Guid mentorId, int skip, int take)
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentorship>()
                .Select("*")
                .Filter("mentor_id", Operator.Equals, mentorId.ToString())
                .Order("created_at", Ordering.Descending)
                .Range(skip, skip + take - 1)
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving paginated mentorships for mentor {MentorId}", mentorId);
            throw new InvalidOperationException($"Failed to retrieve mentorships: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving paginated mentorships for mentor {MentorId}", mentorId);
            throw;
        }
    }

    public async Task<int> GetMentorshipsCountByMentorIdAsync(Guid mentorId)
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentorship>()
                .Select("*")
                .Filter("mentor_id", Operator.Equals, mentorId.ToString())
                .Get();

            return response.Models.Count;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while counting mentorships for mentor {MentorId}", mentorId);
            throw new InvalidOperationException($"Failed to count mentorships: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while counting mentorships for mentor {MentorId}", mentorId);
            throw;
        }
    }

    public async Task<List<Mentorship>> GetAllMentorshipsAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentorship>()
                .Select("*")
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving all mentorships");
            throw new InvalidOperationException($"Failed to retrieve mentorships: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving all mentorships");
            throw;
        }
    }

    public async Task<List<Mentorship>> GetActiveMentorshipsAsync(int skip, int take)
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentorship>()
                .Select("*")
                .Filter("status", Operator.Equals, "Active")
                .Order("created_at", Ordering.Descending)
                .Range(skip, skip + take - 1)
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving active mentorships");
            throw new InvalidOperationException($"Failed to retrieve active mentorships: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving active mentorships");
            throw;
        }
    }

    public async Task<int> GetActiveMentorshipsCountAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentorship>()
                .Select("*")
                .Filter("status", Operator.Equals, "Active")
                .Get();

            return response.Models.Count;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while counting active mentorships");
            throw new InvalidOperationException($"Failed to count active mentorships: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while counting active mentorships");
            throw;
        }
    }

    public async Task<Mentorship> CreateMentorshipAsync(Mentorship mentorship)
    {
        try
        {
            mentorship.Id = Guid.NewGuid();
            mentorship.CreatedAt = DateTime.UtcNow;
            mentorship.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<Mentorship>()
                .Insert(mentorship);

            var created = response.Models.FirstOrDefault() ?? mentorship;
            _logger.LogInformation("Created mentorship {MentorshipId} - {Name}", created.Id, mentorship.Name);
            return created;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while creating mentorship");
            throw new InvalidOperationException($"Failed to create mentorship: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating mentorship");
            throw;
        }
    }

    public async Task<Mentorship> UpdateMentorshipAsync(Mentorship mentorship)
    {
        try
        {
            mentorship.UpdatedAt = DateTime.UtcNow;

            await _supabaseClient
                .From<Mentorship>()
                .Update(mentorship);

            _logger.LogInformation("Updated mentorship {MentorshipId}", mentorship.Id);
            return mentorship;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while updating mentorship {MentorshipId}", mentorship.Id);
            throw new InvalidOperationException($"Failed to update mentorship: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating mentorship {MentorshipId}", mentorship.Id);
            throw;
        }
    }
}

