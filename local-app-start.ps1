param(
    [switch]$Help,
    [switch]$NoWatch
)

$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'scripts\windows\start.ps1'
if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "Windows startup script not found: $scriptPath"
}
Push-Location $PSScriptRoot
try {
    & $scriptPath -Help:$Help -NoWatch:$NoWatch
}
finally {
    Pop-Location
}
