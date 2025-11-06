using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace Mentoragente.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<ConversationRepository> _logger;

    public ConversationRepository(IConfiguration configuration, ILogger<ConversationRepository> logger)
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

    public async Task<List<ChatMessage>> GetConversationHistoryAsync(Guid agentSessionId)
    {
        try
        {
            var response = await _supabaseClient
                .From<Conversation>()
                .Select("sender, message, created_at")
                .Filter("agent_session_id", Operator.Equals, agentSessionId.ToString())
                .Order("created_at", Ordering.Descending)
                .Limit(20)
                .Get();

            var messages = response.Models
                .Select(c => new ChatMessage
                {
                    Role = c.Sender == "user" ? "user" : "assistant",
                    Content = c.Message,
                    CreatedAt = c.CreatedAt
                })
                .Reverse() // Ordenar cronologicamente
                .ToList();

            _logger.LogDebug("Retrieved {Count} messages from history for agent session {AgentSessionId}", messages.Count, agentSessionId);
            return messages;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving conversation history for {AgentSessionId}", agentSessionId);
            throw new InvalidOperationException($"Failed to retrieve conversation history: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving conversation history for {AgentSessionId}", agentSessionId);
            throw;
        }
    }

    public async Task AddMessageAsync(Guid agentSessionId, string role, string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content cannot be empty", nameof(content));
            }

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                AgentSessionId = agentSessionId,
                Sender = role == "user" ? "user" : "assistant",
                Message = content,
                MessageType = "text",
                CreatedAt = DateTime.UtcNow
            };

            await _supabaseClient
                .From<Conversation>()
                .Insert(conversation);

            _logger.LogInformation("Added message to conversation for agent session {AgentSessionId}", agentSessionId);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while adding message for {AgentSessionId}", agentSessionId);
            throw new InvalidOperationException($"Failed to add message: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding message for {AgentSessionId}", agentSessionId);
            throw;
        }
    }

    public async Task ClearConversationAsync(Guid agentSessionId)
    {
        try
        {
            await _supabaseClient
                .From<Conversation>()
                .Filter("agent_session_id", Operator.Equals, agentSessionId.ToString())
                .Delete();

            _logger.LogInformation("Cleared conversation for agent session {AgentSessionId}", agentSessionId);
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while clearing conversation for {AgentSessionId}", agentSessionId);
            throw new InvalidOperationException($"Failed to clear conversation: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while clearing conversation for {AgentSessionId}", agentSessionId);
            throw;
        }
    }

    public async Task<string> GetConversationThreadIdAsync(Guid agentSessionId)
    {
        try
        {
            var sessionResponse = await _supabaseClient
                .From<AgentSession>()
                .Select("ai_context_id")
                .Filter("id", Operator.Equals, agentSessionId.ToString())
                .Get();

            var session = sessionResponse.Models.FirstOrDefault();
            return session?.AIContextId ?? string.Empty;
        }
        catch (PostgrestException ex)
        {
            _logger.LogError(ex, "Postgrest error while retrieving thread ID for {AgentSessionId}", agentSessionId);
            throw new InvalidOperationException($"Failed to retrieve thread ID: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving thread ID for {AgentSessionId}", agentSessionId);
            throw;
        }
    }
}

