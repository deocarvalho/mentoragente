# 🚀 Mentoragente - Implementation Plan

## 📋 Visão Geral

Implementação passo a passo do projeto Mentoragente, organizada em fases para não perder o foco.

---

## 🎯 Fase 1: Foundation (Enums e Estrutura Base)

### 1.1 Criar Enums
- [ ] `UserStatus` (Active, Inactive, Blocked)
- [ ] `AgentSessionStatus` (Active, Expired, Paused, Completed)
- [ ] `AIProvider` (OpenAI)
- [ ] `MentoriaStatus` (Active, Inactive, Archived)

**Localização:** `Mentoragente.Domain/Enums/`

---

## 🎯 Fase 2: Domain Layer (Entidades)

### 2.1 Criar Entidade User
- [ ] `User.cs` com propriedades básicas
- [ ] Id, PhoneNumber (UNIQUE), Name, Email, Status
- [ ] CreatedAt, UpdatedAt

### 2.2 Criar Entidade Mentoria
- [ ] `Mentoria.cs` com propriedades básicas
- [ ] Id, Nome, MentorId, AssistantId, DuracaoDias, Status
- [ ] CreatedAt, UpdatedAt

### 2.3 Criar Entidade AgentSession
- [ ] `AgentSession.cs` com propriedades básicas
- [ ] Id, UserId, MentoriaId, AIProvider, AIContextId, Status
- [ ] LastInteraction, TotalMessages, CreatedAt, UpdatedAt

### 2.4 Criar Entidade AgentSessionData
- [ ] `AgentSessionData.cs` com propriedades comuns
- [ ] AgentSessionId, AccessStartDate, AccessEndDate
- [ ] ProgressPercentage, ReportGenerated, ReportGeneratedAt
- [ ] AdminNotes, CustomPropertiesJson

### 2.5 Atualizar Entidade Conversation
- [ ] `Conversation.cs` atualizado
- [ ] Trocar `CustomerId` → `AgentSessionId`
- [ ] Manter outras propriedades

**Localização:** `Mentoragente.Domain/Entities/`

---

## 🎯 Fase 3: Domain Layer (Interfaces)

### 3.1 Criar Interfaces de Repositório
- [ ] `IUserRepository`
- [ ] `IMentoriaRepository`
- [ ] `IAgentSessionRepository`
- [ ] `IAgentSessionDataRepository`
- [ ] Atualizar `IConversationRepository`

**Localização:** `Mentoragente.Domain/Interfaces/`

---

## 🎯 Fase 4: Infrastructure Layer (Repositórios)

### 4.1 Implementar UserRepository
- [ ] `GetUserByPhoneAsync(string phoneNumber)`
- [ ] `CreateUserAsync(User user)`
- [ ] `UpdateUserAsync(User user)`
- [ ] `GetUserByIdAsync(Guid id)`

### 4.2 Implementar MentoriaRepository
- [ ] `GetMentoriaByIdAsync(Guid id)`
- [ ] `GetMentoriasByMentorIdAsync(Guid mentorId)`
- [ ] `CreateMentoriaAsync(Mentoria mentoria)`
- [ ] `UpdateMentoriaAsync(Mentoria mentoria)`

### 4.3 Implementar AgentSessionRepository
- [ ] `GetAgentSessionAsync(Guid userId, Guid mentoriaId)`
- [ ] `GetActiveAgentSessionAsync(Guid userId, Guid mentoriaId)`
- [ ] `CreateAgentSessionAsync(AgentSession session)`
- [ ] `UpdateAgentSessionAsync(AgentSession session)`
- [ ] `GetAgentSessionByIdAsync(Guid id)`

### 4.4 Implementar AgentSessionDataRepository
- [ ] `GetAgentSessionDataAsync(Guid agentSessionId)`
- [ ] `CreateAgentSessionDataAsync(AgentSessionData data)`
- [ ] `UpdateAgentSessionDataAsync(AgentSessionData data)`

### 4.5 Atualizar ConversationRepository
- [ ] Trocar todas as referências de `CustomerId` → `AgentSessionId`
- [ ] Atualizar métodos para usar nova estrutura

**Localização:** `Mentoragente.Infrastructure/Repositories/`

---

## 🎯 Fase 5: Application Layer (Services)

### 5.1 Atualizar MessageProcessor
- [ ] Refatorar para usar nova estrutura (User → AgentSession)
- [ ] Implementar validação de acesso (AccessEndDate)
- [ ] Implementar renovação de acesso
- [ ] Remover referências a outras IAs (apenas OpenAI)

### 5.2 Criar/Atualizar Helpers
- [ ] Validação de acesso por mentoria
- [ ] Criação automática de sessão se não existir

**Localização:** `Mentoragente.Application/Services/`

---

## 🎯 Fase 6: API Layer (Controllers)

### 6.1 Atualizar WhatsAppWebhookController
- [ ] Refatorar para usar nova estrutura
- [ ] Normalizar phone number no controller
- [ ] Buscar/crear User
- [ ] Buscar/crear AgentSession
- [ ] Validar acesso
- [ ] Processar mensagem

### 6.2 Criar MentoriaController (opcional - para admin)
- [ ] Endpoints básicos de CRUD de mentorias
- [ ] Apenas se necessário para admin

**Localização:** `Mentoragente.API/Controllers/`

---

## 🎯 Fase 7: Configuration & Setup

### 7.1 Atualizar Program.cs
- [ ] Registrar novos repositórios no DI
- [ ] Atualizar configurações de banco
- [ ] Configurar Supabase connection

### 7.2 Atualizar appsettings
- [ ] Nova connection string do Supabase
- [ ] Configurações do OpenAI
- [ ] Configurações do Evolution API

---

## 🎯 Fase 8: Testing & Validation

### 8.1 Testes Unitários
- [ ] Testar entidades
- [ ] Testar repositórios (mocks)
- [ ] Testar services

### 8.2 Testes de Integração
- [ ] Testar fluxo completo de webhook
- [ ] Testar criação de sessão
- [ ] Testar validação de acesso

---

## 🎯 Fase 9: Documentation

### 9.1 Atualizar README
- [ ] Documentar nova estrutura
- [ ] Documentar setup do banco
- [ ] Documentar configuração

### 9.2 Atualizar Architecture.md
- [ ] Documentar nova arquitetura
- [ ] Diagramas atualizados

---

## 📊 Progresso

- [ ] Fase 1: Foundation
- [ ] Fase 2: Domain Entities
- [ ] Fase 3: Domain Interfaces
- [ ] Fase 4: Infrastructure Repositories
- [ ] Fase 5: Application Services
- [ ] Fase 6: API Controllers
- [ ] Fase 7: Configuration
- [ ] Fase 8: Testing
- [ ] Fase 9: Documentation

---

## 🎯 Próximo Passo

**Começar pela Fase 1: Criar Enums**

