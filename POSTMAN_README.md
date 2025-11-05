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
- **`mentoria_id`**: ID da mentoria (preenchido automaticamente ao criar uma mentoria)
- **`agent_session_id`**: ID da sessão (preenchido automaticamente ao criar uma sessão)

### Como usar

1. **Ajustar variáveis**:
   - Clique com botão direito na collection → **Edit**
   - Vá na aba **Variables**
   - Ajuste os valores conforme necessário

2. **Variáveis automáticas**:
   - Alguns requests têm scripts que preenchem automaticamente variáveis como `user_id`, `mentoria_id`, etc.
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

### Mentorias
- `GET /api/mentorias/{id}` - Buscar mentoria por ID
- `GET /api/mentorias/mentor/{mentorId}` - Listar mentorias de um mentor (paginação)
- `GET /api/mentorias/active` - Listar mentorias ativas (paginação)
- `POST /api/mentorias` - Criar mentoria
- `PUT /api/mentorias/{id}` - Atualizar mentoria
- `DELETE /api/mentorias/{id}` - Deletar mentoria (soft delete)

### Agent Sessions
- `GET /api/agentsessions/{id}` - Buscar sessão por ID
- `GET /api/agentsessions/user/{userId}/mentoria/{mentoriaId}` - Buscar sessão por user e mentoria
- `GET /api/agentsessions/user/{userId}/mentoria/{mentoriaId}/active` - Buscar sessão ativa
- `GET /api/agentsessions/user/{userId}` - Listar sessões de um usuário (paginação)
- `POST /api/agentsessions` - Criar sessão
- `PUT /api/agentsessions/{id}` - Atualizar sessão
- `POST /api/agentsessions/{id}/expire` - Expirar sessão
- `POST /api/agentsessions/{id}/pause` - Pausar sessão
- `POST /api/agentsessions/{id}/resume` - Retomar sessão

### WhatsApp Webhook
- `POST /api/WhatsAppWebhook?mentoriaId={id}` - Receber mensagem do WhatsApp

## 🔐 Autenticação

A autenticação por API Key está **opcional** (comentada nos controllers). Para habilitar:

1. Descomente `[Authorize]` nos controllers
2. Configure `ApiKey` no `appsettings.json`
3. Adicione o header `X-API-Key` nas requisições

As requisições já têm o header `X-API-Key` configurado (mas desabilitado por padrão).

## 🚀 Fluxo de Teste Recomendado

1. **Health Check**: Verifique se a API está rodando
2. **Criar Usuário (Mentor)**: Crie um usuário que será o mentor
3. **Criar Mentoria**: Use o ID do mentor criado
4. **Criar Usuário (Mentee)**: Crie outro usuário que será o mentorado
5. **Criar Agent Session**: Crie uma sessão ligando o mentorado à mentoria
6. **Testar WhatsApp Webhook**: Envie uma mensagem simulada

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

### Criar uma mentoria:

```json
POST /api/mentorias
{
    "mentorId": "{{mentor_id}}",
    "nome": "Nina - Descoberta de Oferta",
    "assistantId": "asst_YOUR_ASSISTANT_ID",
    "duracaoDias": 30,
    "descricao": "Programa de 30 dias"
}
```

### Testar WhatsApp Webhook:

```json
POST /api/WhatsAppWebhook?mentoriaId={{mentoria_id}}
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

