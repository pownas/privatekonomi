#!/bin/bash

# ============================================================================
# Privatekonomi Application Startup Script
# ============================================================================
# 
# This script starts the Privatekonomi application using .NET Aspire Dashboard.
# 
# Prerequisites:
# - Run bash setup-codespaces.sh first to set up the development environment
# 
# What this script does:
# 1. Stop any existing .NET processes and free up ports
# 2. Check prerequisites are installed
# 3. Navigate to AppHost directory
# 4. Start Aspire Dashboard with all services
#
# Created: October 23, 2025
# ============================================================================

set -e  # Exit on any error

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Function to stop existing processes and free ports
cleanup_processes() {
    log_info "Stopping existing .NET processes and freeing ports..."
    
    # Kill all dotnet processes
    if pgrep -f "dotnet" > /dev/null; then
        log_warning "Found running .NET processes, stopping them..."
        pkill -f "dotnet" || true
        sleep 2
        
        # Force kill if still running
        if pgrep -f "dotnet" > /dev/null; then
            log_warning "Force killing remaining .NET processes..."
            pkill -9 -f "dotnet" || true
            sleep 1
        fi
    fi
    
    # Check for processes using common Aspire ports and kill them
    local aspire_ports=(15184 17127 19109 20095 21218 22282 18080 18443)
    
    for port in "${aspire_ports[@]}"; do
        local pid=$(lsof -ti:$port 2>/dev/null || true)
        if [ -n "$pid" ]; then
            log_warning "Port $port is in use by process $pid, stopping it..."
            kill $pid 2>/dev/null || true
            sleep 1
            
            # Force kill if still running
            if kill -0 $pid 2>/dev/null; then
                log_warning "Force killing process $pid on port $port..."
                kill -9 $pid 2>/dev/null || true
            fi
        fi
    done
    
    log_success "Process cleanup completed"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_section() {
    echo -e "\n${BLUE}========================================${NC}"
    echo -e "${BLUE} $1${NC}"
    echo -e "${BLUE}========================================${NC}\n"
}

# Check prerequisites
check_prerequisites() {
    log_section "Checking Prerequisites"
    
    # Check if .NET is installed and accessible
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is not installed or not in PATH"
        log_warning "Please run 'bash setup-codespaces.sh' first to set up the environment"
        exit 1
    fi
    
    log_info "✅ .NET SDK version: $(dotnet --version)"
    
    # Check if .NET 11 is available
    if ! dotnet --list-sdks | grep -q "11\."; then
        log_error ".NET 11 SDK is not installed"
        log_warning "Please run 'bash setup-codespaces.sh' first to install .NET 11"
        exit 1
    fi
    
    log_info "✅ .NET 11 SDK is available"
    
    # Check if Aspire packages are available (via NuGet, not workload)
    log_info "Checking for Aspire support..."
    if [[ -f "src/Privatekonomi.AppHost/Privatekonomi.AppHost.csproj" ]]; then
        if grep -q "Aspire.Hosting" "src/Privatekonomi.AppHost/Privatekonomi.AppHost.csproj"; then
            log_info "✅ Aspire is configured via NuGet packages"
        else
            log_warning "Aspire packages not found in AppHost project"
        fi
    fi
    
    # Check if solution exists and can be built
    if [[ ! -f "Privatekonomi.slnx" ]]; then
        log_error "Privatekonomi.slnx not found"
        log_warning "Make sure you're in the project root directory"
        exit 1
    fi
    
    log_info "✅ Project solution found"
    
    # Check if AppHost directory exists
    if [[ ! -d "src/Privatekonomi.AppHost" ]]; then
        log_error "AppHost directory not found at src/Privatekonomi.AppHost"
        exit 1
    fi
    
    log_info "✅ AppHost directory found"
    log_success "All prerequisites are satisfied"
}

# Quick build check
quick_build_check() {
    log_section "Quick Build Check"
    
    log_info "Performing quick build verification..."
    if dotnet build Privatekonomi.slnx --verbosity minimal --no-restore; then
        log_success "Project builds successfully"
    else
        log_warning "Build issues detected - attempting to restore and rebuild..."
        dotnet restore Privatekonomi.slnx
        dotnet build Privatekonomi.slnx
        log_success "Project restored and built successfully"
    fi
}

# Start application
start_application() {
    log_section "Starting Privatekonomi Application"
    
    log_info "Navigating to AppHost directory..."
    cd "src/Privatekonomi.AppHost"
    
    echo ""
    echo -e "${GREEN}🚀 Starting Privatekonomi with .NET Aspire Dashboard...${NC}"
    echo -e "${BLUE}The dashboard will open automatically in your browser.${NC}"
    echo ""
    echo -e "${YELLOW}Services that will be available:${NC}"
    echo -e "  • ${GREEN}Aspire Dashboard:${NC} View logs, traces, and metrics"
    echo -e "  • ${GREEN}Web Application:${NC} Blazor Server UI"
    echo -e "  • ${GREEN}API:${NC} REST API with Swagger documentation"
    echo ""
    echo -e "${BLUE}💡 Useful Aspire Dashboard features:${NC}"
    echo -e "  • ${YELLOW}Structured Logs:${NC} Real-time application logging"
    echo -e "  • ${YELLOW}Distributed Tracing:${NC} Request flow across services"  
    echo -e "  • ${YELLOW}Metrics:${NC} Performance and health monitoring"
    echo -e "  • ${YELLOW}Resources:${NC} Service status and configuration"
    echo ""
    echo -e "${RED}Press Ctrl+C to stop all services${NC}"
    echo ""
    
    # Run the AppHost
    dotnet run
}

# Display help information
show_help() {
    echo -e "${BLUE}Privatekonomi Application Startup Script${NC}"
    echo ""
    echo -e "${YELLOW}Usage:${NC}"
    echo -e "  cd scripts/codespaces && bash start.sh    Start the application"
    echo -e "  bash start.sh --help                      Show this help message"
    echo ""
    echo -e "${YELLOW}Prerequisites:${NC}"
    echo -e "  Run ${GREEN}bash setup-codespaces.sh${NC} first to set up the development environment"
    echo ""
    echo -e "${YELLOW}What this script does:${NC}"
    echo -e "  1. Stops any existing .NET processes and frees up ports"
    echo -e "  2. Checks that prerequisites are installed (.NET 11, Aspire dependencies)"
    echo -e "  3. Verifies the project builds successfully"
    echo -e "  4. Starts the Aspire Dashboard with all services"
    echo ""
    echo -e "${YELLOW}Troubleshooting:${NC}"
    echo -e "  • If you get 'command not found' errors, run ${GREEN}bash setup-codespaces.sh${NC}"
    echo -e "  • If services fail to start, check the Aspire Dashboard logs"
    echo -e "  • For HTTPS certificate issues, run ${GREEN}dotnet dev-certs https --trust${NC}"
}

# Main execution
main() {
    # Check for help flag
    if [[ "$1" == "--help" || "$1" == "-h" ]]; then
        show_help
        exit 0
    fi
    
    log_section "Privatekonomi Application Startup"
    
    cleanup_processes
    check_prerequisites
    quick_build_check
    start_application
}

# Run main function
main "$@"
