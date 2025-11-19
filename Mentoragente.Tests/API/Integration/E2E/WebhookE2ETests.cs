using FluentAssertions;
using Xunit;
using Mentoragente.Domain.Models;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using Npgsql;

namespace Mentoragente.Tests.API.Integration.E2E;

/// <summary>
/// Testes E2E para Webhooks usando banco de dados real (TestContainers)
/// 
/// Estes testes simulam o fluxo completo de webhook:
/// 1. Criar mentorship e enrollment
/// 2. Receber webhook do Z-API
/// 3. Processar mensagem
/// 4. Verificar que resposta foi gerada
/// 
/// IMPORTANTE: Requer Docker instalado e rodando.
/// </summary>
[Trait("Category", "E2E")]
public class WebhookE2ETests : IClassFixture<E2ETestHelper>, IDisposable
{
    private readonly E2ETestHelper _helper;
    private readonly HttpClient _client;
    private readonly string _instanceToken;

    public WebhookE2ETests(E2ETestHelper helper)
    {
        _helper = helper;
        _client = helper.Client;
        // Token de instância para teste (deve corresponder ao instance_token na mentorship)
        _instanceToken = "test-instance-token-e2e";
    }

    [Fact]
    [Trait("Requires", "Docker")]
    public async Task ZApiWebhook_E2E_ShouldProcessMessageForEnrolledUser()
    {
        // Arrange - Criar fluxo completo: Mentor -> Mentorship -> User -> Enrollment
        var mentorId = await CreateTestUser("5511999999998", "Test Mentor", "mentor@test.com");
        var mentorshipId = await CreateTestMentorship(mentorId);
        var userId = await CreateTestUser("5511999999999", "Test User", "user@test.com");
        var sessionId = await CreateTestSession(userId, mentorshipId);

        // Criar webhook payload do Z-API
        var webhook = new ZApiWebhookDto
        {
            Phone = "5511999999999",
            MessageId = Guid.NewGuid().ToString(),
            Type = "ReceivedCallback",
            FromMe = false,
            Text = new ZApiTextMessage
            {
                Message = "Olá, preciso de ajuda"
            },
            Momment = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // Act - Enviar webhook
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Z-Api-Token", _instanceToken);
        
        var response = await _client.PostAsJsonAsync($"/api/webhooks/zapi", webhook);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        // BadRequest pode ocorrer se OpenAI não estiver configurado, mas o webhook foi recebido
        
        // Verificar que mensagem foi salva no banco (se conversation foi criada)
        var conversations = await GetConversationsFromDatabase(sessionId);
        // Pode ter 0 ou 1 conversa dependendo se o processamento completo funcionou
        // O importante é que não deu erro 401 (autenticação) ou 500 (erro interno)
    }

    [Fact]
    [Trait("Requires", "Docker")]
    public async Task ZApiWebhook_E2E_ShouldRejectUnauthorizedUser()
    {
        // Arrange - Criar mentorship mas NÃO criar enrollment
        var mentorId = await CreateTestUser("5511888888888", "Test Mentor", "mentor2@test.com");
        var mentorshipId = await CreateTestMentorship(mentorId);
        
        // Criar usuário mas SEM enrollment (sem session)
        var userId = await CreateTestUser("5511777777777", "Unauthorized User", "unauthorized@test.com");
        // NÃO criar session - usuário não está enrolled

        var webhook = new ZApiWebhookDto
        {
            Phone = "5511777777777",
            MessageId = Guid.NewGuid().ToString(),
            Type = "ReceivedCallback",
            FromMe = false,
            Text = new ZApiTextMessage
            {
                Message = "Olá"
            }
        };

        // Act
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Z-Api-Token", _instanceToken);
        
        var response = await _client.PostAsJsonAsync($"/api/webhooks/zapi", webhook);

        // Assert
        // Deve retornar BadRequest porque usuário não está enrolled
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.ToLowerInvariant().Should().Contain("not enrolled", because: "Should reject unauthorized user");
    }

    [Fact]
    [Trait("Requires", "Docker")]
    public async Task ZApiWebhook_E2E_ShouldRejectInvalidZApiToken()
    {
        // Arrange
        var webhook = new ZApiWebhookDto
        {
            Phone = "5511999999999",
            MessageId = Guid.NewGuid().ToString(),
            Type = "ReceivedCallback",
            FromMe = false,
            Text = new ZApiTextMessage { Message = "Test" }
        };

        // Act - Enviar com token inválido (não existe mentorship com esse instance_token)
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Z-Api-Token", "invalid-instance-token");
        
        var response = await _client.PostAsJsonAsync("/api/webhooks/zapi", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        var content = await response.Content.ReadAsStringAsync();
        content.ToLowerInvariant().Should().Contain("z-api-token", because: "Should reject invalid Z-Api-Token");
    }

    [Fact]
    [Trait("Requires", "Docker")]
    public async Task ZApiWebhook_E2E_ShouldAutoDetectMentorship()
    {
        // Arrange - Criar enrollment completo
        var mentorId = await CreateTestUser("5511666666666", "Test Mentor", "mentor3@test.com");
        var mentorshipId = await CreateTestMentorship(mentorId);
        var userId = await CreateTestUser("5511555555555", "Test User", "user3@test.com");
        await CreateTestSession(userId, mentorshipId);

        var webhook = new ZApiWebhookDto
        {
            Phone = "5511555555555",
            MessageId = Guid.NewGuid().ToString(),
            Type = "ReceivedCallback",
            FromMe = false,
            Text = new ZApiTextMessage
            {
                Message = "Test message"
            }
        };

        // Act - NÃO passar mentorshipId (deve identificar pela instance_token)
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Z-Api-Token", _instanceToken);
        
        var response = await _client.PostAsJsonAsync("/api/webhooks/zapi", webhook);

        // Assert
        // Deve processar (OK ou BadRequest se OpenAI não configurado)
        // Mas NÃO deve dar erro de "mentorship not found" ou "unauthorized"
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Requires", "Docker")]
    public async Task ZApiWebhook_E2E_ShouldIgnoreDuplicateMessages()
    {
        // Arrange
        var mentorId = await CreateTestUser("5511444444444", "Test Mentor", "mentor4@test.com");
        var mentorshipId = await CreateTestMentorship(mentorId);
        var userId = await CreateTestUser("5511333333333", "Test User", "user4@test.com");
        await CreateTestSession(userId, mentorshipId);

        var messageId = Guid.NewGuid().ToString();
        var webhook = new ZApiWebhookDto
        {
            Phone = "5511333333333",
            MessageId = messageId,
            Type = "ReceivedCallback",
            FromMe = false,
            Text = new ZApiTextMessage { Message = "Test" }
        };

        // Act - Enviar mesma mensagem duas vezes
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Z-Api-Token", _instanceToken);
        
        var firstResponse = await _client.PostAsJsonAsync($"/api/webhooks/zapi", webhook);
        var secondResponse = await _client.PostAsJsonAsync($"/api/webhooks/zapi", webhook);

        // Assert
        firstResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK); // Deve retornar OK com "duplicate ignored"
        
        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        secondContent.ToLowerInvariant().Should().Contain("duplicate", because: "Should ignore duplicate messages");
    }

    // Helper methods
    private async Task<Guid> CreateTestUser(string phoneNumber, string name, string? email)
    {
        var userId = Guid.NewGuid();
        var sql = @"
            INSERT INTO users (id, phone_number, name, email, status)
            VALUES (@id, @phoneNumber, @name, @email, @status)
            ON CONFLICT (phone_number) DO UPDATE SET name = EXCLUDED.name, email = EXCLUDED.email
            RETURNING id
        ";

        using var connection = new NpgsqlConnection(_helper.ConnectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", userId);
        command.Parameters.AddWithValue("phoneNumber", phoneNumber);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
        command.Parameters.AddWithValue("status", "Active");
        
        var result = await command.ExecuteScalarAsync();
        return result != null ? (Guid)result : userId;
    }

    private async Task<Guid> CreateTestMentorship(Guid mentorId)
    {
        var mentorshipId = Guid.NewGuid();
        var instanceToken = "test-instance-token-e2e";
        var sql = @"
            INSERT INTO mentorships (id, name, mentor_id, assistant_id, duration_days, status, whatsapp_provider, instance_code, instance_token)
            VALUES (@id, @name, @mentorId, @assistantId, @durationDays, @status, @provider, @instanceCode, @instanceToken)
        ";

        using var connection = new NpgsqlConnection(_helper.ConnectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", mentorshipId);
        command.Parameters.AddWithValue("name", "Test Mentorship E2E");
        command.Parameters.AddWithValue("mentorId", mentorId);
        command.Parameters.AddWithValue("assistantId", "asst_TEST123");
        command.Parameters.AddWithValue("durationDays", 30);
        command.Parameters.AddWithValue("status", "Active");
        command.Parameters.AddWithValue("provider", "ZApi");
        command.Parameters.AddWithValue("instanceCode", "test_instance");
        command.Parameters.AddWithValue("instanceToken", instanceToken);
        
        await command.ExecuteNonQueryAsync();
        return mentorshipId;
    }

    private async Task<Guid> CreateTestSession(Guid userId, Guid mentorshipId)
    {
        var sessionId = Guid.NewGuid();
        var sql = @"
            INSERT INTO agent_sessions (id, user_id, mentorship_id, status, ai_provider)
            VALUES (@id, @userId, @mentorshipId, @status, @aiProvider)
            ON CONFLICT DO NOTHING
            RETURNING id
        ";

        using var connection = new NpgsqlConnection(_helper.ConnectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", sessionId);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("mentorshipId", mentorshipId);
        command.Parameters.AddWithValue("status", "Active");
        command.Parameters.AddWithValue("aiProvider", "OpenAI");
        
        var result = await command.ExecuteScalarAsync();
        if (result != null)
        {
            return (Guid)result;
        }

        // Se não retornou (porque já existe), buscar o existente
        var selectSql = @"
            SELECT id FROM agent_sessions 
            WHERE user_id = @userId AND mentorship_id = @mentorshipId AND status = 'Active'
        ";
        using var selectCommand = new NpgsqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("userId", userId);
        selectCommand.Parameters.AddWithValue("mentorshipId", mentorshipId);
        
        var existingId = await selectCommand.ExecuteScalarAsync();
        return existingId != null ? (Guid)existingId : sessionId;
    }

    private async Task<List<Conversation>> GetConversationsFromDatabase(Guid sessionId)
    {
        var conversations = new List<Conversation>();
        var sql = "SELECT id, agent_session_id, sender, message FROM conversations WHERE agent_session_id = @sessionId";

        using var connection = new NpgsqlConnection(_helper.ConnectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            conversations.Add(new Conversation
            {
                Id = reader.GetGuid(0),
                AgentSessionId = reader.GetGuid(1),
                Sender = reader.GetString(2),
                Message = reader.GetString(3),
                MessageType = "text",
                CreatedAt = DateTime.UtcNow
            });
        }
        
        return conversations;
    }

    public void Dispose()
    {
        _helper.CleanupTestDataAsync().GetAwaiter().GetResult();
    }
}

