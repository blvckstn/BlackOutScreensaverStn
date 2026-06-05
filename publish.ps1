# Сборка и публикация PowerOffScreensaver как самодостаточного .exe
param(
    [string]$OutputDir = "publish"
)

$ErrorActionPreference = "Stop"

Write-Host "Сборка PowerOffScreensaver..." -ForegroundColor Cyan

dotnet publish src/PowerOffScreensaver/PowerOffScreensaver.csproj `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Ошибка сборки." -ForegroundColor Red
    exit 1
}

$exePath = Join-Path $OutputDir "PowerOffScreensaver.exe"
$scrPath = Join-Path $OutputDir "PowerOffScreensaver.scr"

if (Test-Path $exePath) {
    Copy-Item $exePath $scrPath -Force
    $sizeMB = [math]::Round((Get-Item $exePath).Length / 1MB, 1)
    Write-Host ""
    Write-Host "Готово!" -ForegroundColor Green
    Write-Host "  EXE: $exePath ($sizeMB MB)"
    Write-Host "  SCR: $scrPath"
    Write-Host ""
    Write-Host "Установка хранителя экрана:" -ForegroundColor Yellow
    Write-Host "  Скопируйте $scrPath в C:\Windows\System32\"
    Write-Host "  Правый клик -> Установить"
} else {
    Write-Host "EXE не найден в $OutputDir" -ForegroundColor Red
    exit 1
}
