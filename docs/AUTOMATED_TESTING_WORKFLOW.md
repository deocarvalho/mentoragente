# 🤖 Workflow de Testes Automatizados

Este documento descreve como usar os testes automatizados para **diminuir o trabalho operacional** e aumentar a confiança nas mudanças.

---

## 🎯 Objetivo

**Automatizar ao máximo os testes manuais**, permitindo que você:
- ✅ Confie que mudanças não quebraram funcionalidades existentes
- ✅ Detecte problemas antes de fazer deploy
- ✅ Reduza tempo gasto em testes manuais repetitivos
- ✅ Tenha documentação viva do comportamento esperado

---

## 📋 Tipos de Testes e Quando Usar

### 1. **Testes com Mocks** (Desenvolvimento Diário)
**Velocidade:** ⚡⚡⚡ Muito rápido (< 1 segundo)
**Cobertura:** Lógica de negócio, estrutura HTTP, validações

**Quando usar:**
- ✅ Durante desenvolvimento
- ✅ Antes de cada commit
- ✅ Em CI/CD para validação rápida

**Como executar:**
```powershell
# Todos os testes rápidos
dotnet test --filter "Category!=E2E&Category!=Smoke"

# Apenas testes de um controller
dotnet test --filter "FullyQualifiedName~EnrollmentsIntegrationTests"
```

### 2. **Testes E2E com TestContainers** (Validação Crítica)
**Velocidade:** ⚡⚡ Rápido (5-10 segundos por teste)
**Cobertura:** Banco de dados real, queries, constraints, transações

**Quando usar:**
- ✅ Antes de merge para `main`
- ✅ Antes de deploy para produção
- ✅ Quando mudanças afetam banco de dados

**Como executar:**
```powershell
# Requer Docker rodando
dotnet test --filter "Category=E2E"

# Apenas testes de enrollment E2E
dotnet test --filter "FullyQualifiedName~EnrollmentsE2ETests"
```

### 3. **Testes de Smoke** (Pós-Deploy)
**Velocidade:** ⚡⚡ Rápido (2-5 segundos)
**Cobertura:** Aplicação deployada funcionando

**Quando usar:**
- ✅ Após cada deploy
- ✅ Em CI/CD após deploy automático
- ✅ Validação rápida de que deploy foi bem-sucedido

**Como executar:**
```powershell
# Configurar URL
$env:SMOKE_TEST_URL="https://mentoragente-hmg.onrender.com"

# Executar
dotnet test --filter "Category=Smoke"
```

---

## 🚀 Scripts de Automação

### Script Principal: `run-all-tests.ps1`

Executa todos os tipos de testes de forma organizada:

```powershell
# Executar todos os testes
.\Mentoragente.Tests\scripts\run-all-tests.ps1

# Apenas testes rápidos (mocks)
.\Mentoragente.Tests\scripts\run-all-tests.ps1 -SkipE2E -SkipSmoke

# Apenas testes E2E
.\Mentoragente.Tests\scripts\run-all-tests.ps1 -OnlyE2E

# Com output detalhado
.\Mentoragente.Tests\scripts\run-all-tests.ps1 -Verbose
```

**O que o script faz:**
1. ✅ Verifica se Docker está rodando (para E2E)
2. ✅ Compila o projeto
3. ✅ Executa testes com mocks
4. ✅ Executa testes E2E (se Docker disponível)
5. ✅ Executa testes de Smoke (se URL configurada)
6. ✅ Mostra resumo dos resultados

### Script de Smoke: `run-smoke-tests.ps1`

Executa apenas testes de Smoke:

```powershell
# Com URL específica
.\Mentoragente.Tests\scripts\run-smoke-tests.ps1 -Url "https://mentoragente-hmg.onrender.com"

# Usando variável de ambiente
$env:SMOKE_TEST_URL="https://mentoragente-hmg.onrender.com"
.\Mentoragente.Tests\scripts\run-smoke-tests.ps1
```

---

## 📅 Workflow Recomendado

### **Durante Desenvolvimento**

```powershell
# 1. Desenvolver feature
# ... código ...

# 2. Executar testes rápidos (mocks)
dotnet test --filter "Category!=E2E&Category!=Smoke"

# 3. Se passou, fazer commit
git add .
git commit -m "feat: nova feature"
```

### **Antes de Merge para Main**

```powershell
# 1. Executar todos os testes (incluindo E2E)
.\Mentoragente.Tests\scripts\run-all-tests.ps1

# 2. Se tudo passou, fazer merge
git checkout main
git merge development
```

### **Após Deploy**

```powershell
# 1. Executar smoke tests
.\Mentoragente.Tests\scripts\run-smoke-tests.ps1 -Url "https://mentoragente-hmg.onrender.com"

# 2. Se passou, deploy foi bem-sucedido ✅
```

---

## 🔧 Integração em CI/CD

### GitHub Actions (Exemplo)

```yaml
name: Tests

on:
  push:
    branches: [development, main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Setup Docker
        run: |
          sudo systemctl start docker
      
      - name: Run Tests (Mocks)
        run: |
          dotnet test --filter "Category!=E2E&Category!=Smoke" --no-build
      
      - name: Run E2E Tests
        run: |
          dotnet test --filter "Category=E2E" --no-build
      
      - name: Run Smoke Tests (apenas em main)
        if: github.ref == 'refs/heads/main'
        env:
          SMOKE_TEST_URL: ${{ secrets.SMOKE_TEST_URL }}
        run: |
          dotnet test --filter "Category=Smoke" --no-build
```

### Render (Post-Deploy Hook)

No Render, configure um **Webhook** que executa smoke tests após deploy:

```bash
# Script: post-deploy.sh
#!/bin/bash
export SMOKE_TEST_URL="https://mentoragente-hmg.onrender.com"
dotnet test --filter "Category=Smoke" --no-build
```

---

## 📊 O que Cada Teste Cobre

### Testes com Mocks
- ✅ Validação de entrada (FluentValidation)
- ✅ Estrutura de resposta HTTP
- ✅ Lógica de negócio (services)
- ✅ Tratamento de erros
- ✅ Autenticação e autorização

### Testes E2E (TestContainers)
- ✅ Criação real de usuários no banco
- ✅ Criação real de mentorships
- ✅ Criação real de sessions
- ✅ Constraints do banco (unique, foreign keys)
- ✅ Transações e race conditions
- ✅ Queries SQL reais
- ✅ Fluxo completo de webhook (com banco real)

### Testes de Smoke
- ✅ Health checks funcionando
- ✅ Aplicação respondendo
- ✅ Endpoints básicos acessíveis
- ✅ Autenticação funcionando

---

## ⚠️ Limitações

### Testes E2E
- ⚠️ **Não testa Supabase client** - usa Npgsql diretamente
- ⚠️ **Não testa APIs externas** - OpenAI, Z-API ainda são mockadas
- ⚠️ **Mais lento** - cada teste leva alguns segundos

### Testes de Smoke
- ⚠️ **Requer aplicação deployada** - não funciona localmente
- ⚠️ **Depende de rede** - precisa de internet
- ⚠️ **Não testa lógica complexa** - apenas valida que está funcionando

---

## 🎯 Redução de Trabalho Operacional

### Antes (Manual)
1. ❌ Testar cada endpoint manualmente (Postman/curl)
2. ❌ Verificar banco de dados manualmente
3. ❌ Testar após cada deploy manualmente
4. ❌ **Tempo:** 30-60 minutos por deploy

### Depois (Automatizado)
1. ✅ Executar `run-all-tests.ps1` (2-5 minutos)
2. ✅ Smoke tests automáticos após deploy (30 segundos)
3. ✅ CI/CD executa testes automaticamente
4. ✅ **Tempo:** 5-10 minutos (incluindo execução)

**Economia:** ~80% do tempo de testes manuais! 🎉

---

## 📚 Documentação Relacionada

- `docs/E2E_TESTING_GUIDE.md` - Guia completo de testes E2E
- `Mentoragente.Tests/README_E2E.md` - README específico para testes E2E
- `docs/SECURITY_PRODUCTION_CHECKLIST.md` - Checklist de segurança

---

**Última atualização:** 2025-01-15

