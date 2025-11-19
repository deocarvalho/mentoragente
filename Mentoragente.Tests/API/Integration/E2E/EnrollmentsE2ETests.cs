using FluentAssertions;
using Xunit;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using Npgsql;

namespace Mentoragente.Tests.API.Integration.E2E;

/// <summary>
/// Testes E2E para Enrollment usando banco de dados real (TestContainers)
/// 
/// IMPORTANTE: Estes testes requerem Docker instalado e rodando.
/// Eles são mais lentos que testes com mocks, mas testam integração real.
/// 
/// Para executar:
/// 1. Certifique-se de que Docker está rodando
/// 2. Execute: dotnet test --filter "Category=E2E"
/// </summary>
[Trait("Category", "E2E")]
public class EnrollmentsE2ETests : IClassFixture<E2ETestHelper>, IDisposable
{
    private readonly E2ETestHelper _helper;
    private readonly HttpClient _client;

    public EnrollmentsE2ETests(E2ETestHelper helper)
    {
        _helper = helper;
        _client = helper.Client;
    }

    [Fact]
    [Trait("Requires", "Docker")]
    public async Task CreateEnrollment_E2E_ShouldCreateUserAndSessionInDatabase()
    {
        // Arrange
        var mentorshipId = await CreateTestMentorship();
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511999999999",
            MentorshipId = mentorshipId,
            Name = "Test User E2E",
            Email = "teste2e@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EnrollmentResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.SessionId.Should().NotBeEmpty();

        // Verificar no banco real
        var user = await GetUserFromDatabase(request.PhoneNumber);
        user.Should().NotBeNull();
        user!.Name.Should().Be(request.Name);
        user.PhoneNumber.Should().Be(request.PhoneNumber);
        user.Email.Should().Be(request.Email);

        var session = await GetSessionFromDatabase(user.Id, mentorshipId);
        session.Should().NotBeNull();
        session!.Status.Should().Be(AgentSessionStatus.Active);
        session.UserId.Should().Be(user.Id);
        session.MentorshipId.Should().Be(mentorshipId);
    }

    [Fact]
    [Trait("Requires", "Docker")]
    public async Task CreateEnrollment_E2E_ShouldUpdateExistingUser()
    {
        // Arrange - Criar usuário primeiro
        var existingUser = await CreateTestUser("5511888888888", "Old Name", "old@example.com");
        
        var mentorshipId = await CreateTestMentorship();
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511888888888",
            MentorshipId = mentorshipId,
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verificar que usuário foi atualizado
        var user = await GetUserFromDatabase(request.PhoneNumber);
        user.Should().NotBeNull();
        user!.Name.Should().Be("Updated Name");
        user.Email.Should().Be("updated@example.com");
    }

    [Fact]
    [Trait("Requires", "Docker")]
    public async Task CreateEnrollment_E2E_ShouldPreventDuplicateActiveSessions()
    {
        // Arrange
        var mentorshipId = await CreateTestMentorship();
        var userId = await CreateTestUser("5511777777777", "Test User", null);
        
        // Criar primeira sessão
        var firstRequest = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511777777777",
            MentorshipId = mentorshipId,
            Name = "Test User"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/enrollments", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Tentar criar segunda sessão (deve retornar Conflict ou usar existente)
        var secondRequest = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511777777777",
            MentorshipId = mentorshipId,
            Name = "Test User"
        };

        // Act
        var secondResponse = await _client.PostAsJsonAsync("/api/enrollments", secondRequest);

        // Assert
        // Pode retornar OK (usando sessão existente) ou Conflict
        secondResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);

        // Verificar que há apenas uma sessão ativa
        var activeSessions = await GetActiveSessionsFromDatabase(userId, mentorshipId);
        activeSessions.Should().HaveCount(1);
    }

    // Helper methods para interagir com o banco real
    private async Task<Guid> CreateTestMentorship()
    {
        // Criar mentor primeiro
        var mentorId = await CreateTestUser("5511999999998", "Test Mentor", "mentor@test.com");
        
        // Criar mentorship no banco
        var mentorshipId = Guid.NewGuid();
        var sql = @"
            INSERT INTO mentorships (id, name, mentor_id, assistant_id, duration_days, status, whatsapp_provider, instance_code)
            VALUES (@id, @name, @mentorId, @assistantId, @durationDays, @status, @provider, @instanceCode)
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
        
        await command.ExecuteNonQueryAsync();
        
        return mentorshipId;
    }

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

    private async Task<User?> GetUserFromDatabase(string phoneNumber)
    {
        var sql = "SELECT id, phone_number, name, email, status FROM users WHERE phone_number = @phoneNumber";

        using var connection = new NpgsqlConnection(_helper.ConnectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("phoneNumber", phoneNumber);
        
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetGuid(0),
                PhoneNumber = reader.GetString(1),
                Name = reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                Status = Enum.Parse<UserStatus>(reader.GetString(4))
            };
        }
        
        return null;
    }

    private async Task<AgentSession?> GetSessionFromDatabase(Guid userId, Guid mentorshipId)
    {
        var sql = @"
            SELECT id, user_id, mentorship_id, status 
            FROM agent_sessions 
            WHERE user_id = @userId AND mentorship_id = @mentorshipId AND status = 'Active'
        ";

        using var connection = new NpgsqlConnection(_helper.ConnectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("mentorshipId", mentorshipId);
        
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AgentSession
            {
                Id = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                MentorshipId = reader.GetGuid(2),
                Status = Enum.Parse<AgentSessionStatus>(reader.GetString(3))
            };
        }
        
        return null;
    }

    private async Task<List<AgentSession>> GetActiveSessionsFromDatabase(Guid userId, Guid mentorshipId)
    {
        var sessions = new List<AgentSession>();
        var sql = @"
            SELECT id, user_id, mentorship_id, status 
            FROM agent_sessions 
            WHERE user_id = @userId AND mentorship_id = @mentorshipId AND status = 'Active'
        ";

        using var connection = new NpgsqlConnection(_helper.ConnectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("mentorshipId", mentorshipId);
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sessions.Add(new AgentSession
            {
                Id = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                MentorshipId = reader.GetGuid(2),
                Status = Enum.Parse<AgentSessionStatus>(reader.GetString(3))
            });
        }
        
        return sessions;
    }

    public void Dispose()
    {
        // Limpar dados de teste após cada teste
        _helper.CleanupTestDataAsync().GetAwaiter().GetResult();
    }
}

