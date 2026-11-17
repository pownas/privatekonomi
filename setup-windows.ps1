$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$documentationPath = Join-Path $repositoryRoot 'docs\scripts\windows\README.md'
$scriptsDirectory = Join-Path $repositoryRoot 'scripts\windows'
$availableScripts = @(Get-ChildItem -LiteralPath $scriptsDirectory -Filter '*.ps1' -File | Sort-Object Name)

if (-not (Test-Path -LiteralPath $documentationPath -PathType Leaf) -or -not (Test-Path -LiteralPath $scriptsDirectory -PathType Container) -or $availableScripts.Count -eq 0) {
    Write-Error 'Hittar inte Windows-dokumentationen eller skripten. Klona hela repositoryt och kör skriptet från dess rot.'
    exit 1
}

Write-Host "Dokumentation: $documentationPath"
Write-Host "Skriptkatalog: $scriptsDirectory"

function Show-SetupHelp {
    Write-Host 'Användning: .\setup-windows.ps1 [skriptnamn] [argument...]'
    Write-Host 'Utelämna skriptnamn för att välja interaktivt. Argument utan skriptnamn skickas till install.ps1.'
    Write-Host 'Tillgängliga skript:'
    foreach ($script in $availableScripts) {
        Write-Host "  $($script.Name)"
    }
}

function Resolve-SetupScript {
    param([string]$Choice)

    $number = 0
    if ([int]::TryParse($Choice, [ref]$number)) {
        if ($number -ge 1 -and $number -le $availableScripts.Count) {
            return $availableScripts[$number - 1]
        }
        return $null
    }

    return $availableScripts | Where-Object { $_.BaseName -ieq $Choice -or $_.Name -ieq $Choice } | Select-Object -First 1
}

if ($args -contains '-Help' -or $args -contains '--help' -or $args -contains '-?') {
    Show-SetupHelp
    exit 0
}

$scriptArguments = @($args)
$selectedScript = $null
if ($scriptArguments.Count -gt 0) {
    $actionChoice = [string]$scriptArguments[0]
    $selectedScript = Resolve-SetupScript $actionChoice
    if ($null -ne $selectedScript) {
        $scriptArguments = @($scriptArguments | Select-Object -Skip 1)
    }
    elseif ($actionChoice.StartsWith('-')) {
        $selectedScript = Resolve-SetupScript 'install'
    }
    else {
        Write-Error "Okänd åtgärd: $actionChoice. Kör .\setup-windows.ps1 -Help för tillgängliga skript."
        exit 2
    }
}
else {
    Write-Host 'Tillgängliga skript för Windows:'
    foreach ($script in $availableScripts) {
        Write-Host "  $($script.Name)"
    }

    $defaultScript = Resolve-SetupScript 'install'
    if ($null -ne $defaultScript) {
        $actionChoice = Read-Host 'Välj skript [install.ps1] (eller q för att avbryta)'
        if ([string]::IsNullOrWhiteSpace($actionChoice)) {
            $selectedScript = $defaultScript
        }
        elseif ($actionChoice -match '^(?i:q)$') {
            Write-Host 'Avbrutet.'
            exit 0
        }
        else {
            $selectedScript = Resolve-SetupScript $actionChoice
        }
    }
    else {
        $actionChoice = Read-Host 'Välj skript (eller q för att avbryta)'
        if ($actionChoice -match '^(?i:q)$') {
            Write-Host 'Avbrutet.'
            exit 0
        }
        $selectedScript = Resolve-SetupScript $actionChoice
    }
    if ($null -eq $selectedScript) {
        Write-Error "Ogiltigt skriptval: $actionChoice"
        exit 2
    }
}

Write-Host "Valt skript: $($selectedScript.FullName)"
$confirmation = Read-Host "Vill du köra $($selectedScript.Name)? (y/n)"
if ($confirmation -notmatch '^(?i:j|ja|y|yes)$') {
    Write-Host 'Åtgärden avbröts.'
    exit 0
}

& $selectedScript.FullName @scriptArguments
if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
