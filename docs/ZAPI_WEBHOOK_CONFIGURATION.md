# 🔧 Configuração de Webhook Z-API

## ⚠️ Problema: "Missing Client-Token header"

Se você está vendo este erro nos logs:
```
Z-API webhook rejected: Missing Client-Token header
```

**Causa:** O Z-API não está enviando o header `Client-Token` nas requisições de webhook.

---

## ✅ Solução: Configurar Webhook no Z-API

O Z-API precisa ser configurado para **enviar** o `Client-Token` no header de cada webhook.

### Passo 1: Obter o Client-Token

1. Acesse o painel do Z-API
2. Vá em **Configurações** → **Webhooks** (ou similar)
3. Encontre o **Client-Token** da sua instância
4. **Copie o token** (exemplo: `F019f245f5d454e4cadff6f85bb3f4131S`)

### Passo 2: Configurar Webhook no Z-API

No painel do Z-API, ao configurar o webhook:

1. **URL do Webhook:**
   ```
   https://mentoragente-lpx7.onrender.com/api/webhooks/zapi
   ```

2. **Headers Customizados:**
   - **Nome do Header:** `Client-Token`
   - **Valor:** Seu Client-Token (ex: `F019f245f5d454e4cadff6f85bb3f4131S`)

3. **Método:** `POST`

4. **Content-Type:** `application/json`

### Passo 3: Verificar Variável de Ambiente no Render

Certifique-se de que a variável está configurada no Render:

```
ZApi__Client-Token=F019f245f5d454e4cadff6f85bb3f4131S
```

**Importante:** O valor deve ser **exatamente o mesmo** que você configurou no webhook do Z-API.

---

## 🔍 Como Verificar se Está Funcionando

### 1. Verificar Logs no Render

Após configurar, você deve ver nos logs:
```
✅ Received Z-API webhook payload: {...}
```

Em vez de:
```
❌ Z-API webhook rejected: Missing Client-Token header
```

### 2. Testar Manualmente

```bash
# Testar sem Client-Token (deve falhar)
curl -X POST https://mentoragente-lpx7.onrender.com/api/webhooks/zapi \
  -H "Content-Type: application/json" \
  -d '{"phone": "5511999999999", "text": {"message": "test"}}'

# Testar COM Client-Token (deve funcionar)
curl -X POST https://mentoragente-lpx7.onrender.com/api/webhooks/zapi \
  -H "Content-Type: application/json" \
  -H "Client-Token: F019f245f5d454e4cadff6f85bb3f4131S" \
  -d '{"phone": "5511999999999", "text": {"message": "test"}}'
```

---

## 📋 Checklist de Configuração

- [ ] Client-Token obtido do painel Z-API
- [ ] Webhook configurado no Z-API com header `Client-Token`
- [ ] Variável `ZApi__Client-Token` configurada no Render
- [ ] Valores são **idênticos** (Z-API e Render)
- [ ] Webhook testado e funcionando

---

## ⚠️ Problemas Comuns

### 1. "Missing Client-Token header"
**Causa:** Z-API não está enviando o header  
**Solução:** Configurar header customizado no webhook do Z-API

### 2. "Invalid Client-Token"
**Causa:** Token no header não corresponde ao token no Render  
**Solução:** Verificar se os valores são idênticos (case-sensitive)

### 3. "Z-API Client-Token not configured"
**Causa:** Variável `ZApi__Client-Token` não está configurada no Render  
**Solução:** Adicionar variável de ambiente no Render

---

## 🔐 Segurança

- ✅ **Nunca** compartilhe o Client-Token publicamente
- ✅ Use tokens **diferentes** para HMG e PRD (se usar instâncias diferentes)
- ✅ O token é validado em **cada requisição** de webhook
- ✅ Webhooks sem token válido são **rejeitados** (401 Unauthorized)

---

## 📚 Documentação Relacionada

- `docs/SECURITY_PRODUCTION_CHECKLIST.md` - Item #1: Webhook Authentication
- `docs/DEPLOYMENT_GUIDE.md` - Configuração de variáveis de ambiente

---

**Última atualização:** 2025-11-18

