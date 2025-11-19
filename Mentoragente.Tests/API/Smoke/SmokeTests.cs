using FluentAssertions;
using Xunit;
using System.Net;

namespace Mentoragente.Tests.API.Smoke;

/// <summary>
/// Testes de Smoke - Validam que a aplicação está funcionando após deploy
/// 
/// Estes testes são executados contra a aplicação DEPLOYADA (não local).
/// Use para validar que o deploy foi bem-sucedido.
/// 
/// Para executar:
/// 1. Configure a variável de ambiente SMOKE_TEST_URL
/// 2. Execute: dotnet test --filter "Category=Smoke"
/// </summary>
[Trait("Category", "Smoke")]
public class SmokeTests
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public SmokeTests()
    {
        // URL da aplicação em produção/staging
        // Pode ser configurado via variável de ambiente
        _baseUrl = Environment.GetEnvironmentVariable("SMOKE_TEST_URL") 
            ?? "https://mentoragente-hmg.onrender.com";
        
        _client = new HttpClient 
        { 
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30) // Timeout maior para smoke tests
        };
    }

    [Fact]
    [Trait("Requires", "DeployedApplication")]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Requires", "DeployedApplication")]
    public async Task HealthCheck_Live_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Requires", "DeployedApplication")]
    public async Task HealthCheck_Ready_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.ToLowerInvariant().Should().Contain("healthy", because: "Health check should return healthy status");
    }

    [Fact]
    [Trait("Requires", "DeployedApplication")]
    public async Task RootEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.ToLowerInvariant().Should().Contain("status", because: "Root endpoint should return status information");
    }

    [Fact]
    [Trait("Requires", "DeployedApplication")]
    public async Task Swagger_ShouldBeAvailableInDevelopment()
    {
        // Act
        var response = await _client.GetAsync("/swagger");

        // Assert
        // Em Development/Staging: deve retornar 200
        // Em Production: deve retornar 404
        // Aceitamos ambos como válidos
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Requires", "DeployedApplication")]
    public async Task ApiEndpoint_ShouldRequireAuthentication()
    {
        // Act - Tentar acessar endpoint administrativo sem autenticação
        var response = await _client.GetAsync("/api/mentorships");

        // Assert
        // Deve retornar 401 (Unauthorized) ou 403 (Forbidden)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

