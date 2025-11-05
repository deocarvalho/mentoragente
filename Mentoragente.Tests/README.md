# Mentoragente.Tests

Test suite for Mentoragente project using xUnit, Moq, and FluentAssertions.

## 🧪 Test Structure

```
Mentoragente.Tests/
├── Domain/
│   ├── Entities/        # Entity tests
│   └── Models/          # Model tests
├── Application/
│   └── Services/        # Service tests
├── Infrastructure/
│   └── Services/        # External service tests
└── API/
    └── Controllers/     # Controller tests
```

## 🚀 Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~MessageProcessorTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~ProcessMessageAsync_ShouldCreateUserIfNotExists"
```

## 📊 Code Coverage

Target: 80%+ coverage

Coverage reports are generated in `./coverage/` directory.

## 🧩 Test Categories

### Domain Tests
- Entity validation tests
- Model tests
- Enum tests

### Application Tests
- MessageProcessor tests
- Business logic tests

### Infrastructure Tests
- Repository tests (mocked)
- External service tests

### API Tests
- Controller tests
- Integration tests

## 📝 Test Patterns

- **Arrange-Act-Assert (AAA)** pattern
- **FluentAssertions** for readable assertions
- **Moq** for mocking dependencies
- **xUnit** for test framework

## ✅ Current Test Coverage

- ✅ Domain Entities (5 entities) - Complete
- ✅ Domain Models (2 models) - Complete
- ✅ Application Services (MessageProcessor) - 10+ test cases
- ✅ API Controllers (WhatsAppWebhookController) - 7+ test cases
- ✅ Infrastructure Services (OpenAI, EvolutionAPI) - Structure tests
- ✅ Integration Tests - Structure tests
- ⏳ Infrastructure Repositories - Structure tests (require Supabase mocking)

## 📋 Test Files

### Domain Tests
- `Domain/Entities/UserTests.cs`
- `Domain/Entities/MentoriaTests.cs`
- `Domain/Entities/AgentSessionTests.cs`
- `Domain/Entities/AgentSessionDataTests.cs`
- `Domain/Entities/ConversationTests.cs`
- `Domain/Models/ChatMessageTests.cs`
- `Domain/Models/WhatsAppWebhookDtoTests.cs`

### Application Tests
- `Application/Services/MessageProcessorTests.cs` (6 tests)
- `Application/Services/MessageProcessorAdditionalTests.cs` (3 additional tests)

### API Tests
- `API/Controllers/WhatsAppWebhookControllerTests.cs` (7 tests)
- `API/Integration/WhatsAppWebhookIntegrationTests.cs` (3 integration tests)

### Infrastructure Tests
- `Infrastructure/Services/OpenAIAssistantServiceTests.cs`
- `Infrastructure/Services/EvolutionAPIServiceTests.cs`
- `Infrastructure/Repositories/UserRepositoryTests.cs`

---

**Status:** ✅ Core tests implemented - 20+ test cases covering main flows

