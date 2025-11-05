# Mentoragente

SaaS platform for mentors to create AI-powered WhatsApp assistants for their mentees. Multi-tenant architecture with OpenAI Assistants API, built with Clean Architecture in C# .NET 8.0.

## 🚀 Features

- 🤖 AI-powered WhatsApp assistants using OpenAI Assistants API
- 👥 Multi-tenant architecture (multiple mentors, multiple mentorias)
- 📱 WhatsApp integration via Evolution API
- 🎯 Structured mentorship programs with configurable duration
- 💾 Persistent conversation context (Thread persistence)
- 📊 Progress tracking and session management

## 🏗️ Architecture

Clean Architecture with 4 layers:

```
Mentoragente/
├── Mentoragente.Domain/          # Domain layer (entities, interfaces, enums)
├── Mentoragente.Application/     # Application layer (business logic)
├── Mentoragente.Infrastructure/  # Infrastructure layer (repositories, external services)
└── Mentoragente.API/             # API layer (controllers, configuration)
```

## 📋 Prerequisites

- .NET 8.0 SDK
- Supabase PostgreSQL database
- OpenAI API key
- Evolution API instance (WhatsApp)
- Docker (optional, for deployment)

## ⚙️ Configuration

### 1. Database Setup

Execute the SQL schema in Supabase:

```sql
-- 1. Run: source/Mentoragente/DATABASE_SCHEMA.sql
-- 2. Run: source/Mentoragente/ENABLE_RLS.sql (recommended for security)
```

**Important:** After creating the schema, enable Row Level Security (RLS) by running `ENABLE_RLS.sql`. This script:
- Enables RLS on all tables
- Creates policies for service role access
- Maintains security best practices

**Note:** Your backend uses ServiceRoleKey, which bypasses RLS automatically, so enabling RLS won't break your application. It's recommended for security and compliance.

### 2. Environment Variables

Configure in `appsettings.Development.json` or environment variables:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "BaseUrl": "https://api.openai.com/v1",
    "AssistantId": "your-assistant-id"
  },
  "EvolutionAPI": {
    "BaseUrl": "https://your-evolution-api.com",
    "ApiKey": "your-api-key",
    "InstanceName": "your-instance-name"
  },
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ServiceRoleKey": "your-service-role-key"
  }
}
```

### 3. Create Mentoria

Before using the webhook, create a mentoria in the database:

```sql
INSERT INTO mentorias (nome, mentor_id, assistant_id, duracao_dias, descricao)
VALUES (
    'Nina - Descoberta de Oferta de Mentoria',
    'mentor-user-id-here',
    'your-openai-assistant-id',
    30,
    'Programa de 30 dias para descobrir sua oferta única de mentoria'
);
```

## 🚀 Running Locally

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
cd Mentoragente.API
dotnet run
```

Access Swagger UI: `https://localhost:7000/swagger`

## 📡 Webhook Configuration

### Evolution API Webhook

Configure the webhook URL in Evolution API:

```
https://your-api.com/api/webhook?mentoriaId={MENTORIA_ID}
```

**Note:** Currently, `mentoriaId` must be provided via query parameter. This will be improved in future versions to support automatic detection.

### Example Webhook Payload

```json
{
  "event": "messages.upsert",
  "data": {
    "key": {
      "remoteJid": "5511999999999@s.whatsapp.net",
      "fromMe": false
    },
    "message": {
      "conversation": "Hello!"
    }
  }
}
```

## 🗄️ Database Schema

### Tables

- `users` - Pessoas físicas (phone_number como identificador único)
- `mentorias` - Cadastro de mentorias
- `agent_sessions` - Sessões de agentes (vincula User + Mentoria)
- `agent_session_data` - Dados da sessão (propriedades comuns)
- `conversations` - Histórico de mensagens

See `DATABASE_SCHEMA.sql` for complete schema.

## 🔄 Flow

1. **User sends WhatsApp message** → Evolution API webhook
2. **WhatsAppWebhookController** → Extracts phone number, finds mentoria
3. **MessageProcessor** → Creates/gets User, AgentSession, validates access
4. **OpenAI Assistant** → Processes message with Thread context
5. **Response sent** → Via Evolution API back to WhatsApp

## 📝 API Endpoints

- `POST /api/webhook?mentoriaId={guid}` - WhatsApp webhook
- `GET /health` - Health check
- `GET /swagger` - API documentation

## 🧪 Testing

```bash
# Run tests (when implemented)
dotnet test
```

## 🐳 Docker

```bash
# Build
docker build -t mentoragente .

# Run
docker run -p 8080:8080 mentoragente
```

## 📚 Project Structure

```
Mentoragente/
├── Mentoragente.Domain/          # Domain entities, interfaces, enums
│   ├── Entities/                 # User, Mentoria, AgentSession, etc.
│   ├── Interfaces/               # Repository and service interfaces
│   ├── Enums/                    # UserStatus, AgentSessionStatus, etc.
│   └── Models/                   # DTOs and models
├── Mentoragente.Application/     # Business logic
│   └── Services/                  # MessageProcessor
├── Mentoragente.Infrastructure/  # External integrations
│   ├── Repositories/             # Database access
│   └── Services/                 # OpenAI, Evolution API
└── Mentoragente.API/             # Web API
    └── Controllers/              # HTTP endpoints
```

## 🔧 Development

### Adding a New Mentoria

1. Create mentor user in `users` table
2. Insert mentoria in `mentorias` table
3. Configure webhook with `mentoriaId` query parameter

### Access Control

- Sessions expire based on `access_end_date` in `agent_session_data`
- Status automatically updated to `Expired` when access ends
- Context preserved (Thread ID persists)

## 📝 License

MIT License

---

**Status:** 🚧 In Development

**Repository:** https://github.com/deocarvalho/mentoragente

