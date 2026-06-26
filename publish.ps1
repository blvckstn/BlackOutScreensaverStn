# Build and publish PowerOffScreensaver as a self-contained .exe
param(
    [string]$OutputDir = "publish"
)

$ErrorActionPreference = "Stop"

Write-Host "Building PowerOffScreensaver..." -ForegroundColor Cyan

dotnet publish src/PowerOffScreensaver/PowerOffScreensaver.csproj `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=false `
    -p:EnableCompressionInSingleFile=true `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit 1
}

$exePath = Join-Path $OutputDir "PowerOffScreensaver.exe"
$scrPath = Join-Path $OutputDir "PowerOffScreensaver.scr"

if (Test-Path $exePath) {
    Copy-Item $exePath $scrPath -Force
    $sizeMB = [math]::Round((Get-Item $exePath).Length / 1MB, 1)
    Write-Host ""
    Write-Host "Done!" -ForegroundColor Green
    Write-Host "  EXE: $exePath ($sizeMB MB)"
    Write-Host "  SCR: $scrPath"
    Write-Host ""
    Write-Host "Install screensaver:" -ForegroundColor Yellow
    Write-Host "  Copy $scrPath to C:\Windows\System32\"
    Write-Host "  Right-click -> Install"
} else {
    Write-Host "EXE not found in $OutputDir" -ForegroundColor Red
    exit 1
}
