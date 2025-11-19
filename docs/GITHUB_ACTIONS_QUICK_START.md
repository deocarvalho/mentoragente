# ⚡ Quick Start: GitHub Actions em 5 Minutos

Guia rápido para configurar GitHub Actions **agora mesmo**.

---

## 🎯 O que você vai fazer

1. ✅ Fazer push do arquivo de workflow
2. ✅ Ver os testes executando automaticamente
3. ✅ (Opcional) Configurar URL para smoke tests

---

## 📝 Passo 1: Verificar se o arquivo existe

O arquivo já foi criado em:
```
.github/workflows/tests.yml
```

**Verifique:**
```powershell
# No PowerShell, na pasta do projeto
Test-Path .github\workflows\tests.yml
```

Se retornar `True`, pule para o Passo 2.

---

## 📤 Passo 2: Fazer Push para o GitHub

```powershell
# 1. Verificar status
git status

# 2. Adicionar arquivo
git add .github/workflows/tests.yml

# 3. Commit
git commit -m "ci: adicionar GitHub Actions para testes automatizados"

# 4. Push
git push origin development
```

**Pronto!** 🎉 GitHub Actions já está ativo!

---

## 👀 Passo 3: Ver os Testes Executando

1. Vá no GitHub: `https://github.com/seu-usuario/seu-repositorio`
2. Clique na aba **Actions** (no topo)
3. Você verá o workflow **"🧪 Tests"** executando
4. Clique nele para ver os detalhes

**Aguarde 3-6 minutos** para todos os testes terminarem.

---

## ✅ Passo 4: Verificar Resultado

### Se tudo passou (✅):
- Badge verde aparece
- Você pode fazer merge com confiança!

### Se algo falhou (❌):
1. Clique no job que falhou (ex: "Testes E2E")
2. Veja os logs para entender o erro
3. Corrija localmente
4. Faça push novamente

---

## 🔐 Passo 5: Configurar Smoke Tests (Opcional)

**Só faça isso se você tem uma aplicação deployada!**

1. No GitHub, vá em **Settings** → **Secrets and variables** → **Actions**
2. Clique em **New repository secret**
3. Configure:
   - **Name:** `SMOKE_TEST_URL`
   - **Value:** `https://sua-aplicacao.onrender.com`
4. Clique em **Add secret**

**Pronto!** Smoke tests vão executar automaticamente em `main`.

---

## 🎉 Pronto!

Agora, **toda vez que você fizer push**, os testes vão executar automaticamente!

### O que acontece automaticamente:

1. ✅ Você faz push
2. ✅ GitHub Actions detecta
3. ✅ Executa testes com mocks
4. ✅ Executa testes E2E (se Docker disponível)
5. ✅ Mostra resultado (verde/vermelho)

### Você não precisa fazer nada! 🚀

---

## 📊 Ver Resultados em Pull Requests

Quando você cria um Pull Request:

1. GitHub Actions executa automaticamente
2. Badge aparece no PR:
   - ✅ **"All checks have passed"** = Pode fazer merge
   - ❌ **"Some checks have failed"** = Corrija antes de fazer merge

---

## 🐛 Problemas Comuns

### "Workflow não aparece em Actions"
**Solução:** Verifique se você fez push do arquivo `.github/workflows/tests.yml`

### "Docker not found" em testes E2E
**Solução:** Normal se Docker não estiver disponível. Testes E2E serão pulados.

### "SMOKE_TEST_URL not configured"
**Solução:** Isso é normal! Smoke tests só executam se você configurou o secret.

---

## 📚 Mais Informações

Para entender melhor, leia:
- `docs/GITHUB_ACTIONS_GUIDE.md` - Guia completo e detalhado

---

**Tempo total:** ~5 minutos ⏱️

