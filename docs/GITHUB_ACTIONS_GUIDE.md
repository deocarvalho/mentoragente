# 🚀 Guia Completo: GitHub Actions (CI/CD)

Este guia explica **tudo** sobre GitHub Actions, desde o básico até como configurar testes automatizados.

---

## 📚 O que é GitHub Actions?

**GitHub Actions** é uma ferramenta do GitHub que permite **automatizar tarefas** quando você faz push, cria Pull Request, ou outras ações no repositório.

### Exemplo Prático:
- **Antes:** Você fazia push, depois executava testes manualmente
- **Agora:** Você faz push, GitHub Actions **executa testes automaticamente** e te avisa se passou ou falhou

---

## 🎯 O que vamos configurar?

1. ✅ **Testes automáticos** em cada push
2. ✅ **Testes E2E** com banco real (Docker)
3. ✅ **Smoke tests** após deploy em produção
4. ✅ **Notificações** quando testes falham

---

## 📁 Estrutura de Arquivos

GitHub Actions usa arquivos YAML (`.yml`) dentro da pasta `.github/workflows/`:

```
Mentoragente/
├── .github/
│   └── workflows/
│       └── tests.yml          ← Arquivo de configuração dos testes
└── ...
```

**Importante:** Esta pasta já foi criada! O arquivo `tests.yml` já está configurado.

---

## 🔧 Passo 1: Entender o Arquivo `tests.yml`

Vamos ver o que cada parte faz:

### 1. Nome e Triggers (Quando Executar)

```yaml
name: 🧪 Tests

on:
  push:
    branches: [development, main]
  pull_request:
    branches: [main]
```

**O que faz:**
- Executa testes quando você faz **push** para `development` ou `main`
- Executa testes quando você cria **Pull Request** para `main`

### 2. Jobs (Tarefas)

O arquivo tem 4 "jobs" (tarefas):

#### Job 1: `test-mocks` (Testes Rápidos)
- Executa testes com mocks
- Rápido (< 1 minuto)
- Sempre executa

#### Job 2: `test-e2e` (Testes E2E)
- Executa testes com banco real (Docker)
- Mais lento (2-5 minutos)
- Requer Docker

#### Job 3: `smoke-tests` (Pós-Deploy)
- Executa apenas em `main` (produção)
- Valida que aplicação está funcionando
- Requer URL configurada

#### Job 4: `test-summary` (Resumo)
- Mostra resumo dos resultados
- Sempre executa

---

## 🚀 Passo 2: Como Usar (Primeira Vez)

### Opção A: GitHub já tem o arquivo (Recomendado)

Se você já fez push do arquivo `.github/workflows/tests.yml`:

1. ✅ **Pronto!** GitHub Actions já está configurado
2. Faça um push qualquer para `development` ou `main`
3. Vá em **Actions** no GitHub (aba no topo do repositório)
4. Veja os testes executando em tempo real!

### Opção B: Criar manualmente (Se não tiver)

1. No GitHub, vá no seu repositório
2. Clique em **Actions** (aba no topo)
3. Clique em **New workflow**
4. Clique em **set up a workflow yourself**
5. Cole o conteúdo do arquivo `tests.yml`
6. Clique em **Start commit**
7. Commit na branch `development`

---

## 🔐 Passo 3: Configurar Secrets (Opcional)

**Secrets** são variáveis secretas (senhas, tokens) que você não quer expor no código.

### Quando usar?
- Para **Smoke Tests** (precisa da URL da aplicação deployada)
- Para **notificações** (Slack, email, etc.)

### Como configurar:

1. No GitHub, vá no seu repositório
2. Clique em **Settings** (configurações)
3. No menu lateral, clique em **Secrets and variables** → **Actions**
4. Clique em **New repository secret**
5. Configure:

   **Nome:** `SMOKE_TEST_URL`
   **Valor:** `https://mentoragente-hmg.onrender.com`
   
6. Clique em **Add secret**

**Pronto!** Agora o smoke test vai usar essa URL automaticamente.

---

## 📊 Passo 4: Ver Resultados

### Durante Execução:

1. Vá em **Actions** no GitHub
2. Clique no workflow que está rodando
3. Veja os logs em tempo real

### Após Execução:

1. Vá em **Actions**
2. Clique no workflow mais recente
3. Veja:
   - ✅ **Verde** = Todos os testes passaram
   - ❌ **Vermelho** = Algum teste falhou
   - ⚠️ **Amarelo** = Algum teste foi pulado

### Em Pull Requests:

Quando você cria um Pull Request, GitHub Actions executa automaticamente e mostra o resultado:

- ✅ **"All checks have passed"** = Pode fazer merge
- ❌ **"Some checks have failed"** = Corrija os erros antes de fazer merge

---

## 🎨 Entendendo os Símbolos

No arquivo `tests.yml`, você vê emojis nos nomes:

- 📥 = Baixar/Checkout
- 🔧 = Configurar/Setup
- 📦 = Pacotes/Dependências
- 🔨 = Compilar/Build
- 🧪 = Testes
- 🐳 = Docker
- 💨 = Smoke Tests
- 📊 = Resumo/Relatório

**Isso é só visual!** Não afeta o funcionamento.

---

## ⚙️ Personalização

### Executar apenas em certas branches:

```yaml
on:
  push:
    branches: [main]  # Só executa em main
```

### Executar manualmente:

```yaml
on:
  workflow_dispatch:  # Permite executar manualmente
```

Já está no arquivo! Você pode executar manualmente indo em **Actions** → **Tests** → **Run workflow**.

### Adicionar notificações:

```yaml
- name: 📧 Notificar no Slack
  if: failure()
  run: |
    # Código para enviar notificação
```

---

## 🐛 Troubleshooting

### ❌ "Docker not found"
**Problema:** Testes E2E falham porque Docker não está disponível.

**Solução:** O arquivo já está configurado com Docker. Se ainda falhar, verifique se o job `test-e2e` tem:
```yaml
services:
  docker:
    image: docker:24-dind
```

### ❌ "SMOKE_TEST_URL not configured"
**Problema:** Smoke tests são pulados.

**Solução:** Isso é **normal** se você não configurou o secret. Smoke tests só executam em `main` e se a URL estiver configurada.

### ❌ "Tests failed"
**Problema:** Testes falharam.

**Solução:**
1. Clique no job que falhou
2. Veja os logs para entender o erro
3. Corrija o código localmente
4. Faça push novamente

---

## 📈 Exemplo de Fluxo Completo

### Cenário: Você fez uma mudança

1. **Você faz commit e push:**
   ```bash
   git add .
   git commit -m "feat: nova feature"
   git push origin development
   ```

2. **GitHub Actions detecta o push:**
   - ✅ Inicia automaticamente

3. **Executa os jobs:**
   - ✅ `test-mocks` (30 segundos)
   - ✅ `test-e2e` (2-5 minutos)
   - ⏭️ `smoke-tests` (pulado, não é main)

4. **Você recebe notificação:**
   - ✅ Email do GitHub (se configurado)
   - ✅ Badge verde no Pull Request

5. **Se tudo passou:**
   - ✅ Você pode fazer merge com confiança!

---

## 🎯 Próximos Passos (Opcional)

### 1. Adicionar Badge no README

Adicione no `README.md`:

```markdown
![Tests](https://github.com/seu-usuario/seu-repo/actions/workflows/tests.yml/badge.svg)
```

### 2. Configurar Notificações

No GitHub:
1. Vá em **Settings** → **Notifications**
2. Configure notificações por email quando workflows falharem

### 3. Adicionar Mais Workflows

Crie outros workflows para:
- Deploy automático
- Linting
- Build de Docker
- etc.

---

## ✅ Checklist de Configuração

- [x] Arquivo `.github/workflows/tests.yml` criado
- [ ] Fazer push do arquivo para o GitHub
- [ ] (Opcional) Configurar secret `SMOKE_TEST_URL`
- [ ] Testar fazendo um push
- [ ] Verificar resultados em **Actions**

---

## 📚 Recursos Adicionais

- [Documentação oficial do GitHub Actions](https://docs.github.com/en/actions)
- [Marketplace de Actions](https://github.com/marketplace?type=actions)
- [Exemplos de workflows](https://github.com/actions/starter-workflows)

---

## ❓ Perguntas Frequentes

### P: Preciso pagar?
**R:** Não! GitHub Actions é **grátis** para repositórios públicos. Para privados, há um limite mensal grátis (2000 minutos).

### P: Posso desabilitar?
**R:** Sim! Basta deletar o arquivo `.github/workflows/tests.yml` ou comentar os triggers.

### P: Quanto tempo leva?
**R:** 
- Testes com mocks: ~30 segundos
- Testes E2E: ~2-5 minutos
- Total: ~3-6 minutos

### P: Posso executar localmente?
**R:** Sim! Use os scripts PowerShell que criamos:
```powershell
.\Mentoragente.Tests\scripts\run-all-tests.ps1
```

---

**Última atualização:** 2025-01-15

