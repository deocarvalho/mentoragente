# Script para executar testes de Smoke após deploy
# Uso: .\scripts\run-smoke-tests.ps1 -Url "https://mentoragente-hmg.onrender.com"

param(
    [Parameter(Mandatory=$false)]
    [string]$Url = $env:SMOKE_TEST_URL,
    
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($Url)) {
    Write-Host "❌ Erro: URL não fornecida" -ForegroundColor Red
    Write-Host ""
    Write-Host "Uso:" -ForegroundColor Yellow
    Write-Host "  .\run-smoke-tests.ps1 -Url 'https://mentoragente-hmg.onrender.com'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Ou configure a variável de ambiente:" -ForegroundColor Yellow
    Write-Host "  `$env:SMOKE_TEST_URL='https://mentoragente-hmg.onrender.com'" -ForegroundColor Gray
    exit 1
}

Write-Host "💨 Executando Smoke Tests" -ForegroundColor Cyan
Write-Host "   URL: $Url" -ForegroundColor Gray
Write-Host ""

# Mudar para diretório do projeto
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

# Configurar variável de ambiente
$env:SMOKE_TEST_URL = $Url

# Executar testes
$verbosity = if ($Verbose) { "normal" } else { "minimal" }

dotnet test Mentoragente.Tests/Mentoragente.Tests.csproj `
    --filter "Category=Smoke" `
    --verbosity $verbosity

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Smoke tests passaram! Aplicação está funcionando." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ Smoke tests falharam! Verifique a aplicação." -ForegroundColor Red
    exit 1
}

