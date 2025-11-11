# Script para configurar variáveis de ambiente no Render a partir do arquivo .env
# Requer: Render API Key configurada como variável de ambiente RENDER_API_KEY
# Uso: .\scripts\configure-render-env.ps1 -ServiceId "your-service-id" -EnvFile "Mentoragente.API\.env"

param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceId,
    
    [Parameter(Mandatory=$false)]
    [string]$EnvFile = "Mentoragente.API\.env",
    
    [Parameter(Mandatory=$false)]
    [string]$RenderApiKey = $env:RENDER_API_KEY
)

if (-not $RenderApiKey) {
    Write-Error "RENDER_API_KEY não configurada. Configure como variável de ambiente ou passe como parâmetro."
    exit 1
}

if (-not (Test-Path $EnvFile)) {
    Write-Error "Arquivo .env não encontrado: $EnvFile"
    exit 1
}

Write-Host "📖 Lendo variáveis de ambiente de: $EnvFile" -ForegroundColor Cyan
Write-Host "🔧 Configurando serviço Render: $ServiceId" -ForegroundColor Cyan
Write-Host ""

# Ler arquivo .env
$envVars = @{}
Get-Content $EnvFile | ForEach-Object {
    $line = $_.Trim()
    # Ignorar linhas vazias e comentários
    if ($line -and -not $line.StartsWith("#")) {
        if ($line -match "^([^=]+)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            # Remover aspas se existirem
            $value = $value -replace '^["'']|["'']$', ''
            $envVars[$key] = $value
        }
    }
}

Write-Host "✅ Encontradas $($envVars.Count) variáveis de ambiente" -ForegroundColor Green
Write-Host ""

# Preparar payload para Render API
$envVarsArray = @()
foreach ($key in $envVars.Keys) {
    $envVarsArray += @{
        key = $key
        value = $envVars[$key]
        generateValue = $false
    }
}

$body = @{
    envVars = $envVarsArray
} | ConvertTo-Json -Depth 10

# Configurar variáveis via Render API
$headers = @{
    "Authorization" = "Bearer $RenderApiKey"
    "Content-Type" = "application/json"
}

$uri = "https://api.render.com/v1/services/$ServiceId/env-vars"

Write-Host "🚀 Enviando configurações para Render..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri $uri -Method Put -Headers $headers -Body $body
    
    Write-Host ""
    Write-Host "✅ Variáveis de ambiente configuradas com sucesso!" -ForegroundColor Green
    Write-Host "📊 Total de variáveis: $($envVars.Count)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "💡 Dica: O serviço será reiniciado automaticamente no Render." -ForegroundColor Yellow
}
catch {
    Write-Host ""
    Write-Error "❌ Erro ao configurar variáveis: $($_.Exception.Message)"
    if ($_.ErrorDetails.Message) {
        Write-Host "Detalhes: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

