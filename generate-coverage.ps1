# Script to generate code coverage report
# Usage: .\generate-coverage.ps1

Write-Host "Running tests with code coverage..." -ForegroundColor Cyan
dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults" --verbosity minimal

Write-Host "`nGenerating HTML coverage report..." -ForegroundColor Cyan
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:"Html;TextSummary"

Write-Host "`nOpening coverage report in browser..." -ForegroundColor Green
Start-Process "CoverageReport\index.html"

Write-Host "`nCoverage report generated successfully!" -ForegroundColor Green
Write-Host "Report location: $PWD\CoverageReport\index.html" -ForegroundColor Yellow

