using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Interfaces;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace Mentoragente.Infrastructure.Repositories;

public class MentoriaRepository : IMentoriaRepository
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<MentoriaRepository> _logger;

    public MentoriaRepository(IConfiguration configuration, ILogger<MentoriaRepository> logger)
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

    public async Task<Mentoria?> GetMentoriaByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentoria>()
                .Select("*")
                .Filter("id", Operator.Equals, id)
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving mentoria {MentoriaId}", id);
            throw new InvalidOperationException($"Failed to retrieve mentoria: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving mentoria {MentoriaId}", id);
            throw;
        }
    }

    public async Task<List<Mentoria>> GetMentoriasByMentorIdAsync(Guid mentorId)
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentoria>()
                .Select("*")
                .Filter("mentor_id", Operator.Equals, mentorId)
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving mentorias for mentor {MentorId}", mentorId);
            throw new InvalidOperationException($"Failed to retrieve mentorias: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving mentorias for mentor {MentorId}", mentorId);
            throw;
        }
    }

    public async Task<List<Mentoria>> GetAllMentoriasAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<Mentoria>()
                .Select("*")
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving all mentorias");
            throw new InvalidOperationException($"Failed to retrieve mentorias: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving all mentorias");
            throw;
        }
    }

    public async Task<Mentoria> CreateMentoriaAsync(Mentoria mentoria)
    {
        try
        {
            mentoria.Id = Guid.NewGuid();
            mentoria.CreatedAt = DateTime.UtcNow;
            mentoria.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<Mentoria>()
                .Insert(mentoria);

            var created = response.Models.FirstOrDefault() ?? mentoria;
            _logger.LogInformation("Created mentoria {MentoriaId} - {Nome}", created.Id, mentoria.Nome);
            return created;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while creating mentoria");
            throw new InvalidOperationException($"Failed to create mentoria: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating mentoria");
            throw;
        }
    }

    public async Task<Mentoria> UpdateMentoriaAsync(Mentoria mentoria)
    {
        try
        {
            mentoria.UpdatedAt = DateTime.UtcNow;

            await _supabaseClient
                .From<Mentoria>()
                .Update(mentoria);

            _logger.LogInformation("Updated mentoria {MentoriaId}", mentoria.Id);
            return mentoria;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while updating mentoria {MentoriaId}", mentoria.Id);
            throw new InvalidOperationException($"Failed to update mentoria: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating mentoria {MentoriaId}", mentoria.Id);
            throw;
        }
    }
}

