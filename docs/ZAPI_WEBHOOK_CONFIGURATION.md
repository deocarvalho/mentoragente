# 🔧 Configuração de Webhook Z-API

## 📋 Entendendo os Tokens

### Client-Token
- **O que é:** Token único por conta Z-API
- **Onde fica:** `appsettings.json` → `ZApi:Client-Token` (variável de ambiente: `ZApi__Client-Token`)
- **Para que serve:** Autenticar requisições que **FAZEMOS** para a API do Z-API
- **Exemplo:** Quando enviamos mensagens via `ZApiService.SendMessageAsync()`

### Z-Api-Token (instance_token)
- **O que é:** Token único por instância Z-API
- **Onde fica:** Tabela `mentorships.instance_token` no banco de dados
- **Para que serve:** 
  - Identificar a mentorship quando o Z-API envia webhooks
  - O Z-API envia automaticamente no header `Z-Api-Token` de cada webhook
- **Validação:** O sistema busca a mentorship ativa que tem esse `instance_token`

---

## ⚠️ Problema: "Missing Z-Api-Token header" ou "No active mentorship found"

Se você está vendo estes erros nos logs:
```
Z-API webhook rejected: Missing Z-Api-Token header
```
ou
```
Z-API webhook rejected: No active mentorship found for instance token
```

**Causas possíveis:**
1. Z-API não está enviando o header `Z-Api-Token` (improvável)
2. O `instance_token` na tabela `mentorships` não corresponde ao token que o Z-API está enviando
3. A mentorship está inativa ou não existe

---

## ✅ Solução: Configurar instance_token na Mentorship

### Passo 1: Obter o Z-Api-Token da Instância

1. Acesse o painel do Z-API
2. Vá em **Configurações** → **Instância** (ou similar)
3. Encontre o **Token da Instância** (exemplo: `71AD58EA2BA03C41A38C5C3B`)
4. **Copie o token**

### Passo 2: Configurar na Mentorship

Ao criar ou atualizar uma mentorship, configure o `instance_token` com o token da instância Z-API:

```json
{
  "name": "Minha Mentoria",
  "instanceCode": "3EA14DA371360142A582CEE0FE05F6ED",
  "instanceToken": "71AD58EA2BA03C41A38C5C3B",
  "whatsAppProvider": "ZApi"
}
```

**Importante:** 
- O `instance_token` deve ser o **mesmo** que o Z-API envia no header `Z-Api-Token`
- Cada instância Z-API tem seu próprio token único
- O sistema valida o webhook buscando a mentorship ativa com esse `instance_token`

### Passo 3: Configurar Client-Token no Render (para requisições que fazemos ao Z-API)

```
ZApi__Client-Token=F019f245f5d454e4cadff6f85bb3f4131S
```

**Nota:** Este é o token da **conta** Z-API (não da instância), usado para autenticar requisições que fazemos para a API do Z-API.

---

## 🔍 Como Verificar se Está Funcionando

### 1. Verificar Logs no Render

Após configurar, você deve ver nos logs:
```
✅ Received Z-API webhook payload: {...}
```

Em vez de:
```
❌ Z-API webhook rejected: Missing Z-Api-Token header
```

### 2. Testar Manualmente

```bash
# Testar sem Z-Api-Token (deve falhar)
curl -X POST https://mentoragente-lpx7.onrender.com/api/webhooks/zapi \
  -H "Content-Type: application/json" \
  -d '{"phone": "5511999999999", "text": {"message": "test"}}'

# Testar COM Z-Api-Token (deve funcionar)
curl -X POST https://mentoragente-lpx7.onrender.com/api/webhooks/zapi \
  -H "Content-Type: application/json" \
  -H "Z-Api-Token: 71AD58EA2BA03C41A38C5C3B" \
  -d '{"phone": "5511999999999", "text": {"message": "test"}}'
```

---

## 📋 Checklist de Configuração

- [ ] **Client-Token** configurado no Render (`ZApi__Client-Token`) - token da conta Z-API
- [ ] **instance_token** configurado na mentorship - token da instância Z-API
- [ ] Webhook configurado no Z-API com URL: `https://mentoragente-lpx7.onrender.com/api/webhooks/zapi`
- [ ] Valores são **idênticos** (token que Z-API envia no header `Z-Api-Token` = `instance_token` na mentorship)
- [ ] Mentorship está **ativa** (status = Active)
- [ ] Webhook testado e funcionando

---

## ⚠️ Problemas Comuns

### 1. "Missing Z-Api-Token header"
**Causa:** Z-API não está enviando o header (improvável - Z-API sempre envia)  
**Solução:** Verificar se o webhook está configurado corretamente no Z-API

### 2. "No active mentorship found for instance token"
**Causa:** O `instance_token` na mentorship não corresponde ao token que o Z-API está enviando, ou a mentorship está inativa  
**Solução:** 
- Verificar se o `instance_token` na mentorship é idêntico ao token que o Z-API envia (case-sensitive)
- Verificar se a mentorship está ativa (status = Active)
- Atualizar a mentorship com o `instance_token` correto

### 3. "Z-API Client-Token not configured"
**Causa:** Variável `ZApi__Client-Token` não está configurada no Render  
**Solução:** Adicionar variável de ambiente `ZApi__Client-Token` no Render com o token da **conta** Z-API (não da instância)

---

## 🔐 Segurança

- ✅ **Nunca** compartilhe o Client-Token ou instance_token publicamente
- ✅ **Client-Token**: Token da conta, usado para requisições que fazemos ao Z-API
- ✅ **instance_token**: Token da instância, validado contra a tabela `mentorships` em cada webhook
- ✅ Webhooks sem token válido ou sem mentorship correspondente são **rejeitados** (401 Unauthorized)
- ✅ O Z-API **envia automaticamente** o header `Z-Api-Token` - você não precisa configurar headers customizados
- ✅ Cada mentorship pode ter seu próprio `instance_token` (suporta múltiplas instâncias Z-API)

---

## 📚 Documentação Relacionada

- `docs/SECURITY_PRODUCTION_CHECKLIST.md` - Item #1: Webhook Authentication
- `docs/DEPLOYMENT_GUIDE.md` - Configuração de variáveis de ambiente

---

**Última atualização:** 2025-11-18

