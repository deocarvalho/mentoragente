# Protegendo o Swagger UI com Autenticação

## Opções Disponíveis

### Opção 1: Basic Authentication (Recomendado para Desenvolvimento)

Adiciona um login básico antes de acessar o Swagger UI.

**Implementação:**

1. Criar um middleware customizado em `Mentoragente.API/Middleware/SwaggerAuthMiddleware.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mentoragente.API.Middleware;

public class SwaggerAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _username;
    private readonly string _password;

    public SwaggerAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _username = configuration["Swagger:Username"] ?? "admin";
        _password = configuration["Swagger:Password"] ?? "admin";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                var encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                var usernamePassword = decodedUsernamePassword.Split(':');
                var username = usernamePassword[0];
                var password = usernamePassword[1];

                if (username == _username && password == _password)
                {
                    await _next(context);
                    return;
                }
            }

            context.Response.Headers["WWW-Authenticate"] = "Basic";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        await _next(context);
    }
}
```

2. Registrar no `Program.cs`:

```csharp
// Adicionar ANTES de app.UseSwagger()
app.UseMiddleware<Middleware.SwaggerAuthMiddleware>();
```

3. Configurar no `appsettings.json`:

```json
{
  "Swagger": {
    "Username": "admin",
    "Password": "sua-senha-secreta-aqui"
  }
}
```

**Vantagens:**
- Simples de implementar
- Funciona bem para desenvolvimento
- Não requer bibliotecas externas

**Desvantagens:**
- Basic Auth não é muito seguro (senha em base64, não criptografada)
- Não recomendado para produção sem HTTPS

---

### Opção 2: Usar a Mesma API Key (Recomendado para Produção)

Exigir a API Key também para acessar o Swagger UI.

**Implementação:**

1. Modificar `ApiKeyAuthenticationHandler.cs` para não pular autenticação no Swagger:

```csharp
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    // Remover a linha que pula autenticação para Swagger:
    // if (Request.Path.StartsWithSegments("/swagger")) { ... }
    
    // Manter apenas health checks e webhooks sem autenticação
    if (Request.Path == "/" ||
        Request.Path.StartsWithSegments("/health") || 
        Request.Path.StartsWithSegments("/api/webhooks"))
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    // ... resto do código
}
```

2. Adicionar `[Authorize]` em um controller dummy ou criar um endpoint de validação

**Vantagens:**
- Usa a mesma autenticação da API
- Mais seguro que Basic Auth
- Não requer configuração adicional

**Desvantagens:**
- Swagger UI não suporta nativamente API Key no header antes de fazer requisições
- Requer configurar a API Key manualmente no Swagger após acessar

---

### Opção 3: Desabilitar Swagger em Produção (Mais Seguro)

Simplesmente não expor o Swagger em produção.

**Implementação:**

No `Program.cs`:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**Vantagens:**
- Mais seguro
- Não expõe documentação da API publicamente
- Padrão recomendado para produção

**Desvantagens:**
- Não há acesso à documentação em produção
- Requer ferramentas externas (Postman, etc.) para testar

---

## Recomendação

- **Desenvolvimento:** Opção 1 (Basic Auth) ou deixar sem proteção
- **Produção:** Opção 3 (desabilitar Swagger) + usar Postman/Insomnia com API Key

## Implementação Rápida (Opção 1)

Se quiser implementar a Opção 1 agora, posso criar o middleware e configurar. É a mais simples e funciona bem para desenvolvimento/testes.

