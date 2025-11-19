# Script para executar todos os testes de forma organizada
# Uso: .\scripts\run-all-tests.ps1

param(
    [switch]$SkipE2E,
    [switch]$SkipSmoke,
    [switch]$OnlyE2E,
    [switch]$OnlySmoke,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "🧪 Executando Testes Mentoragente" -ForegroundColor Cyan
Write-Host ""

# Mudar para diretório do projeto
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

# Verificar se Docker está rodando (para E2E)
$dockerRunning = $false
if (-not $SkipE2E -and -not $OnlySmoke) {
    try {
        docker ps | Out-Null
        $dockerRunning = $true
        Write-Host "✅ Docker está rodando" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Docker não está rodando - testes E2E serão pulados" -ForegroundColor Yellow
        $SkipE2E = $true
    }
}

# Build do projeto de testes
Write-Host ""
Write-Host "📦 Compilando projeto de testes..." -ForegroundColor Cyan
dotnet build Mentoragente.Tests/Mentoragente.Tests.csproj --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Erro na compilação!" -ForegroundColor Red
    exit 1
}

$testResults = @{
    MockTests = $null
    E2ETests = $null
    SmokeTests = $null
}

# 1. Testes com Mocks (sempre executar, a menos que seja OnlyE2E ou OnlySmoke)
if (-not $OnlyE2E -and -not $OnlySmoke) {
    Write-Host ""
    Write-Host "🔧 Executando testes com Mocks (rápidos)..." -ForegroundColor Cyan
    $filter = "Category!=E2E&Category!=Smoke"
    $verbosity = if ($Verbose) { "normal" } else { "minimal" }
    
    dotnet test Mentoragente.Tests/Mentoragente.Tests.csproj `
        --filter $filter `
        --verbosity $verbosity `
        --no-build
    
    $testResults.MockTests = $LASTEXITCODE
}

# 2. Testes E2E (TestContainers)
if (-not $SkipE2E -and -not $OnlySmoke) {
    if ($dockerRunning) {
        Write-Host ""
        Write-Host "🐳 Executando testes E2E (TestContainers - banco real)..." -ForegroundColor Cyan
        Write-Host "   ⏱️  Estes testes são mais lentos (5-10s cada)" -ForegroundColor Yellow
        
        $verbosity = if ($Verbose) { "normal" } else { "minimal" }
        
        dotnet test Mentoragente.Tests/Mentoragente.Tests.csproj `
            --filter "Category=E2E" `
            --verbosity $verbosity `
            --no-build
        
        $testResults.E2ETests = $LASTEXITCODE
    } else {
        Write-Host ""
        Write-Host "⏭️  Pulando testes E2E (Docker não está rodando)" -ForegroundColor Yellow
        $testResults.E2ETests = -1
    }
}

# 3. Testes de Smoke (requer aplicação deployada)
if (-not $SkipSmoke -and -not $OnlyE2E) {
    $smokeUrl = $env:SMOKE_TEST_URL
    if ([string]::IsNullOrEmpty($smokeUrl)) {
        Write-Host ""
        Write-Host "⏭️  Pulando testes de Smoke (SMOKE_TEST_URL não configurado)" -ForegroundColor Yellow
        Write-Host "   Configure com: `$env:SMOKE_TEST_URL='https://seu-app.onrender.com'" -ForegroundColor Gray
        $testResults.SmokeTests = -1
    } else {
        Write-Host ""
        Write-Host "💨 Executando testes de Smoke contra: $smokeUrl" -ForegroundColor Cyan
        
        $verbosity = if ($Verbose) { "normal" } else { "minimal" }
        
        dotnet test Mentoragente.Tests/Mentoragente.Tests.csproj `
            --filter "Category=Smoke" `
            --verbosity $verbosity `
            --no-build
        
        $testResults.SmokeTests = $LASTEXITCODE
    }
}

# Resumo
Write-Host ""
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RESUMO DOS TESTES" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan

if ($testResults.MockTests -ne $null) {
    $status = if ($testResults.MockTests -eq 0) { "✅ PASSOU" } else { "❌ FALHOU" }
    Write-Host "Testes com Mocks: $status" -ForegroundColor $(if ($testResults.MockTests -eq 0) { "Green" } else { "Red" })
}

if ($testResults.E2ETests -ne $null) {
    if ($testResults.E2ETests -eq -1) {
        Write-Host "Testes E2E: ⏭️  PULADO (Docker não disponível)" -ForegroundColor Yellow
    } else {
        $status = if ($testResults.E2ETests -eq 0) { "✅ PASSOU" } else { "❌ FALHOU" }
        Write-Host "Testes E2E: $status" -ForegroundColor $(if ($testResults.E2ETests -eq 0) { "Green" } else { "Red" })
    }
}

if ($testResults.SmokeTests -ne $null) {
    if ($testResults.SmokeTests -eq -1) {
        Write-Host "Testes de Smoke: ⏭️  PULADO (SMOKE_TEST_URL não configurado)" -ForegroundColor Yellow
    } else {
        $status = if ($testResults.SmokeTests -eq 0) { "✅ PASSOU" } else { "❌ FALHOU" }
        Write-Host "Testes de Smoke: $status" -ForegroundColor $(if ($testResults.SmokeTests -eq 0) { "Green" } else { "Red" })
    }
}

Write-Host ""

# Exit code baseado nos resultados
$hasFailures = ($testResults.MockTests -ne $null -and $testResults.MockTests -ne 0) `
            -or ($testResults.E2ETests -ne $null -and $testResults.E2ETests -ne 0 -and $testResults.E2ETests -ne -1) `
            -or ($testResults.SmokeTests -ne $null -and $testResults.SmokeTests -ne 0 -and $testResults.SmokeTests -ne -1)

if ($hasFailures) {
    Write-Host "❌ Alguns testes falharam!" -ForegroundColor Red
    exit 1
} else {
    Write-Host "✅ Todos os testes passaram!" -ForegroundColor Green
    exit 0
}

