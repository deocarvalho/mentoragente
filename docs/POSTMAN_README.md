# Postman Collection - Mentoragente API

Esta collection do Postman contém todos os endpoints da API Mentoragente para facilitar testes e desenvolvimento.

## 📥 Importação

1. Abra o Postman
2. Clique em **Import**
3. Selecione o arquivo `Mentoragente.postman_collection.json`
4. A collection será importada com todas as requisições organizadas

## ⚙️ Configuração de Variáveis

### Variáveis da Collection

A collection já vem com variáveis pré-configuradas que você pode ajustar:

- **`base_url`**: URL base da API (padrão: `http://localhost:5000`)
- **`api_key`**: Chave da API para autenticação (opcional, se habilitar `[Authorize]`)
- **`user_id`**: ID do usuário (preenchido automaticamente ao criar um usuário)
- **`mentor_id`**: ID do mentor (use o mesmo `user_id` se for mentor)
- **`mentorship_id`**: ID da mentorship (preenchido automaticamente ao criar uma mentorship)
- **`agent_session_id`**: ID da sessão (preenchido automaticamente ao criar uma sessão)

### Como usar

1. **Ajustar variáveis**:
   - Clique com botão direito na collection → **Edit**
   - Vá na aba **Variables**
   - Ajuste os valores conforme necessário

2. **Variáveis automáticas**:
   - Alguns requests têm scripts que preenchem automaticamente variáveis como `user_id`, `mentorship_id`, etc.
   - Execute os requests de criação primeiro para popular essas variáveis

## 📋 Endpoints Disponíveis

### Health
- `GET /health` - Health check da API

### Users
- `GET /api/users` - Listar usuários (paginação)
- `GET /api/users/{id}` - Buscar usuário por ID
- `GET /api/users/phone/{phoneNumber}` - Buscar usuário por telefone
- `POST /api/users` - Criar usuário
- `PUT /api/users/{id}` - Atualizar usuário
- `DELETE /api/users/{id}` - Deletar usuário (soft delete)

### Mentorships
- `GET /api/mentorships/{id}` - Get mentorship by ID
- `GET /api/mentorships/mentor/{mentorId}` - List mentorships for a mentor (paginated)
- `GET /api/mentorships/active` - List active mentorships (paginated)
- `POST /api/mentorships` - Create mentorship
- `PUT /api/mentorships/{id}` - Update mentorship
- `DELETE /api/mentorships/{id}` - Delete mentorship (soft delete)

### Agent Sessions
- `GET /api/agentsessions/{id}` - Get session by ID
- `GET /api/agentsessions/user/{userId}/mentorship/{mentorshipId}` - Get session by user and mentorship
- `GET /api/agentsessions/user/{userId}/mentorship/{mentorshipId}/active` - Get active session
- `GET /api/agentsessions/user/{userId}` - Listar sessões de um usuário (paginação)
- `POST /api/agentsessions` - Criar sessão
- `PUT /api/agentsessions/{id}` - Atualizar sessão
- `POST /api/agentsessions/{id}/expire` - Expirar sessão
- `POST /api/agentsessions/{id}/pause` - Pausar sessão
- `POST /api/agentsessions/{id}/resume` - Retomar sessão

### WhatsApp Webhook
- `POST /api/WhatsAppWebhook?mentorshipId={id}` - Receive WhatsApp message

## 🔐 Autenticação

A autenticação por API Key está **opcional** (comentada nos controllers). Para habilitar:

1. Descomente `[Authorize]` nos controllers
2. Configure `ApiKey` no `appsettings.json`
3. Adicione o header `X-API-Key` nas requisições

As requisições já têm o header `X-API-Key` configurado (mas desabilitado por padrão).

## 🚀 Fluxo de Teste Recomendado

1. **Health Check**: Verify API is running
2. **Create User (Mentor)**: Create a user who will be the mentor
3. **Create Mentorship**: Use the created mentor ID
4. **Create User (Mentee)**: Create another user who will be the mentee
5. **Create Agent Session**: Create a session linking the mentee to the mentorship
6. **Test WhatsApp Webhook**: Send a simulated message

## 📝 Exemplos de Uso

### Criar um usuário mentor:

```json
POST /api/users
{
    "phoneNumber": "5511999999999",
    "name": "Paula",
    "email": "paula@example.com"
}
```

### Create a mentorship:

```json
POST /api/mentorships
{
    "mentorId": "{{mentor_id}}",
    "name": "Nina - Mentorship Offer Discovery",
    "assistantId": "asst_YOUR_ASSISTANT_ID",
    "durationDays": 30,
    "description": "30-day program",
    "evolutionApiKey": "YOUR_EVOLUTION_API_KEY",
    "evolutionInstanceName": "YOUR_INSTANCE_NAME"
}
```

### Test WhatsApp Webhook:

```json
POST /api/WhatsAppWebhook?mentorshipId={{mentorship_id}}
{
    "event": "messages.upsert",
    "data": {
        "key": {
            "remoteJid": "5511888888888@s.whatsapp.net",
            "fromMe": false
        },
        "message": {
            "conversation": "Olá!"
        }
    }
}
```

## 🔍 Validação

Todos os endpoints de criação/atualização têm validação via FluentValidation:
- Phone numbers: apenas dígitos, 10-15 caracteres
- Nomes: mínimo 2 caracteres, máximo 100
- Emails: formato válido (quando fornecido)
- Status: valores válidos do enum

## 📊 Paginação

Endpoints de listagem suportam paginação:
- `page`: Número da página (padrão: 1)
- `pageSize`: Itens por página (padrão: 10, máximo: 100)

Exemplo de resposta paginada:
```json
{
    "users": [...],
    "total": 50,
    "page": 1,
    "pageSize": 10,
    "totalPages": 5
}
```

---

**Nota**: Certifique-se de que a API está rodando antes de testar. A URL padrão é `http://localhost:5000`, mas você pode ajustar na variável `base_url`.

