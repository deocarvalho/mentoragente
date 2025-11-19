# Plano de Ação: Segurança e Preparação para Produção

**Data de Criação:** 2025-11-14  
**Status:** Em Progresso  
**Prioridade:** Crítico → Importante → Melhorias

---

## 📋 Resumo Executivo

Este documento lista todas as melhorias de segurança e preparação para produção, organizadas por prioridade. Cada item deve ser implementado e testado antes de marcar como concluído.

**Score Atual:**
- Segurança: 6/10
- Lógica: 8/10  
- Pronto para Produção: 5/10

---

## 🔴 CRÍTICO - Fazer ANTES de ir para produção

### 1. Autenticação de Webhooks ✅ **CONCLUÍDO (Z-API)**

**Problema:** Webhooks estavam completamente abertos - qualquer um poderia enviar webhooks falsos e processar mensagens.

**Impacto:** 
- Ataque poderia criar sessões falsas
- Ataque poderia consumir recursos (OpenAI, WhatsApp)
- Ataque poderia enviar mensagens para usuários

**Solução Implementada:**
- ✅ **Z-API**: Validação do `Client-Token` no header implementada
  - Validação no `ZApiWebhookController.cs` antes de processar o webhook
  - Retorna `401 Unauthorized` se o token estiver ausente ou inválido
  - Token é lido de `appsettings.json` (`ZApi:Client-Token`)
  - Documentado no Swagger com security scheme customizado
  - OperationFilter aplica o security requirement apenas ao endpoint do Z-API

**❓ Pendências:**
1. Evolution API: Verificar se envia algum header de autenticação
2. Evolution API: Implementar validação similar se disponível
3. IP Whitelist: Considerar como camada adicional de segurança (opcional)

**Arquivos Modificados:**
- ✅ `ZApiWebhookController.cs` - Validação do Client-Token
- ✅ `Program.cs` - Configuração do Swagger com security scheme
- ✅ `Filters/ZApiWebhookSecurityFilter.cs` - OperationFilter para aplicar security apenas ao Z-API
- ✅ `Mentoragente.API.csproj` - Adicionado `Swashbuckle.AspNetCore.Annotations`
- ✅ `Directory.Packages.props` - Adicionado `Swashbuckle.AspNetCore.Annotations`

**Status:** ✅ Z-API implementado | ⏳ Evolution API pendente

---

### 2. Habilitar Autenticação nos Endpoints Administrativos ✅ **CONCLUÍDO**

**Problema:** Endpoints críticos (Mentorships, Users, Enrollments) estavam sem autenticação.

**Impacto:**
- Qualquer um poderia criar/deletar mentorships
- Qualquer um poderia criar enrollments
- Qualquer um poderia ver dados de usuários

**Solução Implementada:**
- ✅ Adicionado `[Authorize]` em todos os controllers administrativos:
  - `MentorshipsController.cs`
  - `UsersController.cs`
  - `EnrollmentsController.cs`
  - `AgentSessionsController.cs`
- ✅ Configurado Swagger para mostrar o campo `X-API-Key` nos endpoints protegidos
- ✅ Criado `ApiKeySecurityFilter` para aplicar automaticamente a autenticação no Swagger

**Arquivos modificados:**
- `MentorshipsController.cs` - Adicionado `[Authorize]` e `using Microsoft.AspNetCore.Authorization`
- `UsersController.cs` - Adicionado `[Authorize]` e `using Microsoft.AspNetCore.Authorization`
- `EnrollmentsController.cs` - Adicionado `[Authorize]` e `using Microsoft.AspNetCore.Authorization`
- `AgentSessionsController.cs` - Adicionado `[Authorize]` e `using Microsoft.AspNetCore.Authorization`
- `Program.cs` - Adicionado security scheme "ApiKey" no Swagger
- `Filters/ApiKeySecurityFilter.cs` - Novo filtro para aplicar autenticação no Swagger

**Como usar:**
1. Configure a API Key no `appsettings.json`: `"ApiKey": "sua-chave-secreta-aqui"`
2. No Swagger, clique em "Authorize" e insira a API Key
3. No Postman, adicione o header: `X-API-Key: sua-chave-secreta-aqui`

**📚 Documentação:**
- **Diretrizes de API Key:** Veja `docs/API_KEY_GUIDELINES.md` para especificações, formato recomendado (64+ caracteres), e como gerar chaves seguras
- **Proteger Swagger UI:** Veja `docs/SWAGGER_AUTHENTICATION.md` para opções de autenticação no Swagger

---

### 3. Rate Limiting ✅ **CONCLUÍDO**

**Problema:** Sistema vulnerável a spam/DoS - sem limites de requisições.

**Impacto:**
- Spam de mensagens pode consumir OpenAI credits
- Spam de enrollments pode criar muitas sessões
- DoS pode derrubar o serviço

**Solução Implementada:**
- ✅ Implementado rate limiting usando ASP.NET Core Rate Limiting nativo (.NET 8)
- ✅ Limites configurados:
  - **Webhooks:** 500 req/min por IP (Z-API e Evolution API)
  - **Enrollment:** 5 req/hora por phone number (middleware customizado)
  - **Endpoints administrativos:** 10 req/min por API Key (Mentorships, Users, AgentSessions)

**Arquivos modificados:**
- `Program.cs` - Adicionado `AddRateLimiter` com políticas "WebhookPolicy" e "AdminPolicy"
- `Middleware/PhoneNumberRateLimitMiddleware.cs` - Novo middleware para rate limiting por phone number
- `Controllers/ZApiWebhookController.cs` - Adicionado `[EnableRateLimiting("WebhookPolicy")]`
- `Controllers/EvolutionWebhookController.cs` - Adicionado `[EnableRateLimiting("WebhookPolicy")]`
- `Controllers/MentorshipsController.cs` - Adicionado `[EnableRateLimiting("AdminPolicy")]`
- `Controllers/UsersController.cs` - Adicionado `[EnableRateLimiting("AdminPolicy")]`
- `Controllers/AgentSessionsController.cs` - Adicionado `[EnableRateLimiting("AdminPolicy")]`
- `appsettings.json` - Adicionada seção "RateLimiting" com documentação dos limites

**Como funciona:**
- **Webhooks:** Limite compartilhado por IP do remetente (Z-API/Evolution API)
- **Enrollment:** Limite individual por phone number (extraído do body da requisição)
- **Administrativos:** Limite individual por API Key (extraído do header X-API-Key)

**Respostas de rate limit:**
- Status Code: `429 Too Many Requests`
- Header: `Retry-After` (segundos até poder tentar novamente)
- Body: JSON com mensagem de erro e `retryAfter` em segundos

---

### 4. Validação de Mentorship no Enrollment ✅ **CONCLUÍDO**

**Problema:** `EnrollmentsController` não valida se mentorship existe/está ativo antes de criar sessão.

**Impacto:**
- Pode criar sessão para mentorship inexistente
- Pode criar sessão para mentorship inativo/arquivado

**Solução Implementada:**
- ✅ Adicionada validação no `EnrollmentsController` antes de criar sessão
- ✅ Verifica se mentorship existe (retorna `404 Not Found` se não existir)
- ✅ Verifica se mentorship está ativo (retorna `400 Bad Request` se inativo/arquivado)

**Arquivos modificados:**
- `EnrollmentsController.cs` - Adicionado `IMentorshipService` e validação de mentorship
- `EnrollmentsControllerTests.cs` - Adicionado mock de `IMentorshipService` e novos testes

**Validações implementadas:**
1. **Mentorship não encontrado:** Retorna `404 Not Found` com mensagem clara
2. **Mentorship inativo/arquivado:** Retorna `400 Bad Request` com status atual do mentorship

**Ordem de validação:**
1. Validação FluentValidation (request DTO)
2. **Validação de mentorship (existe e está ativo)** ← NOVO
3. Criação/atualização de usuário
4. Criação de sessão
5. Envio de mensagem de boas-vindas

---

### 5. Sanitização de Logs ✅ **CONCLUÍDO**

**Problema:** Logs podem conter dados sensíveis (tokens, credenciais, mensagens de usuários).

**Impacto:**
- Vazamento de dados sensíveis em logs
- Compliance/GDPR issues

**Solução Implementada:**
- ✅ Criado serviço `ILogSanitizer` para mascarar dados sensíveis
- ✅ Aplicado sanitização em todos os logs que contêm dados sensíveis
- ✅ Configurados níveis de log diferentes para dev/prod

**Arquivos modificados:**
- `LogSanitizer.cs` (novo) - Serviço de sanitização de logs
- `ZApiWebhookController.cs` - Sanitização de payloads, phone numbers, tokens e mensagens
- `EvolutionWebhookController.cs` - Sanitização de phone numbers e mensagens
- `EnrollmentsController.cs` - Sanitização de phone numbers
- `appsettings.json` - Configuração de log levels para produção
- `appsettings.Development.json` - Configuração de log levels para desenvolvimento

**Dados mascarados:**
1. **Phone Numbers:** Mantém primeiros 5 e últimos 4 dígitos (ex: "55119****9999")
2. **Tokens/API Keys:** Mantém primeiros 3 e últimos 3 caracteres (ex: "sk-***xyz")
3. **Emails:** Mascara username, mantém domínio (ex: "u***@example.com")
4. **JSON Payloads:** Remove campos sensíveis (token, apikey, password, secret, client-token, etc.)
5. **Mensagens de texto:** Truncadas para 100 caracteres (configurável)

**Log Levels configurados:**
- **Produção (`appsettings.json`):** Default: Information, Microsoft.AspNetCore: Warning
- **Desenvolvimento (`appsettings.Development.json`):** Default: Debug, Mentoragente: Debug

**Pontos de sanitização:**
- Webhook payloads (Z-API e Evolution API)
- Phone numbers em todos os logs
- Tokens (Client-Token) em logs de autenticação
- Mensagens de texto dos usuários
- Logs de enrollment

---

## 🟡 IMPORTANTE - Fazer em breve (próximas semanas)

### 6. Validação de Ownership no Enrollment ⚠️ **NÃO CRÍTICO NO MOMENTO**

**Problema:** Qualquer um pode criar enrollment para qualquer mentorship.

**Impacto:**
- Usuário pode se inscrever em mentorship sem autorização do mentor
- Possível abuso do sistema

**Status Atual:**
- ✅ Endpoint protegido por API Key (`[Authorize]`)
- ✅ Apenas administrador faz enrollment manualmente
- ⚠️ Se no futuro houver múltiplos mentores ou webhooks de plataforma, será necessário implementar validação de ownership

**Solução Futura (quando necessário):**
- Validar se o mentor autorizou o enrollment
- Opções:
  - Verificar se mentorship permite auto-enrollment
  - Requerer aprovação prévia do mentor
  - Validar token/voucher de compra
  - Webhook authentication para plataformas de pagamento
  - Idempotency para evitar duplicatas

**Nota:** Comentário adicionado no código (`EnrollmentsController.cs`) documentando as implementações futuras necessárias.

**Estimativa:** 2-4 horas (quando necessário)

---

### 7. Transação no Enrollment (Race Condition) ✅ **CONCLUÍDO**

**Problema:** Dois requests simultâneos podem criar duas sessões para o mesmo usuário/mentorship.

**Impacto:**
- Duplicação de sessões
- Inconsistência de dados

**Solução Implementada:**
- ✅ Aproveitamento da constraint única do banco (`idx_agent_sessions_unique_active`)
- ✅ Detecção de violação de constraint única no `AgentSessionRepository`
- ✅ Recuperação automática da sessão existente em caso de race condition
- ✅ Proteção também para `AgentSessionData` (verificação antes de criar)

**Arquivos modificados:**
- `AgentSessionRepository.cs` - Detecção e tratamento de violação de constraint única
- `AgentSessionService.cs` - Tratamento adicional de race conditions e proteção para `AgentSessionData`
- `EnrollmentsController.cs` - Comentários explicativos sobre proteção contra race conditions

**Como funciona:**
1. O banco de dados tem uma constraint única: `CREATE UNIQUE INDEX idx_agent_sessions_unique_active ON agent_sessions(user_id, mentorship_id) WHERE status = 'Active'`
2. Se dois requests chegarem simultaneamente, o segundo tentará inserir e receberá uma violação de constraint única
3. O repository detecta a violação (código 23505 ou mensagem contendo "duplicate key"/"unique constraint")
4. Automaticamente busca e retorna a sessão existente criada pelo primeiro request
5. O mesmo tratamento é aplicado para `AgentSessionData` (verificação antes de criar)

**Vantagens:**
- Não requer locks ou transações explícitas
- Aproveita a constraint do banco de dados (mais eficiente)
- Recuperação automática sem erro para o usuário
- Funciona mesmo em cenários de alta concorrência

---

### 8. Health Checks Robustos ✅ **CONCLUÍDO**

**Problema:** Health check atual é básico - não verifica dependências.

**Impacto:**
- Não detecta problemas com banco de dados
- Não detecta problemas com APIs externas
- Monitoramento inadequado

**Solução Implementada:**
- ✅ Health checks customizados para todas as dependências:
  - **Supabase/Database** - Verifica conectividade e capacidade de query
  - **OpenAI API** - Verifica disponibilidade e validade da API key
  - **Z-API** - Verifica se o serviço está acessível
  - **Evolution API** - Verifica se o serviço está acessível
- ✅ Endpoints de health check:
  - `/health` - Health check geral (todos os checks)
  - `/health/live` - Liveness probe (apenas verifica se a aplicação está rodando)
  - `/health/ready` - Readiness probe (verifica todas as dependências críticas)
- ✅ Response formatado em JSON com detalhes de cada check

**Arquivos criados/modificados:**
- `HealthChecks/SupabaseHealthCheck.cs` (novo) - Health check para Supabase
- `HealthChecks/OpenAIHealthCheck.cs` (novo) - Health check para OpenAI
- `HealthChecks/ZApiHealthCheck.cs` (novo) - Health check para Z-API
- `HealthChecks/EvolutionApiHealthCheck.cs` (novo) - Health check para Evolution API
- `HealthChecks/HealthCheckExtensions.cs` (novo) - Extensão para registrar todos os health checks
- `Program.cs` - Configuração dos endpoints de health check

**Como funciona:**
1. **Liveness (`/health/live`)**: Retorna healthy se a aplicação estiver rodando (sem verificar dependências)
2. **Readiness (`/health/ready`)**: Verifica todas as dependências marcadas com tag "ready"
3. **Overall (`/health`)**: Executa todos os health checks

**Status dos checks:**
- `Healthy` - Serviço está funcionando corretamente
- `Degraded` - Serviço está acessível mas com problemas (ex: API key inválida)
- `Unhealthy` - Serviço está inacessível ou com falha crítica

**Uso:**
- Load balancers podem usar `/health/live` para verificar se a instância está rodando
- Kubernetes pode usar `/health/ready` para verificar se a aplicação está pronta para receber tráfego
- Monitoramento pode usar `/health` para verificar o status completo do sistema

---

### 9. Monitoramento e Alertas 📌 **PENDENTE - Application Insights Planejado**

**Problema:** Falta de visibilidade sobre saúde do sistema em produção.

**Impacto:**
- Problemas não detectados rapidamente
- Sem métricas para otimização

**Status Atual:**
- ✅ Logs básicos do Render disponíveis
- 📌 **Application Insights planejado para implementação futura**
- ⚠️ Alertas automáticos não configurados ainda

**Solução Planejada:**
- Application Insights (sem necessidade de migrar para Azure)
- Configurar alertas para:
  - Taxa de erro alta
  - Latência alta
  - Falhas de webhook
- Logging estruturado (Serilog) opcional para melhor análise

**Nota:** Application Insights pode ser usado com aplicação no Render, apenas enviando telemetria para o Azure. Free tier: 5GB/mês.

**Estimativa:** 3-4 horas (quando implementar)

---

### 10. Configurações de Produção ✅ **CONCLUÍDO**

**Problema:** Swagger exposto, CORS não configurado, etc.

**Impacto:**
- Exposição de API documentation
- Possíveis problemas de CORS

**Solução Implementada:**
- ✅ Swagger desabilitado em produção (apenas Development e Staging)
- ✅ CORS configurado adequadamente:
  - **Development:** Permite todas as origens (para facilitar desenvolvimento)
  - **Production:** Restrito a origens específicas configuradas em `Cors:AllowedOrigins`
  - Se nenhuma origem for configurada, bloqueia todas (mais seguro)
- ✅ `appsettings.Production.json` criado com configurações de produção
- ✅ Log levels ajustados para produção (Warning por padrão, Information para Mentoragente)

**Arquivos criados/modificados:**
- `appsettings.Production.json` (novo) - Configurações específicas de produção
- `appsettings.json` - Adicionada seção `Cors:AllowedOrigins`
- `appsettings.Development.json` - Adicionada seção `Cors:AllowedOrigins`
- `Program.cs` - Configuração de CORS e Swagger condicional

**Configurações de Produção:**
1. **Swagger:** Desabilitado automaticamente em produção
2. **CORS:** Configurável via `Cors:AllowedOrigins` no `appsettings.Production.json`
   - Para permitir origens específicas, adicione: `"AllowedOrigins": ["https://seu-frontend.com"]`
   - Array vazio = bloqueia todas as origens (mais seguro)
3. **Log Levels:** 
   - Default: Warning (reduz ruído)
   - Mentoragente: Information (mantém logs importantes)
4. **AllowedHosts:** `*` (pode ser restrito se necessário)

**Como configurar CORS em produção:**
No `appsettings.Production.json` ou variáveis de ambiente no Render:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://seu-frontend.com",
      "https://www.seu-frontend.com"
    ]
  }
}
```

**Nota:** Se você não tiver um frontend que precise acessar a API via CORS, deixe o array vazio para máxima segurança.

---

## 🟢 MELHORIAS - Opcional (fazer quando tiver tempo)

### 11. Retry Policies Mais Inteligentes
- Já existe, mas pode ser otimizado
- Adicionar exponential backoff com jitter

### 12. Circuit Breaker
- Implementar circuit breaker para APIs externas
- Prevenir cascata de falhas

### 13. Cache de Validações
- Cache de validações frequentes (ex: mentorship ativo)

### 14. Background Jobs
- Limpeza automática de dados antigos
- Processamento assíncrono de tarefas pesadas

---

## 📊 Progresso

- [ ] 1. Autenticação de Webhooks ⚠️ **AGUARDANDO ESCLARECIMENTO**
- [ ] 2. Habilitar Autenticação nos Endpoints
- [ ] 3. Rate Limiting ⚠️ **AGUARDANDO DECISÃO**
- [ ] 4. Validação de Mentorship no Enrollment
- [ ] 5. Sanitização de Logs ⚠️ **AGUARDANDO DECISÃO**
- [ ] 6. Validação de Ownership ⚠️ **AGUARDANDO ESCLARECIMENTO**
- [ ] 7. Transação no Enrollment
- [ ] 8. Health Checks Robustos
- [ ] 9. Monitoramento e Alertas ⚠️ **AGUARDANDO DECISÃO**
- [ ] 10. Configurações de Produção

---

## 🚀 Ordem de Implementação Recomendada

1. **Fase 1 (Crítico - Esta Semana):**
   - Item 2 (Autenticação endpoints) - Mais rápido
   - Item 4 (Validação mentorship) - Rápido
   - Item 1 (Autenticação webhooks) - Após esclarecimento
   - Item 3 (Rate limiting) - Após decisão
   - Item 5 (Sanitização logs) - Após decisão

2. **Fase 2 (Importante - Próximas 2 Semanas):**
   - Item 7 (Transação)
   - Item 8 (Health checks)
   - Item 10 (Config produção)
   - Item 6 (Ownership) - Após esclarecimento
   - Item 9 (Monitoramento) - Após decisão

3. **Fase 3 (Melhorias - Quando possível):**
   - Itens 11-14

---

## ❓ Perguntas Pendentes

### Precisa de Resposta Urgente:

1. **Autenticação de Webhooks:**
   - Z-API e Evolution API enviam algum header de autenticação?
   - Eles permitem configurar IP whitelist?
   - Qual método você prefere? (IP whitelist, token, ou HMAC)

2. **Rate Limiting:**
   - Prefere biblioteca ou middleware customizado?
   - Os limites sugeridos fazem sentido?

3. **Sanitização de Logs:**
   - Quais dados devem ser mascarados? (tokens, phone numbers, mensagens?)

4. **Validação de Ownership:**
   - Como funciona o fluxo de compra/enrollment?
   - Mentor precisa aprovar ou é automático após compra?

5. **Monitoramento:**
   - Qual sistema prefere? (Render logs, Application Insights, etc.)

---

## 📝 Notas

- Todos os itens marcados com ⚠️ precisam de esclarecimento antes de implementar
- Após cada implementação, testar e atualizar este documento
- Manter este documento atualizado com progresso

---

**Última Atualização:** 2025-11-14

