# 🧪 Testes E2E (End-to-End) Automatizados

Este documento explica como usar os testes E2E que automatizam os testes manuais.

---

## 📋 Tipos de Testes Disponíveis

### 1. **Testes E2E com TestContainers** (Banco Real)
- ✅ Testa com PostgreSQL real em Docker
- ✅ Testa queries, constraints, transações reais
- ✅ Isolado (cada teste tem seu próprio banco)
- ⚠️ Requer Docker instalado e rodando
- ⚠️ Mais lento que testes com mocks (5-10 segundos por teste)

**Localização:** `Mentoragente.Tests/API/Integration/E2E/`

### 2. **Testes de Smoke** (Pós-Deploy)
- ✅ Valida que a aplicação está funcionando após deploy
- ✅ Rápido e simples
- ⚠️ Requer aplicação deployada

**Localização:** `Mentoragente.Tests/API/Smoke/`

---

## 🚀 Como Executar

### Pré-requisitos

1. **Docker Desktop** instalado e rodando (para TestContainers)
2. **.NET 8.0 SDK** instalado

### Executar Testes E2E (TestContainers)

```bash
# Executar todos os testes E2E
dotnet test --filter "Category=E2E"

# Executar teste específico
dotnet test --filter "FullyQualifiedName~EnrollmentsE2ETests"

# Executar com verbose para ver logs do Docker
dotnet test --filter "Category=E2E" --verbosity normal
```

### Executar Testes de Smoke

```bash
# Configurar URL da aplicação deployada
$env:SMOKE_TEST_URL="https://mentoragente-hmg.onrender.com"

# Executar testes de smoke
dotnet test --filter "Category=Smoke"
```

### Executar Todos os Testes (Mocks + E2E)

```bash
# Executar todos os testes (incluindo E2E)
dotnet test

# Executar apenas testes com mocks (rápidos)
dotnet test --filter "Category!=E2E&Category!=Smoke"
```

---

## 📝 O que Cada Tipo de Teste Cobre

### Testes E2E (TestContainers)

**Cobertura:**
- ✅ Criação real de usuários no banco
- ✅ Criação real de mentorships no banco
- ✅ Criação real de sessions no banco
- ✅ Constraints do banco (unique, foreign keys)
- ✅ Transações e race conditions
- ✅ Queries SQL reais

**Exemplo:**
```csharp
[Fact]
public async Task CreateEnrollment_E2E_ShouldCreateUserAndSessionInDatabase()
{
    // Este teste cria dados REAIS no banco e verifica que foram criados
    // Não usa mocks - testa integração completa
}
```

### Testes de Smoke

**Cobertura:**
- ✅ Health checks funcionando
- ✅ Aplicação respondendo
- ✅ Endpoints básicos acessíveis
- ✅ Autenticação funcionando

**Exemplo:**
```csharp
[Fact]
public async Task HealthCheck_ShouldReturnOk()
{
    // Testa aplicação DEPLOYADA
    // Valida que deploy foi bem-sucedido
}
```

---

## 🔧 Configuração

### TestContainers

Não requer configuração adicional. O TestContainers:
1. Baixa imagem do PostgreSQL automaticamente
2. Inicia container Docker
3. Executa migrations
4. Limpa tudo após os testes

### Smoke Tests

Configure a URL da aplicação:

**Windows (PowerShell):**
```powershell
$env:SMOKE_TEST_URL="https://mentoragente-hmg.onrender.com"
```

**Linux/Mac:**
```bash
export SMOKE_TEST_URL="https://mentoragente-hmg.onrender.com"
```

**Ou crie `appsettings.Test.json`:**
```json
{
  "SmokeTest": {
    "BaseUrl": "https://mentoragente-hmg.onrender.com"
  }
}
```

---

## ⚠️ Limitações

### TestContainers
- ⚠️ **Não testa Supabase client** - usa Npgsql diretamente
- ⚠️ **Não testa APIs externas** - OpenAI, Z-API ainda são mockadas
- ⚠️ **Mais lento** - cada teste leva alguns segundos

### Smoke Tests
- ⚠️ **Requer aplicação deployada** - não funciona localmente
- ⚠️ **Depende de rede** - precisa de internet
- ⚠️ **Não testa lógica complexa** - apenas valida que está funcionando

---

## 📊 Comparação

| Aspecto | Testes com Mocks | Testes E2E (TestContainers) | Smoke Tests |
|--------|------------------|----------------------------|-------------|
| **Velocidade** | ⚡⚡⚡ Muito rápido | ⚡⚡ Rápido | ⚡⚡ Rápido |
| **Banco Real** | ❌ Mock | ✅ Real (Docker) | ❌ N/A |
| **APIs Externas** | ❌ Mock | ❌ Mock | ✅ Real |
| **Deploy Necessário** | ❌ Não | ❌ Não | ✅ Sim |
| **Docker Necessário** | ❌ Não | ✅ Sim | ❌ Não |
| **Cobertura** | Lógica | Lógica + Banco | Funcionalidade básica |

---

## ✅ Quando Usar Cada Tipo

### Desenvolvimento Diário
- ✅ **Testes com Mocks** (rápidos, cobrem lógica)

### Antes de Commit
- ✅ **Todos os testes com mocks**
- ✅ **Testes E2E críticos** (Enrollment, Webhook)

### Antes de Deploy
- ✅ **Todos os testes automatizados**
- ✅ **Teste manual rápido** (5-10 min) dos fluxos críticos

### Após Deploy
- ✅ **Testes de Smoke** (automatizados via CI/CD)
- ✅ **Monitoramento** (health checks, logs)

---

## 🐛 Troubleshooting

### Erro: "Docker daemon is not running"
**Solução:** Inicie o Docker Desktop

### Erro: "Could not find DATABASE_SCHEMA.sql"
**Solução:** O teste criará schema básico automaticamente. Para usar schema completo, coloque `DATABASE_SCHEMA.sql` na raiz do projeto.

### Erro: "Connection refused" em Smoke Tests
**Solução:** Verifique se a aplicação está deployada e acessível. Verifique a variável `SMOKE_TEST_URL`.

### Testes E2E muito lentos
**Solução:** Normal. TestContainers leva alguns segundos para iniciar. Execute apenas quando necessário:
```bash
dotnet test --filter "Category=E2E"  # Apenas E2E
dotnet test --filter "Category!=E2E"  # Todos exceto E2E
```

---

## 📚 Documentação Relacionada

- `docs/E2E_TESTING_GUIDE.md` - Guia completo de testes E2E
- `README.md` - Documentação geral do projeto de testes

---

**Última atualização:** 2025-01-15

