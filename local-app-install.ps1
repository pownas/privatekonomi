$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'scripts\windows\install.ps1'
if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "Windows installer not found: $scriptPath"
}
Push-Location $PSScriptRoot
try {
    & $scriptPath @args
}
finally {
    Pop-Location
}
