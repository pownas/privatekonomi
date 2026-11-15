# ============================================================================
# Privatekonomi Local Environment Setup Script (PowerShell)
# ============================================================================
#
# This script automates the complete setup of the development environment
# for the Privatekonomi project on Windows/PowerShell.
#
# Summary of setup performed:
# 1. Install .NET 11 SDK (required for this project)
# 2. Restore project dependencies
# 3. Clean and rebuild solution
# 4. Install Entity Framework CLI tools
# 5. Configure HTTPS development certificates
# 6. Verify installation and test readiness
# 7. Display usage information
#
# Created: October 23, 2025
# Based on: GitHub Copilot chat session for local development prerequisites
# ============================================================================

$ErrorActionPreference = "Stop"

# Logging functions with colors
function Write-LogInfo {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-LogSuccess {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-LogWarning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-LogError {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-LogSection {
    param([string]$Title)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Blue
    Write-Host " $Title" -ForegroundColor Blue
    Write-Host "========================================" -ForegroundColor Blue
    Write-Host ""
}

function Get-DotNetDevCertificate {
    try {
        $certificates = Get-ChildItem -Path Cert:\CurrentUser\My -ErrorAction Stop |
            Where-Object {
                $_.FriendlyName -eq ".NET Core HTTPS Development Certificate" -or
                $_.Subject -like "*CN=localhost*"
            } |
            Sort-Object -Property NotAfter -Descending

        return $certificates | Select-Object -First 1
    }
    catch {
        Write-LogWarning "Unable to inspect existing HTTPS development certificates: $($_.Exception.Message)"
        return $null
    }
}

# Check if running with appropriate permissions
function Test-AdminPrivileges {
    Write-LogSection "Checking Administrator Privileges"
    
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    $isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-LogWarning "This script is not running as Administrator"
        Write-LogWarning "Some operations may require elevated privileges"
        Write-LogWarning "Consider running PowerShell as Administrator if you encounter issues"
    } else {
        Write-LogInfo "Running with Administrator privileges"
    }
}

# Step 1: Install .NET 11 SDK
function Install-DotNet11 {
    Write-LogSection "Installing .NET 11 SDK"
    
    Write-LogInfo "Checking current .NET installation..."
    try {
        $dotnetVersion = dotnet --version
        Write-LogInfo "Current .NET version: $dotnetVersion"
        Write-LogInfo "Installed SDKs:"
        dotnet --list-sdks
        
        # Check if .NET 11 is already installed
        $sdks = dotnet --list-sdks | Out-String
        if ($sdks -match "11\.") {
            Write-LogSuccess ".NET 11 SDK is already installed"
            return
        }
    }
    catch {
        Write-LogWarning ".NET not found in PATH"
    }
    
    Write-LogInfo "Downloading and installing .NET 11 SDK..."
    try {
        # Download the .NET install script
        $installScript = Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -UseBasicParsing
        $scriptPath = "$env:TEMP\dotnet-install.ps1"
        $installScript.Content | Out-File -FilePath $scriptPath -Encoding UTF8
        
        # Run the installer
        & $scriptPath -Channel 11.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"
        
        # Add to PATH for current session
        $dotnetPath = "$env:LOCALAPPDATA\Microsoft\dotnet"
        if ($env:PATH -notlike "*$dotnetPath*") {
            $env:PATH = "$dotnetPath;$env:PATH"
        }
        
        # Add to user PATH permanently
        $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
        if ($userPath -notlike "*$dotnetPath*") {
            [Environment]::SetEnvironmentVariable("PATH", "$dotnetPath;$userPath", "User")
        }
        
        Write-LogInfo "Verifying .NET 11 installation..."
        dotnet --list-sdks
        Write-LogSuccess ".NET 11 SDK installation completed"
    }
    catch {
        Write-LogError "Failed to install .NET 11 SDK: $($_.Exception.Message)"
        Write-LogWarning "Please manually download and install .NET 11 SDK from: https://dotnet.microsoft.com/download/dotnet/11.0"
        throw
    }
}

# Step 2: Restore and build project
function Setup-Project {
    Write-LogSection "Setting up Project Dependencies"
    
    if (-not (Test-Path "Privatekonomi.slnx")) {
        Write-LogError "Privatekonomi.slnx not found in current directory"
        Write-LogError "Please run this script from the project root directory"
        throw "Solution file not found"
    }
    
    Write-LogInfo "Restoring project dependencies..."
    try {
        dotnet restore Privatekonomi.slnx
        
        Write-LogInfo "Cleaning previous build artifacts..."
        dotnet clean Privatekonomi.slnx
        
        Write-LogInfo "Building solution..."
        dotnet build Privatekonomi.slnx
        
        Write-LogSuccess "Project setup completed"
    }
    catch {
        Write-LogError "Failed to restore/build project: $($_.Exception.Message)"
        throw
    }
}

# Step 3: Install Entity Framework CLI tools
function Install-EFTools {
    Write-LogSection "Installing Entity Framework CLI Tools"
    
    Write-LogInfo "Checking if Entity Framework tools are already installed..."
    $globalTools = dotnet tool list --global | Out-String
    if ($globalTools -match "dotnet-ef") {
        Write-LogSuccess "Entity Framework tools are already installed"
        # Verify it's working
        try {
            dotnet ef --version | Out-Null
            return
        }
        catch {
            Write-LogWarning "dotnet-ef is installed but not working, attempting to reinstall..."
            dotnet tool uninstall --global dotnet-ef 2>$null
        }
    }
    
    Write-LogInfo "Installing Entity Framework global tools..."
    try {
        dotnet tool install --global dotnet-ef --version 10.0.12 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Installation failed"
        }
    }
    catch {
        Write-LogWarning "Installation failed, clearing NuGet cache and retrying..."
        dotnet nuget locals all --clear | Out-Null
        
        Write-LogInfo "Attempting installation with version 10.0.12..."
        try {
            dotnet tool install --global dotnet-ef --version 10.0.12
            if ($LASTEXITCODE -ne 0) {
                throw "Installation failed after cache clear"
            }
        }
        catch {
            Write-LogError "Failed to install Entity Framework tools after cache clear"
            Write-LogError "Try manually: dotnet tool install --global dotnet-ef --version 10.0.12"
            throw
        }
    }
    
    Write-LogInfo "Verifying Entity Framework tools installation..."
    if ((dotnet tool list --global | Out-String) -match "dotnet-ef") {
        Write-LogSuccess "Entity Framework tools installed successfully"
        try {
            dotnet ef --version
        }
        catch {
            Write-LogWarning "EF version check had issues but tool is installed"
        }
    }
    else {
        Write-LogError "Failed to verify Entity Framework tools installation"
        throw "EF tools verification failed"
    }
    Write-LogSuccess "Entity Framework tools installation completed"
}

# Step 4: Configure HTTPS development certificates
function Configure-DevCerts {
    Write-LogSection "Configuring HTTPS Development Certificates"
    
    $renewalThresholdDays = 60
    $shouldRenew = $true
    $existingCert = Get-DotNetDevCertificate

    Write-LogInfo "Checking existing HTTPS development certificate..."
    try {
        $null = dotnet dev-certs https --check
    }
    catch {
        Write-LogWarning "dotnet dev-certs check reported an issue: $($_.Exception.Message)"
    }

    if ($existingCert) {
        $daysRemaining = [math]::Floor(($existingCert.NotAfter - (Get-Date)).TotalDays)
        Write-LogInfo (("Existing certificate expires on {0:yyyy-MM-dd} (~{1} days remaining).") -f $existingCert.NotAfter, $daysRemaining)
        if ($daysRemaining -ge $renewalThresholdDays) {
            $shouldRenew = $false
            Write-LogSuccess (("Certificate is valid for more than {0} days; keeping existing certificate.") -f $renewalThresholdDays)
        }
        elseif ($daysRemaining -ge 0) {
            Write-LogWarning (("Certificate expires within {0} days; renewing certificate.") -f $renewalThresholdDays)
        }
        else {
            Write-LogWarning "Certificate has already expired; renewing certificate."
        }
    }
    else {
        Write-LogWarning "No existing HTTPS development certificate found; generating a new one."
    }

    if ($shouldRenew) {
        Write-LogInfo "Cleaning existing HTTPS development certificates..."
        try {
            dotnet dev-certs https --clean
        }
        catch {
            Write-LogWarning "Failed to clean existing certificates (may already be removed): $($_.Exception.Message)"
        }

        Write-LogInfo "Generating and trusting a new HTTPS development certificate..."
        try {
            dotnet dev-certs https --trust
        }
        catch {
            Write-LogWarning "Failed to trust the new certificate (may require manual trust): $($_.Exception.Message)"
        }

        $existingCert = Get-DotNetDevCertificate
    }

    Write-LogInfo "Verifying HTTPS certificate installation..."
    try {
        $certCheck = dotnet dev-certs https --check
        if ($LASTEXITCODE -eq 0) {
            Write-LogSuccess "HTTPS development certificate is properly configured"
        }
        else {
            Write-LogWarning "HTTPS certificate verification reported issues"
        }
    }
    catch {
        Write-LogWarning "HTTPS certificate verification failed - applications may have SSL issues"
    }

    if ($existingCert) {
        Write-LogSuccess (("HTTPS development certificates configuration completed (current cert valid until {0:yyyy-MM-dd}).") -f $existingCert.NotAfter)
    }
    else {
        Write-LogSuccess "HTTPS development certificates configuration completed"
    }
}

# Step 5: Verify installation
function Test-Installation {
    Write-LogSection "Verifying Installation"
    
    Write-LogInfo "Checking .NET installation..."
    try {
        dotnet --version
        dotnet --list-sdks
        
        Write-LogInfo "Checking Entity Framework tools..."
        $globalTools = dotnet tool list --global | Out-String
        if ($globalTools -match "dotnet-ef") {
            Write-LogSuccess "EF tools are installed"
        }
        else {
            Write-LogWarning "EF tools not found in global tools"
        }
        
        Write-LogInfo "Checking HTTPS development certificates..."
        try {
            $certCheck = dotnet dev-certs https --check --quiet
            if ($LASTEXITCODE -eq 0) {
                Write-LogSuccess "HTTPS development certificates are properly configured"
            }
            else {
                Write-LogWarning "HTTPS development certificates may need attention"
            }
        }
        catch {
            Write-LogWarning "HTTPS development certificates may need attention"
        }
        
        Write-LogInfo "Checking project build status..."
        dotnet build Privatekonomi.slnx --verbosity quiet
        
        Write-LogSuccess "All verifications completed successfully!"
    }
    catch {
        Write-LogError "Installation verification failed: $($_.Exception.Message)"
        throw
    }
}

# Step 6: Display usage information
function Show-UsageInfo {
    Write-LogSection "Installation Complete - Usage Information"
    
    Write-Host "✅ Privatekonomi environment is ready!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Next Steps:" -ForegroundColor Blue
    Write-Host "  Start Application:" -ForegroundColor Yellow
    Write-Host "    .\local-app-start.ps1"
    Write-Host ""
    Write-Host "Available commands:" -ForegroundColor Blue
    Write-Host "  Database Operations:" -ForegroundColor Yellow
    Write-Host "    cd src\Privatekonomi.Core"
    Write-Host "    dotnet ef migrations add <MigrationName>"
    Write-Host "    dotnet ef database update"
    Write-Host ""
    Write-Host "  Build and Test:" -ForegroundColor Yellow
    Write-Host "    dotnet build Privatekonomi.slnx"
    Write-Host "    dotnet test"
    Write-Host ""
    Write-Host "Project Structure:" -ForegroundColor Blue
    Write-Host "  • Privatekonomi.Api - Backend API project" -ForegroundColor Yellow
    Write-Host "  • Privatekonomi.Web - Blazor frontend project" -ForegroundColor Yellow
    Write-Host "  • Privatekonomi.Core - Shared core library" -ForegroundColor Yellow
    Write-Host "  • Privatekonomi.AppHost - Aspire orchestration" -ForegroundColor Yellow
    Write-Host "  • Privatekonomi.ServiceDefaults - Shared service configurations" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Installed Tools:" -ForegroundColor Blue
    Write-Host "  • .NET 11 SDK"
    Write-Host "  • Entity Framework CLI tools"
    Write-Host "  • HTTPS development certificates (trusted)"
    Write-Host "Aspire hanteras via projektets NuGet-paket – ingen separat workload krävs." -ForegroundColor Blue
    Write-Host ""
    Write-Host "Ready to start coding! Run " -NoNewline -ForegroundColor Green
    Write-Host ".\local-app-start.ps1" -NoNewline -ForegroundColor Yellow
    Write-Host " to launch the application! 🚀" -ForegroundColor Green
}

# Main execution
function Main {
    Write-LogSection "Privatekonomi Local Environment Installation"
    Write-LogInfo "Starting automated environment setup..."
    
    try {
        Test-AdminPrivileges
        Install-DotNet11
        Setup-Project
        Install-EFTools
        Configure-DevCerts
        Test-Installation
        Show-UsageInfo
        
        Write-LogSuccess "Environment installation completed successfully!"
    }
    catch {
        Write-LogError "Installation failed: $($_.Exception.Message)"
        Write-Host ""
        Write-Host "Common solutions:" -ForegroundColor Yellow
        Write-Host "  • Run PowerShell as Administrator"
        Write-Host "  • Check your internet connection"
        Write-Host "  • Ensure you're in the project root directory"
        Write-Host "  • Try running individual steps manually"
        exit 1
    }
}

# Run main function
Main