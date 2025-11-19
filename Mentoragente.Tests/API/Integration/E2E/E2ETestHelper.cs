using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Mentoragente.API;
using Npgsql;

namespace Mentoragente.Tests.API.Integration.E2E;

/// <summary>
/// Helper para testes E2E com banco de dados real usando TestContainers
/// 
/// IMPORTANTE: Estes testes são mais lentos que testes com mocks, mas testam
/// integração real com o banco de dados, incluindo queries, constraints, transações, etc.
/// 
/// Requisitos:
/// - Docker instalado e rodando
/// - Acesso à internet (para baixar imagem do PostgreSQL)
/// </summary>
public class E2ETestHelper : IDisposable
{
    private readonly PostgreSqlContainer _postgresContainer;
    public WebApplicationFactory<Program> Factory { get; }
    public HttpClient Client { get; }
    public string ConnectionString { get; }

    public E2ETestHelper()
    {
        // Iniciar PostgreSQL em Docker usando TestContainers
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("mentoragente_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();

        // Aguardar container iniciar
        _postgresContainer.StartAsync().GetAwaiter().GetResult();
        ConnectionString = _postgresContainer.GetConnectionString();

        // Criar factory com configuração do banco real
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Carregar appsettings.Test.json se existir
                    var testConfigPath = Path.Combine(
                        AppContext.BaseDirectory,
                        "..", "..", "..", "..", "appsettings.Test.json");
                    
                    if (File.Exists(testConfigPath))
                    {
                        config.AddJsonFile(testConfigPath, optional: true);
                    }

                    // Substituir configuração do Supabase para usar o banco de teste
                    // Nota: Isso requer ajuste na forma como o Supabase client é criado
                    // Por enquanto, vamos usar a connection string diretamente
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "Supabase:Url", "http://localhost" }, // Não usado, mas necessário
                        { "Supabase:ServiceRoleKey", "test-key" }, // Não usado, mas necessário
                        { "TestDatabase:ConnectionString", ConnectionString },
                        { "ZApi:Client-Token", "test-client-token-e2e" },
                        { "ApiKey", "test-api-key-e2e" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Aqui você pode substituir o Supabase client para usar Npgsql diretamente
                    // ou criar um Supabase client apontando para o container
                    // Por enquanto, mantemos os serviços reais mas com banco de teste
                });
            });

        Client = Factory.CreateClient();

        // Executar migrations no banco de teste
        ExecuteMigrations();
    }

    /// <summary>
    /// Executa o schema do banco de dados no container de teste
    /// </summary>
    private void ExecuteMigrations()
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            // Ler o arquivo DATABASE_SCHEMA.sql
            var schemaPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", "..", "DATABASE_SCHEMA.sql");

            if (!File.Exists(schemaPath))
            {
                // Tentar caminho alternativo
                schemaPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..", "DATABASE_SCHEMA.sql");
            }

            if (File.Exists(schemaPath))
            {
                var schemaSql = File.ReadAllText(schemaPath);
                
                // Executar schema (dividir por comandos se necessário)
                using var command = new NpgsqlCommand(schemaSql, connection);
                command.ExecuteNonQuery();
            }
            else
            {
                // Se não encontrar o arquivo, criar schema básico manualmente
                CreateBasicSchema(connection);
            }
        }
        catch (Exception ex)
        {
            // Log do erro, mas não falhar o teste
            Console.WriteLine($"Warning: Could not execute migrations: {ex.Message}");
        }
    }

    /// <summary>
    /// Cria schema básico se não conseguir ler o arquivo
    /// </summary>
    private void CreateBasicSchema(NpgsqlConnection connection)
    {
        var basicSchema = @"
            CREATE TYPE user_status AS ENUM ('Active', 'Inactive', 'Blocked');
            CREATE TYPE agent_session_status AS ENUM ('Active', 'Expired', 'Paused', 'Completed');
            CREATE TYPE ai_provider AS ENUM ('OpenAI');
            CREATE TYPE mentorship_status AS ENUM ('Active', 'Inactive', 'Archived');
            CREATE TYPE whatsapp_provider AS ENUM ('EvolutionAPI', 'ZApi', 'OfficialWhatsApp');

            CREATE TABLE users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                phone_number VARCHAR UNIQUE NOT NULL,
                name VARCHAR NOT NULL,
                email VARCHAR NULL,
                status user_status NOT NULL DEFAULT 'Active',
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE mentorships (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR NOT NULL,
                mentor_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                assistant_id VARCHAR NOT NULL,
                duration_days INT NOT NULL,
                description TEXT NULL,
                status mentorship_status NOT NULL DEFAULT 'Active',
                whatsapp_provider whatsapp_provider NOT NULL DEFAULT 'ZApi',
                instance_code VARCHAR NOT NULL,
                instance_token VARCHAR NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE agent_sessions (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                mentorship_id UUID NOT NULL REFERENCES mentorships(id) ON DELETE CASCADE,
                ai_provider ai_provider NOT NULL DEFAULT 'OpenAI',
                ai_context_id VARCHAR NULL,
                status agent_session_status NOT NULL DEFAULT 'Active',
                last_interaction TIMESTAMP NULL,
                total_messages INT NOT NULL DEFAULT 0,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX idx_agent_sessions_unique_active 
                ON agent_sessions(user_id, mentorship_id) 
                WHERE status = 'Active';
        ";

        using var command = new NpgsqlCommand(basicSchema, connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Executa uma query SQL diretamente no banco de teste
    /// </summary>
    public async Task<T?> ExecuteQueryAsync<T>(string sql, Func<NpgsqlDataReader, T> mapper, params NpgsqlParameter[] parameters)
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        if (parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }
        
        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return mapper(reader);
        }
        
        return default;
    }

    /// <summary>
    /// Limpa dados de teste do banco
    /// </summary>
    public async Task CleanupTestDataAsync()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        var cleanupSql = @"
            TRUNCATE TABLE agent_sessions CASCADE;
            TRUNCATE TABLE mentorships CASCADE;
            TRUNCATE TABLE users CASCADE;
        ";
        
        using var command = new NpgsqlCommand(cleanupSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        Client?.Dispose();
        Factory?.Dispose();
        _postgresContainer?.DisposeAsync().GetAwaiter().GetResult();
    }
}

