# 🎨 Guia Visual: GitHub Actions

Guia visual passo a passo para entender e usar GitHub Actions.

---

## 🎯 O que é GitHub Actions? (Visual)

```
┌─────────────────────────────────────────────────┐
│  Você faz push no GitHub                         │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│  GitHub Actions detecta o push                   │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│  Executa testes automaticamente                 │
│  • Testes com mocks (30s)                       │
│  • Testes E2E (2-5min)                          │
│  • Smoke tests (30s)                             │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│  Mostra resultado: ✅ ou ❌                     │
└─────────────────────────────────────────────────┘
```

**Você não precisa fazer nada!** Tudo é automático.

---

## 📍 Onde encontrar no GitHub?

### 1. Aba "Actions" (No topo do repositório)

```
┌─────────────────────────────────────────────────┐
│  [Code] [Issues] [Pull requests] [Actions] ⭐  │
└─────────────────────────────────────────────────┘
```

### 2. Lista de Workflows

```
┌─────────────────────────────────────────────────┐
│  🧪 Tests                                        │
│  ─────────────────────────────────────────────   │
│  • Latest run: ✅ Passed (3 min ago)            │
│  • Previous: ✅ Passed (1 hour ago)              │
│  • Previous: ❌ Failed (2 hours ago)            │
└─────────────────────────────────────────────────┘
```

### 3. Detalhes de uma Execução

```
┌─────────────────────────────────────────────────┐
│  🧪 Tests #123                                   │
│  ─────────────────────────────────────────────   │
│                                                  │
│  Jobs:                                           │
│  ✅ test-mocks (30s)                             │
│  ✅ test-e2e (4m 12s)                           │
│  ⏭️ smoke-tests (skipped)                       │
│                                                  │
│  [Ver logs] [Re-executar]                       │
└─────────────────────────────────────────────────┘
```

---

## 🔄 Fluxo Completo Visual

### Cenário: Você fez uma mudança

```
┌─────────────────────────────────────────────────┐
│  1. Você faz commit e push                      │
│     git commit -m "feat: nova feature"          │
│     git push origin development                  │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│  2. GitHub recebe o push                        │
│     • Detecta mudanças em development            │
│     • Inicia workflow "🧪 Tests"                │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│  3. Executa Job 1: test-mocks                   │
│     • Setup .NET                                 │
│     • Restaurar pacotes                          │
│     • Compilar                                   │
│     • Executar testes com mocks                  │
│     ✅ Resultado: Passed (30s)                  │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│  4. Executa Job 2: test-e2e                     │
│     • Setup .NET                                 │
│     • Iniciar Docker                             │
│     • Executar testes E2E                        │
│     ✅ Resultado: Passed (4m 12s)                │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│  5. Mostra resultado final                      │
│     ✅ Todos os testes passaram!                 │
│                                                  │
│     Você pode fazer merge com confiança!        │
└─────────────────────────────────────────────────┘
```

---

## 📊 Badges no Pull Request

Quando você cria um Pull Request:

```
┌─────────────────────────────────────────────────┐
│  Pull Request #42                               │
│  ─────────────────────────────────────────────   │
│                                                  │
│  ✅ All checks have passed                      │
│     • test-mocks: ✅                             │
│     • test-e2e: ✅                               │
│                                                  │
│  [Merge pull request]                            │
└─────────────────────────────────────────────────┘
```

**Ou se falhou:**

```
┌─────────────────────────────────────────────────┐
│  Pull Request #42                               │
│  ─────────────────────────────────────────────   │
│                                                  │
│  ❌ Some checks have failed                      │
│     • test-mocks: ✅                             │
│     • test-e2e: ❌                               │
│                                                  │
│  [Ver detalhes]                                 │
└─────────────────────────────────────────────────┘
```

---

## 🔐 Configurar Secrets (Visual)

### Passo 1: Ir em Settings

```
┌─────────────────────────────────────────────────┐
│  Repositório: mentoragente                      │
│  ─────────────────────────────────────────────   │
│  [Code] [Issues] [Pull requests] [Actions]      │
│  [Projects] [Wiki] [Security] [Settings] ⚙️      │
└─────────────────────────────────────────────────┘
```

### Passo 2: Secrets and variables → Actions

```
┌─────────────────────────────────────────────────┐
│  Settings                                       │
│  ─────────────────────────────────────────────   │
│  • General                                      │
│  • Access                                       │
│  • Secrets and variables → Actions 🔐          │
│  • Actions → General                            │
│  • ...                                          │
└─────────────────────────────────────────────────┘
```

### Passo 3: New repository secret

```
┌─────────────────────────────────────────────────┐
│  Repository secrets                             │
│  ─────────────────────────────────────────────   │
│                                                  │
│  [New repository secret]                         │
│                                                  │
│  Name: SMOKE_TEST_URL                            │
│  Secret: https://mentoragente-hmg.onrender.com  │
│                                                  │
│  [Add secret]                                    │
└─────────────────────────────────────────────────┘
```

---

## 🎨 Cores e Símbolos

### Status dos Jobs:

- ✅ **Verde** = Passou (success)
- ❌ **Vermelho** = Falhou (failure)
- ⚠️ **Amarelo** = Pulado (skipped)
- 🔵 **Azul** = Executando (in progress)

### Símbolos nos Logs:

- 📥 = Baixando/Checkout
- 🔧 = Configurando
- 📦 = Instalando pacotes
- 🔨 = Compilando
- 🧪 = Executando testes
- 🐳 = Docker
- 💨 = Smoke tests
- 📊 = Relatório

---

## 📱 Notificações (Opcional)

### Configurar Email

```
┌─────────────────────────────────────────────────┐
│  Settings → Notifications                       │
│  ─────────────────────────────────────────────   │
│                                                  │
│  ☑️ Email                                       │
│     ☑️ Workflow runs                            │
│        ☑️ On failure                            │
│        ☑️ On success                            │
│                                                  │
│  [Save]                                         │
└─────────────────────────────────────────────────┘
```

**Resultado:** Você recebe email quando testes falham ou passam!

---

## 🎯 Resumo Visual

```
┌─────────────────────────────────────────────────┐
│  ANTES (Manual)                                 │
│  ─────────────────────────────────────────────   │
│  1. Push                                         │
│  2. Você executa testes manualmente              │
│  3. Você verifica resultados                    │
│  4. Você decide se pode fazer merge              │
│                                                  │
│  Tempo: 30-60 minutos                           │
└─────────────────────────────────────────────────┘

                    ⬇️

┌─────────────────────────────────────────────────┐
│  DEPOIS (GitHub Actions)                        │
│  ─────────────────────────────────────────────   │
│  1. Push                                         │
│  2. GitHub Actions executa automaticamente      │
│  3. GitHub mostra resultado                     │
│  4. Badge verde = pode fazer merge              │
│                                                  │
│  Tempo: 0 minutos (você não faz nada!)          │
└─────────────────────────────────────────────────┘
```

---

## ✅ Checklist Visual

```
┌─────────────────────────────────────────────────┐
│  ☑️ Arquivo .github/workflows/tests.yml criado   │
│  ☐ Fazer push do arquivo                        │
│  ☐ Verificar em Actions que apareceu            │
│  ☐ (Opcional) Configurar SMOKE_TEST_URL         │
│  ☐ Fazer um push de teste                       │
│  ☐ Verificar que testes executaram              │
│  ☐ ✅ Pronto!                                    │
└─────────────────────────────────────────────────┘
```

---

**Última atualização:** 2025-01-15

