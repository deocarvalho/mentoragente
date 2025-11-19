# Diretrizes para API Key

## 📋 Especificações Recomendadas

### Tamanho
- **Mínimo:** 32 caracteres
- **Recomendado:** 64-128 caracteres
- **Máximo:** 256 caracteres (para evitar problemas com headers HTTP)

### Formato
A API Key deve ser uma string alfanumérica com alta entropia. Formatos aceitos:

1. **Base64** (recomendado):
   - Exemplo: `dGhpc2lzYXZlcnlsb25nc2VjcmV0YXBpa2V5dGhhdHNob3VsZGJlYXRsZWFzdDY0Y2hhcmFjdGVycw==`
   - Caracteres: A-Z, a-z, 0-9, +, /, = (padding)

2. **Hexadecimal**:
   - Exemplo: `a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456`
   - Caracteres: 0-9, a-f (ou A-F)

3. **Alfanumérico com caracteres especiais**:
   - Exemplo: `Mg-7K9pQ2vX5wR8tY1uI3oP6aS4dF0gHjLzNcVbMx`
   - Caracteres: A-Z, a-z, 0-9, -, _, +, /, =, @, #, $, etc.

### Características de Segurança

✅ **DEVE:**
- Ser gerada aleatoriamente (não previsível)
- Ter alta entropia (muitos bits de aleatoriedade)
- Ser única (não reutilizar a mesma chave em múltiplos ambientes)
- Ser armazenada de forma segura (variáveis de ambiente, Azure Key Vault, etc.)
- Ser rotacionada periodicamente (a cada 90-180 dias)

❌ **NÃO DEVE:**
- Conter informações identificáveis (nomes, datas, etc.)
- Ser um valor padrão ou exemplo (`"YOUR_API_KEY_HERE"`, `"test"`, `"123456"`)
- Ser commitada no repositório Git (usar `appsettings.Production.json` ou variáveis de ambiente)
- Ser compartilhada via email não criptografado ou chat
- Ser reutilizada entre ambientes (dev, staging, produção)

## 🔐 Geração Segura

### Opção 1: PowerShell (Windows)
```powershell
# Gera uma API Key de 64 caracteres (Base64)
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

### Opção 2: .NET CLI
```bash
# Gera uma API Key de 64 caracteres
dotnet run --project GenerateApiKey.csproj
```

### Opção 3: Online (apenas para desenvolvimento)
- https://www.guidgenerator.com/
- https://randomkeygen.com/
- **⚠️ NUNCA use geradores online para produção!**

### Opção 4: Código C# (para gerar programaticamente)
```csharp
using System.Security.Cryptography;

public static string GenerateApiKey(int length = 64)
{
    using var rng = RandomNumberGenerator.Create();
    var bytes = new byte[length];
    rng.GetBytes(bytes);
    return Convert.ToBase64String(bytes)
        .Replace("+", "-")
        .Replace("/", "_")
        .Replace("=", "")
        .Substring(0, length);
}
```

## 📝 Configuração

### appsettings.json (Desenvolvimento)
```json
{
  "ApiKey": "YOUR_DEV_API_KEY_HERE"
}
```

### appsettings.Production.json (Produção)
```json
{
  "ApiKey": "{{GERAR_UMA_CHAVE_SEGURA_AQUI}}"
}
```

### Variáveis de Ambiente (Recomendado para Produção)
```bash
# Linux/Mac
export ApiKey="sua-chave-secreta-aqui"

# Windows PowerShell
$env:ApiKey="sua-chave-secreta-aqui"

# Docker
docker run -e ApiKey="sua-chave-secreta-aqui" ...
```

### Azure App Service
1. Portal Azure → App Service → Configuration
2. Adicionar Application Setting: `ApiKey` = `sua-chave-secreta-aqui`
3. Marcar como "Slot Setting" se necessário

## ✅ Validação Atual

Atualmente, a API Key é validada apenas por comparação de strings. Isso é suficiente, mas podemos melhorar com:

1. **Validação de formato** (opcional, mas recomendado)
2. **Validação de tamanho mínimo**
3. **Logging de tentativas inválidas** (já implementado)
4. **Rate limiting por IP** (item #3 do checklist)

## 🔄 Rotação de Chaves

### Processo Recomendado:
1. Gerar nova chave
2. Atualizar em `appsettings.Production.json` ou variável de ambiente
3. Testar com a nova chave
4. Atualizar todos os clientes (Postman, scripts, etc.)
5. Remover a chave antiga após período de transição (7-14 dias)

## 📊 Exemplos de Chaves Válidas

✅ **Bom:**
```
dGhpc2lzYXZlcnlsb25nc2VjcmV0YXBpa2V5dGhhdHNob3VsZGJlYXRsZWFzdDY0Y2hhcmFjdGVycw==
a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef1234567890
Mg-7K9pQ2vX5wR8tY1uI3oP6aS4dF0gHjLzNcVbMxQwErTyUiOpAsDfGhJkLzXcVbNm
```

❌ **Ruim:**
```
test
123456
YOUR_API_KEY_HERE
admin
password
minha-api-key-secreta
```

## 🛡️ Próximos Passos (Opcional)

Se quiser implementar validação mais robusta, posso adicionar:

1. **Validador de API Key** que verifica:
   - Tamanho mínimo (32 caracteres)
   - Formato válido (Base64, hex, ou alfanumérico)
   - Não é valor padrão

2. **Geração automática** de API Key no primeiro deploy

3. **Aviso no startup** se a API Key for um valor padrão ou muito curta

Quer que eu implemente alguma dessas melhorias?

