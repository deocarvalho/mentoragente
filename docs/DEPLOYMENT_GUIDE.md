# 🚀 Guia de Deploy - Mentoragente

Este guia cobre o processo completo de deploy para os ambientes de **Development (HMG)**, **Staging** e **Production (PRD)**.

---

## 📋 Checklist de Deploy

### ✅ Antes de começar

- [ ] Código testado localmente
- [ ] Build passando sem erros
- [ ] Todas as migrations aplicadas localmente
- [ ] Variáveis de ambiente documentadas
- [ ] Backup do banco de produção (se aplicável)

---

## 1️⃣ Atualizar Schema do Banco de Dados

### Para criar bancos novos (PRD e HMG)

#### Opção A: Usando o Schema Completo (Banco novo/vazio)

1. **Criar projeto no Supabase:**
   - Acesse [supabase.com](https://supabase.com)
   - Crie um novo projeto para cada ambiente:
     - **HMG (Development)**: `mentoragente-hmg`
     - **PRD (Production)**: `mentoragente-prd`

2. **Executar o schema completo:**
   - No Supabase Dashboard → SQL Editor
   - Execute o arquivo `DATABASE_SCHEMA.sql` completo
   - Isso cria todas as tabelas, índices, triggers e RLS

3. **Aplicar migrations (se necessário):**
   - Execute `docs/migrations/001_add_whatsapp_provider_support.sql`
   - Execute `docs/migrations/002_add_instance_token.sql`

#### Opção B: Usando apenas Migrations (Banco existente)

Se você já tem um banco e quer atualizar:

1. Execute as migrations na ordem:
   ```sql
   -- Migration 001
   -- docs/migrations/001_add_whatsapp_provider_support.sql
   
   -- Migration 002
   -- docs/migrations/002_add_instance_token.sql
   ```

### Verificar Schema

Após executar, verifique se todas as tabelas foram criadas:

```sql
-- Verificar tabelas
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_type = 'BASE TABLE'
ORDER BY table_name;

-- Verificar se as colunas novas existem
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'mentorships'
ORDER BY ordinal_position;
```

**Colunas esperadas em `mentorships`:**
- `whatsapp_provider` (enum: ZApi, EvolutionAPI, OfficialWhatsApp)
- `instance_code` (VARCHAR)
- `instance_token` (VARCHAR)
- ❌ **NÃO deve ter:** `evolution_api_key` ou `evolution_instance_name` (removidas na migration 001)

---

## 2️⃣ Deploy: Development → Production (Render)

### Passo 1: Verificar Branch de Development

```bash
# No seu repositório local
git checkout development
git pull origin development

# Verificar se está tudo commitado
git status
```

### Passo 2: Fazer Merge para Main/Master

```bash
# Mudar para branch principal
git checkout main  # ou master

# Atualizar
git pull origin main

# Fazer merge de development
git merge development

# Resolver conflitos se houver
# ...

# Push para GitHub
git push origin main
```

### Passo 3: Atualizar Variáveis de Ambiente no Render (Produção)

1. **Acesse o Render Dashboard:**
   - Vá para seu serviço de produção
   - Settings → Environment

2. **Atualizar variáveis críticas:**

   **Obrigatórias:**
   ```
   ASPNETCORE_ENVIRONMENT=Production
   
   Supabase__Url=https://seu-projeto-prd.supabase.co
   Supabase__ServiceRoleKey=eyJ... (Service Role Key do PRD)
   
   OpenAI__ApiKey=sk-...
   OpenAI__BaseUrl=https://api.openai.com/v1/
   
   ZApi__BaseUrl=https://api.z-api.io
   ZApi__Client-Token=seu-token-zapi-prd
   
   ApiKey=seu-api-key-producao (gerar novo se necessário)
   ```

   **Opcionais (se usar Evolution API):**
   ```
   EvolutionAPI__BaseUrl=https://...
   EvolutionAPI__ApiKey=...
   ```

   **CORS (se tiver frontend):**
   ```
   Cors__AllowedOrigins__0=https://seu-frontend.com
   Cors__AllowedOrigins__1=https://www.seu-frontend.com
   ```

3. **Salvar e fazer Deploy:**
   - Clique em "Save Changes"
   - Render fará deploy automático ou você pode forçar manualmente

### Passo 4: Verificar Deploy

1. **Health Check:**
   ```bash
   curl https://seu-app-prd.onrender.com/health
   ```

2. **Verificar logs:**
   - Render Dashboard → Logs
   - Procure por erros de inicialização

3. **Testar endpoints críticos:**
   - Health checks: `/health`, `/health/ready`
   - Webhook (se configurado): Teste com Z-API

---

## 3️⃣ Deploy: Local → Development (GitHub)

### Passo 1: Verificar Estado Local

```bash
# Verificar status
git status

# Ver mudanças
git diff

# Verificar build
dotnet build
```

### Passo 2: Commit das Mudanças

```bash
# Adicionar todos os arquivos modificados
git add .

# Ou adicionar arquivos específicos
git add source/Mentoragente/

# Commit com mensagem descritiva
git commit -m "feat: implement security checklist items 1-10

- Add webhook authentication (Client-Token validation)
- Add API Key authentication for administrative endpoints
- Implement rate limiting (webhooks, enrollment, admin)
- Add mentorship validation in enrollment
- Implement log sanitization
- Add race condition protection
- Add comprehensive health checks
- Configure production settings (CORS, Swagger)
- Update database schema with migrations"

# Verificar commits
git log --oneline -5
```

### Passo 3: Push para Development

```bash
# Verificar branch atual
git branch

# Se não estiver em development, mudar
git checkout development

# Push para GitHub
git push origin development
```

### Passo 4: Atualizar Variáveis de Ambiente no Render (Development/HMG)

1. **Acesse o serviço de Development no Render**

2. **Atualizar variáveis:**
   ```
   ASPNETCORE_ENVIRONMENT=Development
   
   Supabase__Url=https://seu-projeto-hmg.supabase.co
   Supabase__ServiceRoleKey=eyJ... (Service Role Key do HMG)
   
   OpenAI__ApiKey=sk-... (pode ser a mesma ou diferente)
   ZApi__Client-Token=seu-token-zapi-hmg
   
   ApiKey=dev-... (API Key de desenvolvimento)
   ```

3. **Render fará deploy automático** quando detectar push na branch `development`

### Passo 5: Verificar Deploy de Development

```bash
# Health check
curl https://seu-app-hmg.onrender.com/health

# Swagger deve estar disponível em Development
curl https://seu-app-hmg.onrender.com/swagger
```

---

## 📝 Checklist de Variáveis de Ambiente

### Development (HMG)
- [ ] `ASPNETCORE_ENVIRONMENT=Development`
- [ ] `Supabase__Url` (HMG)
- [ ] `Supabase__ServiceRoleKey` (HMG)
- [ ] `OpenAI__ApiKey`
- [ ] `ZApi__BaseUrl`
- [ ] `ZApi__Client-Token` (HMG)
- [ ] `ApiKey` (dev-...)
- [ ] `Cors__AllowedOrigins` (se necessário)

### Production (PRD)
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `Supabase__Url` (PRD)
- [ ] `Supabase__ServiceRoleKey` (PRD)
- [ ] `OpenAI__ApiKey`
- [ ] `ZApi__BaseUrl`
- [ ] `ZApi__Client-Token` (PRD)
- [ ] `ApiKey` (produção - gerar novo)
- [ ] `Cors__AllowedOrigins` (se necessário)

---

## 🔍 Verificações Pós-Deploy

### 1. Health Checks

```bash
# Liveness
curl https://seu-app.onrender.com/health/live

# Readiness (verifica dependências)
curl https://seu-app.onrender.com/health/ready

# Overall
curl https://seu-app.onrender.com/health
```

### 2. Swagger (apenas Development/Staging)

```bash
# Development: deve funcionar
curl https://seu-app-hmg.onrender.com/swagger

# Production: deve retornar 404
curl https://seu-app-prd.onrender.com/swagger
```

### 3. Autenticação

```bash
# Testar endpoint administrativo sem API Key (deve falhar)
curl -X GET https://seu-app.onrender.com/api/mentorships

# Testar com API Key (deve funcionar)
curl -X GET https://seu-app.onrender.com/api/mentorships \
  -H "X-API-Key: sua-api-key"
```

### 4. Webhook (Z-API)

```bash
# Testar webhook sem Client-Token (deve falhar)
curl -X POST https://seu-app.onrender.com/api/webhooks/zapi \
  -H "Content-Type: application/json" \
  -d '{"phone": "5511999999999", "text": {"message": "test"}}'

# Testar com Client-Token (deve funcionar)
curl -X POST https://seu-app.onrender.com/api/webhooks/zapi \
  -H "Content-Type: application/json" \
  -H "Client-Token: seu-client-token" \
  -d '{"phone": "5511999999999", "text": {"message": "test"}}'
```

---

## ⚠️ Pontos de Atenção

### 1. Banco de Dados Separado

✅ **IMPORTANTE:** Use bancos de dados **separados** para HMG e PRD:
- **HMG**: Dados de teste, pode ser resetado
- **PRD**: Dados reais de clientes, **NUNCA** resetar sem backup

### 2. API Keys Diferentes

- Use API Keys **diferentes** para cada ambiente
- **NUNCA** use a API Key de produção em desenvolvimento
- Gere novas API Keys seguindo `docs/API_KEY_GUIDELINES.md`

### 3. Client-Token Z-API

- Cada ambiente pode ter seu próprio Client-Token
- Ou usar o mesmo se for a mesma instância Z-API
- Configure no Render conforme necessário

### 4. Logs

- **Development**: Logs mais verbosos (Debug)
- **Production**: Logs mais restritivos (Warning/Information)
- Verifique logs no Render Dashboard após deploy

---

## 🆘 Troubleshooting

### Erro: "Supabase connection failed"

**Causa:** ServiceRoleKey incorreto ou banco não existe

**Solução:**
1. Verificar ServiceRoleKey no Supabase Dashboard → Settings → API
2. Verificar se o projeto está ativo (não pausado)
3. Verificar URL do Supabase

### Erro: "Swagger ainda aparece em produção"

**Causa:** `ASPNETCORE_ENVIRONMENT` não está como "Production"

**Solução:**
1. Verificar variável de ambiente no Render
2. Reiniciar o serviço

### Erro: "CORS bloqueando requests"

**Causa:** `Cors:AllowedOrigins` não configurado ou incorreto

**Solução:**
1. Adicionar origem no `appsettings.Production.json` ou variável de ambiente
2. Formato: `Cors__AllowedOrigins__0=https://seu-frontend.com`

### Erro: "Health check falhando"

**Causa:** Dependências (Supabase, OpenAI, etc.) inacessíveis

**Solução:**
1. Verificar `/health/ready` para ver qual check está falhando
2. Verificar credenciais/configurações
3. Verificar conectividade de rede

---

## 📚 Documentação Relacionada

- `SECURITY_PRODUCTION_CHECKLIST.md` - Checklist de segurança
- `API_KEY_GUIDELINES.md` - Guia de API Keys
- `DATABASE_SCHEMA.sql` - Schema completo do banco
- `docs/migrations/` - Migrations do banco de dados

---

## ✅ Checklist Final

Antes de considerar o deploy completo:

- [ ] Schema aplicado em HMG e PRD
- [ ] Variáveis de ambiente configuradas em ambos os ambientes
- [ ] Health checks passando
- [ ] Swagger desabilitado em produção
- [ ] Autenticação funcionando (API Key e Client-Token)
- [ ] Rate limiting funcionando
- [ ] Logs sendo gerados corretamente
- [ ] Testes básicos de endpoints realizados

---

**Última atualização:** 2025-01-15

