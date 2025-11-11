# Scripts de Deploy e Configuração

## configure-render-env.ps1

Script PowerShell para configurar automaticamente variáveis de ambiente no Render a partir do arquivo `.env`.

### Pré-requisitos

1. **Render API Key**: Obtenha em [Render Dashboard → Account Settings → API Keys](https://dashboard.render.com/account/api-keys)
2. **Service ID**: Encontre no URL do serviço Render ou nas configurações do serviço

### Configuração

1. Configure a Render API Key como variável de ambiente:
   ```powershell
   $env:RENDER_API_KEY = "your-render-api-key-here"
   ```

2. Ou passe como parâmetro ao executar o script.

### Uso

```powershell
# Configurar variáveis para um serviço específico
.\scripts\configure-render-env.ps1 -ServiceId "srv-xxxxxxxxxxxxx"

# Especificar arquivo .env customizado
.\scripts\configure-render-env.ps1 -ServiceId "srv-xxxxxxxxxxxxx" -EnvFile "Mentoragente.API\.env.development"

# Passar API Key como parâmetro
.\scripts\configure-render-env.ps1 -ServiceId "srv-xxxxxxxxxxxxx" -RenderApiKey "your-api-key"
```

### Como encontrar o Service ID

1. Acesse seu serviço no Render Dashboard
2. O Service ID está na URL: `https://dashboard.render.com/web/srv-xxxxxxxxxxxxx`
3. Ou vá em Settings → Service ID

### Exemplo Completo

```powershell
# 1. Configure a API Key
$env:RENDER_API_KEY = "rnd_xxxxxxxxxxxxxxxxxxxxx"

# 2. Execute o script
.\scripts\configure-render-env.ps1 -ServiceId "srv-xxxxxxxxxxxxx"
```

### Notas Importantes

- ⚠️ O script **substitui todas** as variáveis de ambiente existentes no serviço
- ✅ Variáveis configuradas manualmente no Render serão sobrescritas
- 🔄 O serviço será reiniciado automaticamente após a configuração
- 🔒 Mantenha sua Render API Key segura e nunca a commite no repositório

### Alternativa Manual

Se preferir configurar manualmente, você pode:

1. Ler o arquivo `.env.example` como referência
2. Copiar cada variável manualmente no Render Dashboard
3. Ou usar o script apenas para desenvolvimento/staging

