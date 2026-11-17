$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$documentationPath = Join-Path $repositoryRoot 'docs\scripts\windows\README.md'
$installerPath = Join-Path $repositoryRoot 'scripts\windows\install.ps1'

if (-not (Test-Path -LiteralPath $documentationPath -PathType Leaf) -or -not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    Write-Error 'Hittar inte Windows-dokumentationen eller installeraren. Klona hela repositoryt och kör skriptet från dess rot.'
    exit 1
}

Write-Host "Dokumentation: $documentationPath"
Write-Host "Installerare: $installerPath"
Write-Host 'Installationen lägger till .NET-verktyg, återställer paket och bygger projektet.'

if ($args -contains '-Help' -or $args -contains '--help' -or $args -contains '-?') {
    Write-Host 'Användning: .\setup-windows.ps1'
    exit 0
}

$confirmation = Read-Host 'Vill du fortsätta med Windows-installationen? (y/n)'
if ($confirmation -notmatch '^(?i:j|ja|y|yes)$') {
    Write-Host 'Installationen avbröts.'
    exit 0
}

& $installerPath @args
if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
