# Mentoragente

SaaS platform for mentors to create AI-powered WhatsApp assistants for their mentees. Multi-tenant architecture with OpenAI Assistants API, built with Clean Architecture in C# .NET 8.0.

## 🚀 Features

- 🤖 AI-powered WhatsApp assistants using OpenAI Assistants API
- 👥 Multi-tenant architecture (multiple mentors, multiple mentorships)
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
- Evolution API instance (WhatsApp) - **Recommended: v2.1.1**
- Docker (optional, for deployment)

## ⚙️ Configuration

### 1. Database Setup

Execute the SQL schema in Supabase:

```sql
-- Run: source/Mentoragente/DATABASE_SCHEMA.sql
```

**Important:** This single SQL file includes:
- Complete database schema (tables, indexes, triggers)
- Row Level Security (RLS) enabled on all tables
- Security policies for service role access

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
    "BaseUrl": "https://your-evolution-api.com"
  },
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ServiceRoleKey": "your-service-role-key"
  }
}
```

### 3. Create Mentorship

Before using the webhook, create a mentorship in the database:

```sql
INSERT INTO mentorships (name, mentor_id, assistant_id, duration_days, description, evolution_api_key, evolution_instance_name)
VALUES (
    'Nina - Mentorship Offer Discovery',
    'mentor-user-id-here',
    'your-openai-assistant-id',
    30,
    '30-day program to discover your unique mentorship offer',
    'your-evolution-api-key',
    'your-instance-name'
);
```

**Note:** Each mentorship now requires `evolution_api_key` and `evolution_instance_name` to be configured. These are stored in the database and can be updated without redeploying the application.

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
- `mentorships` - Mentorship programs
- `agent_sessions` - Agent sessions (links User + Mentorship)
- `agent_session_data` - Dados da sessão (propriedades comuns)
- `conversations` - Histórico de mensagens

See `DATABASE_SCHEMA.sql` for complete schema.

## 🔄 Flow

1. **User sends WhatsApp message** → Evolution API webhook
2. **WhatsAppWebhookController** → Extracts phone number, finds mentorship
3. **MessageProcessor** → Creates/gets User, AgentSession, validates access
4. **OpenAI Assistant** → Processes message with Thread context
5. **Response sent** → Via Evolution API back to WhatsApp

## 📝 API Endpoints

- `POST /api/webhook?mentorshipId={guid}` - WhatsApp webhook
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

## 🚀 Render Deployment

See [RENDER_DEPLOYMENT.md](./RENDER_DEPLOYMENT.md) for complete deployment guide.

**Quick Start:**
- **Evolution API:** Use Docker image `atendai/evolution-api:v2.1.1` (latest stable)
- **Mentoragente API:** Deploy using `Mentoragente.API/Dockerfile`
- Configure environment variables as documented

## 📚 Project Structure

```
Mentoragente/
├── Mentoragente.Domain/          # Domain entities, interfaces, enums
│   ├── Entities/                 # User, Mentorship, AgentSession, etc.
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

