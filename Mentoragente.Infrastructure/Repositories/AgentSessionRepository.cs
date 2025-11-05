using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace Mentoragente.Infrastructure.Repositories;

public class AgentSessionRepository : IAgentSessionRepository
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<AgentSessionRepository> _logger;

    public AgentSessionRepository(IConfiguration configuration, ILogger<AgentSessionRepository> logger)
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

    public async Task<AgentSession?> GetAgentSessionAsync(Guid userId, Guid mentoriaId)
    {
        try
        {
            var response = await _supabaseClient
                .From<AgentSession>()
                .Select("*")
                .Filter("user_id", Operator.Equals, userId)
                .Filter("mentoria_id", Operator.Equals, mentoriaId)
                .Order("created_at", Ordering.Descending)
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving agent session for user {UserId} and mentoria {MentoriaId}", userId, mentoriaId);
            throw new InvalidOperationException($"Failed to retrieve agent session: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving agent session for user {UserId} and mentoria {MentoriaId}", userId, mentoriaId);
            throw;
        }
    }

    public async Task<AgentSession?> GetActiveAgentSessionAsync(Guid userId, Guid mentoriaId)
    {
        try
        {
            var response = await _supabaseClient
                .From<AgentSession>()
                .Select("*")
                .Filter("user_id", Operator.Equals, userId)
                .Filter("mentoria_id", Operator.Equals, mentoriaId)
                .Filter("status", Operator.Equals, AgentSessionStatus.Active)
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving active agent session for user {UserId} and mentoria {MentoriaId}", userId, mentoriaId);
            throw new InvalidOperationException($"Failed to retrieve active agent session: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving active agent session for user {UserId} and mentoria {MentoriaId}", userId, mentoriaId);
            throw;
        }
    }

    public async Task<AgentSession?> GetAgentSessionByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<AgentSession>()
                .Select("*")
                .Filter("id", Operator.Equals, id)
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving agent session {AgentSessionId}", id);
            throw new InvalidOperationException($"Failed to retrieve agent session: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving agent session {AgentSessionId}", id);
            throw;
        }
    }

    public async Task<List<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<AgentSession>()
                .Select("*")
                .Filter("user_id", Operator.Equals, userId)
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving agent sessions for user {UserId}", userId);
            throw new InvalidOperationException($"Failed to retrieve agent sessions: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving agent sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<AgentSession>> GetAgentSessionsByUserIdAsync(Guid userId, int skip, int take)
    {
        try
        {
            var response = await _supabaseClient
                .From<AgentSession>()
                .Select("*")
                .Filter("user_id", Operator.Equals, userId)
                .Order("created_at", Ordering.Descending)
                .Range(skip, skip + take - 1)
                .Get();

            return response.Models;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving paginated agent sessions for user {UserId}", userId);
            throw new InvalidOperationException($"Failed to retrieve agent sessions: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving paginated agent sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetAgentSessionsCountByUserIdAsync(Guid userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<AgentSession>()
                .Select("*")
                .Filter("user_id", Operator.Equals, userId)
                .Get();

            return response.Models.Count;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while counting agent sessions for user {UserId}", userId);
            throw new InvalidOperationException($"Failed to count agent sessions: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while counting agent sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<AgentSession> CreateAgentSessionAsync(AgentSession session)
    {
        try
        {
            session.Id = Guid.NewGuid();
            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<AgentSession>()
                .Insert(session);

            var created = response.Models.FirstOrDefault() ?? session;
            _logger.LogInformation("Created agent session {AgentSessionId} for user {UserId} and mentoria {MentoriaId}", created.Id, session.UserId, session.MentoriaId);
            return created;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while creating agent session");
            throw new InvalidOperationException($"Failed to create agent session: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating agent session");
            throw;
        }
    }

    public async Task<AgentSession> UpdateAgentSessionAsync(AgentSession session)
    {
        try
        {
            session.UpdatedAt = DateTime.UtcNow;

            await _supabaseClient
                .From<AgentSession>()
                .Update(session);

            _logger.LogInformation("Updated agent session {AgentSessionId}", session.Id);
            return session;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while updating agent session {AgentSessionId}", session.Id);
            throw new InvalidOperationException($"Failed to update agent session: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating agent session {AgentSessionId}", session.Id);
            throw;
        }
    }
}

