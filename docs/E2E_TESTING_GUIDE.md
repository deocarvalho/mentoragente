# 🧪 Guia de Testes E2E (End-to-End) Automatizados

Este guia explica como automatizar os testes manuais usando diferentes abordagens.

---

## 📋 Opções de Automação

### 1. **TestContainers** (Recomendado para Banco de Dados)
- ✅ Testa com PostgreSQL real em Docker
- ✅ Testa queries, constraints, transações reais
- ✅ Isolado (cada teste tem seu próprio banco)
- ⚠️ Mais lento que mocks (segundos vs milissegundos)

### 2. **Testes com Serviços Reais** (Ambiente de Teste)
- ✅ Testa integração real com APIs externas
- ✅ Valida configuração e credenciais
- ⚠️ Requer ambiente de teste configurado
- ⚠️ Depende de serviços externos estarem online

### 3. **Testes de Smoke** (Pós-Deploy)
- ✅ Valida que a aplicação está funcionando após deploy
- ✅ Rápido e simples
- ⚠️ Não testa lógica complexa

### 4. **Scripts de Teste** (Postman/Newman, curl, etc.)
- ✅ Fácil de escrever e executar
- ✅ Pode ser integrado em CI/CD
- ⚠️ Menos integrado ao código

---

## 🚀 Implementação: TestContainers para Banco Real

### Passo 1: Adicionar Pacotes

```xml
<!-- Mentoragente.Tests.csproj -->
<ItemGroup>
  <PackageReference Include="Testcontainers.PostgreSql" Version="3.9.0" />
  <PackageReference Include="Testcontainers" Version="3.9.0" />
</ItemGroup>
```

### Passo 2: Criar Helper para Testes E2E

```csharp
// Mentoragente.Tests/API/Integration/E2ETestHelper.cs
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Mvc.Testing;
using Mentoragente.API;

public class E2ETestHelper : IDisposable
{
    private readonly PostgreSqlContainer _postgresContainer;
    public WebApplicationFactory<Program> Factory { get; }
    public HttpClient Client { get; }
    public string ConnectionString { get; }

    public E2ETestHelper()
    {
        // Iniciar PostgreSQL em Docker
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("mentoragente_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();

        _postgresContainer.StartAsync().Wait();
        ConnectionString = _postgresContainer.GetConnectionString();

        // Criar factory com banco real
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Substituir connection string do Supabase
                    // (ajustar conforme sua implementação)
                });
            });

        Client = Factory.CreateClient();
        
        // Executar migrations no banco de teste
        ExecuteMigrations();
    }

    private void ExecuteMigrations()
    {
        // Executar DATABASE_SCHEMA.sql no banco de teste
        // Pode usar Npgsql diretamente ou Supabase client
    }

    public void Dispose()
    {
        Client?.Dispose();
        Factory?.Dispose();
        _postgresContainer?.DisposeAsync().AsTask().Wait();
    }
}
```

### Passo 3: Criar Testes E2E

```csharp
// Mentoragente.Tests/API/Integration/E2E/EnrollmentsE2ETests.cs
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

        // Verificar no banco real
        var user = await GetUserFromDatabase(request.PhoneNumber);
        user.Should().NotBeNull();
        user!.Name.Should().Be(request.Name);

        var session = await GetSessionFromDatabase(user.Id, mentorshipId);
        session.Should().NotBeNull();
        session!.Status.Should().Be(AgentSessionStatus.Active);
    }

    private async Task<Guid> CreateTestMentorship()
    {
        // Criar mentorship real no banco de teste
        // ...
    }

    private async Task<User?> GetUserFromDatabase(string phoneNumber)
    {
        // Buscar usuário real do banco
        // ...
    }

    private async Task<AgentSession?> GetSessionFromDatabase(Guid userId, Guid mentorshipId)
    {
        // Buscar sessão real do banco
        // ...
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
```

---

## 🔧 Implementação: Testes com Serviços Reais

### Opção A: Ambiente de Teste Dedicado

```csharp
// Mentoragente.Tests/API/Integration/RealServices/EnrollmentsRealServicesTests.cs
public class EnrollmentsRealServicesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly IConfiguration _config;

    public EnrollmentsRealServicesTests(WebApplicationFactory<Program> factory)
    {
        // Usar configuração de TESTE (não produção!)
        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .AddEnvironmentVariables("TEST_")
            .Build();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddConfiguration(_config);
            });
        }).CreateClient();
    }

    [Fact]
    [Trait("Category", "RealServices")]
    [Trait("Requires", "TestEnvironment")]
    public async Task CreateEnrollment_WithRealServices_ShouldWork()
    {
        // Este teste usa serviços REAIS (Supabase de teste, OpenAI de teste, etc.)
        // ⚠️ Requer ambiente de teste configurado
        // ⚠️ Pode custar dinheiro (OpenAI API calls)
        
        var request = new CreateEnrollmentRequestDto
        {
            PhoneNumber = "5511999999999",
            MentorshipId = Guid.Parse("test-mentorship-id"),
            Name = "Test User",
            Email = "test@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/enrollments", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### Opção B: Testes de Smoke (Pós-Deploy)

```csharp
// Mentoragente.Tests/API/Smoke/SmokeTests.cs
public class SmokeTests
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public SmokeTests()
    {
        // URL da aplicação em produção/staging
        _baseUrl = Environment.GetEnvironmentVariable("SMOKE_TEST_URL") 
            ?? "https://mentoragente-hmg.onrender.com";
        _client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HealthCheck_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HealthCheck_Ready_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Swagger_ShouldBeAvailableInDevelopment()
    {
        // Só funciona em dev/staging
        var response = await _client.GetAsync("/swagger");
        // Em produção deve retornar 404, em dev deve retornar 200
    }
}
```

---

## 📝 Scripts de Teste (Postman/Newman)

### Criar Collection no Postman

1. Criar requests para cada endpoint
2. Adicionar testes em cada request
3. Exportar collection

### Executar com Newman (CLI)

```bash
# Instalar Newman
npm install -g newman

# Executar collection
newman run Mentoragente.postman_collection.json \
  --environment test-environment.json \
  --reporters cli,json \
  --reporter-json-export test-results.json
```

### Integrar em CI/CD

```yaml
# .github/workflows/smoke-tests.yml
name: Smoke Tests

on:
  deployment_status:
    types: [success]

jobs:
  smoke-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Run Smoke Tests
        run: |
          npm install -g newman
          newman run Mentoragente.postman_collection.json \
            --environment ${{ secrets.TEST_ENV }} \
            --env-var "baseUrl=${{ secrets.DEPLOYMENT_URL }}"
```

---

## 🎯 Recomendações por Cenário

### Desenvolvimento Local
- ✅ **Testes com Mocks** (rápidos, atuais)
- ✅ **Testes E2E com TestContainers** (para validar banco)

### Antes de Commit
- ✅ **Todos os testes com mocks**
- ✅ **Testes E2E críticos com TestContainers**

### Antes de Deploy
- ✅ **Todos os testes automatizados**
- ✅ **Testes de Smoke** (se disponível)
- ⚠️ **Teste manual rápido** (5-10 min) dos fluxos críticos

### Após Deploy
- ✅ **Testes de Smoke automatizados** (via CI/CD)
- ✅ **Monitoramento** (health checks, logs)

---

## 📊 Comparação de Abordagens

| Abordagem | Velocidade | Realismo | Custo | Complexidade |
|-----------|-----------|----------|-------|--------------|
| **Mocks** | ⚡⚡⚡ Muito rápido | ⭐ Baixo | 💰 Grátis | 🟢 Simples |
| **TestContainers** | ⚡⚡ Rápido | ⭐⭐⭐ Alto | 💰 Grátis | 🟡 Média |
| **Serviços Reais** | ⚡ Lento | ⭐⭐⭐⭐⭐ Muito alto | 💰💰 Pode custar | 🔴 Alta |
| **Smoke Tests** | ⚡⚡ Rápido | ⭐⭐ Médio | 💰 Grátis | 🟢 Simples |
| **Scripts (Postman)** | ⚡⚡ Rápido | ⭐⭐⭐ Alto | 💰 Grátis | 🟡 Média |

---

## ✅ Checklist de Implementação

- [ ] Adicionar TestContainers para testes de banco
- [ ] Criar testes E2E para fluxos críticos (Enrollment, Webhook)
- [ ] Configurar ambiente de teste (appsettings.Test.json)
- [ ] Criar testes de Smoke para pós-deploy
- [ ] Integrar testes de Smoke em CI/CD
- [ ] Documentar como executar cada tipo de teste

---

**Última atualização:** 2025-01-15

