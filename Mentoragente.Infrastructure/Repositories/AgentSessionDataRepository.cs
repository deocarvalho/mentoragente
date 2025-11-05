using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Interfaces;
using Supabase.Postgrest.Exceptions;

namespace Mentoragente.Infrastructure.Repositories;

public class AgentSessionDataRepository : IAgentSessionDataRepository
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<AgentSessionDataRepository> _logger;

    public AgentSessionDataRepository(IConfiguration configuration, ILogger<AgentSessionDataRepository> logger)
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

    public async Task<AgentSessionData?> GetAgentSessionDataAsync(Guid agentSessionId)
    {
        try
        {
            var response = await _supabaseClient
                .From<AgentSessionData>()
                .Select("*")
                .Filter("agent_session_id", Supabase.Postgrest.Constants.Operator.Equals, agentSessionId)
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving agent session data for {AgentSessionId}", agentSessionId);
            throw new InvalidOperationException($"Failed to retrieve agent session data: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving agent session data for {AgentSessionId}", agentSessionId);
            throw;
        }
    }

    public async Task<AgentSessionData> CreateAgentSessionDataAsync(AgentSessionData data)
    {
        try
        {
            data.CreatedAt = DateTime.UtcNow;
            data.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<AgentSessionData>()
                .Insert(data);

            var created = response.Models.FirstOrDefault() ?? data;
            _logger.LogInformation("Created agent session data for {AgentSessionId}", data.AgentSessionId);
            return created;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while creating agent session data");
            throw new InvalidOperationException($"Failed to create agent session data: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating agent session data");
            throw;
        }
    }

    public async Task<AgentSessionData> UpdateAgentSessionDataAsync(AgentSessionData data)
    {
        try
        {
            data.UpdatedAt = DateTime.UtcNow;

            await _supabaseClient
                .From<AgentSessionData>()
                .Update(data);

            _logger.LogInformation("Updated agent session data for {AgentSessionId}", data.AgentSessionId);
            return data;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while updating agent session data for {AgentSessionId}", data.AgentSessionId);
            throw new InvalidOperationException($"Failed to update agent session data: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating agent session data for {AgentSessionId}", data.AgentSessionId);
            throw;
        }
    }
}

