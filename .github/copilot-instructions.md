# Copilot Instructions for Privatekonomi

## Project Overview

Privatekonomi is a personal finance application built with .NET 10, Blazor Server, and MudBlazor. The application helps users manage their personal finances with features for transaction tracking, budgeting, savings goals, investments, pension tracking, and more.

**Primary Language:** Swedish (for documentation, UI text, and comments in Swedish-facing components)
**Code Language:** English (for code, variable names, and technical documentation)

## Technology Stack

- **Framework:** .NET 10 SDK
- **Frontend:** Blazor Server with MudBlazor 9.0.0
- **Backend:** ASP.NET Core Web API
- **Database:** Entity Framework Core with multiple providers:
  - InMemory (development/testing)
  - SQLite (production, Raspberry Pi)
  - SQL Server (enterprise)
  - JsonFile (backup/portability)
- **Orchestration:** .NET Aspire for service management and observability
- **Testing:** xUnit with Moq for unit tests, Playwright for E2E tests
- **Features:** Nullable reference types enabled, implicit usings enabled

## Architecture

The solution follows a multi-project architecture:

```
Privatekonomi/
├── src/
│   ├── Privatekonomi.AppHost/        # .NET Aspire orchestrator
│   ├── Privatekonomi.ServiceDefaults/ # Aspire service defaults (telemetry, health checks)
│   ├── Privatekonomi.Web/            # Blazor Server UI (primary user interface)
│   ├── Privatekonomi.Api/            # ASP.NET Core Web API (REST endpoints)
│   └── Privatekonomi.Core/           # Shared library (models, services, data access)
├── tests/
│   ├── Privatekonomi.Core.Tests/     # Unit tests for Core
│   ├── Privatekonomi.Api.Tests/      # Unit tests for API
│   └── playwright/                   # E2E tests
└── docs/                             # Documentation (user guides, technical docs, and specifications)
```

## Development Workflow

### Building the Project

```bash
# Build entire solution
dotnet build

# Build specific project
cd src/Privatekonomi.Web
dotnet build
```

### Running the Application

**Preferred method (with Aspire):**
```bash
cd src/Privatekonomi.AppHost
dotnet run
```
This launches the Aspire Dashboard showing all services, logs, traces, and metrics.

**Alternative (individual services):**
```bash
# Web application
cd src/Privatekonomi.Web
dotnet run  # Access at http://localhost:5274

# API
cd src/Privatekonomi.Api
dotnet run  # Swagger at http://localhost:5000/swagger
```

**Helper scripts:**
- `./app-start.sh` (Linux/macOS/Codespaces)
- `./app-start.ps1` (Windows PowerShell)

### Running Tests

**Unit tests:**
```bash
# All unit tests
dotnet test

# Specific test project
cd tests/Privatekonomi.Core.Tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

**E2E tests with Playwright:**
```bash
cd tests/playwright
npm install
npx playwright install chromium
npm test
```

## Coding Standards

### General Guidelines

1. **Nullable Reference Types:** Always enabled. Use nullable annotations appropriately (`?`, `!`)
2. **Language:** 
   - Code, variable names, and method names: English
   - Swedish UI text, documentation, and user-facing content: Swedish
   - Comments explaining business logic: Can be in Swedish if it aids understanding
3. **Formatting:** Follow standard C# conventions (4 spaces, PascalCase for public members, camelCase for private)
4. **Architecture:** Follow existing patterns in the codebase
   - Services in `Privatekonomi.Core/Services/`
   - Models in `Privatekonomi.Core/Models/`
   - Blazor components in `Privatekonomi.Web/Components/Pages/`
   - API controllers in `Privatekonomi.Api/Controllers/`

### Code Quality

1. **Always add unit tests** for new services and business logic
2. **Use existing test patterns** from `tests/Privatekonomi.Core.Tests/`
3. **Mock dependencies** using Moq (see existing tests for examples)
4. **Validate edge cases** and error scenarios
5. **Add XML documentation comments** for public APIs
6. **Handle exceptions** appropriately with meaningful messages

### MudBlazor Components

- Use MudBlazor components consistently (MudDataGrid, MudCard, MudButton, etc.)
- Follow existing UI patterns for consistency
- Support both light and dark mode (see `docs/DARK_MODE_IMPLEMENTATION.md`)
- Ensure WCAG 2.1 Level AA compliance for accessibility

### Database and Data Access

1. **Storage Provider:** Configurable via `appsettings.json` (`Storage:Provider`)
2. **DbContext:** Use `ApplicationDbContext` from `Privatekonomi.Core.Data`
3. **Migrations:** Add migrations when changing models
4. **Seeding:** Test data seeding controlled by `Storage:SeedTestData`
5. **User Isolation:** All data should be scoped to the current user (`UserId` property)

## Important Patterns

### Service Registration

Services are registered in `Program.cs` of each project. Follow existing patterns:
- Scoped services for request-specific data
- Singleton services for shared state
- Transient services for stateless operations

### Authentication

- ASP.NET Core Identity for user management
- All pages require authentication by default (except login/register)
- Test user: `test@example.com` / `TestPassword123!`

### Data Models

- All models inherit from `BaseEntity` (has `Id` property)
- User-specific models have `UserId` property
- Temporal entities use `TemporalEntity` base class (StartDate, EndDate)
- Audit trail with CreatedAt, UpdatedAt, CreatedBy, UpdatedBy

### Error Handling

- Use try-catch blocks in services
- Return meaningful error messages to UI
- Log errors appropriately (uses OpenTelemetry when running with Aspire)

## Documentation

### When to Update Documentation

- **README.md:** For new major features
- **docs/\*.md:** For all documentation including user guides, feature specifications, and technical implementation details
- **Code comments:** For complex business logic

### Documentation Style

- User guides: Swedish (in `docs/`)
- Technical docs: Can be English or Swedish (in `docs/`)
- Code comments: English or Swedish (prefer Swedish for business domain concepts)
- API documentation: XML comments in English
- Try to always add printscreens of the new features in PR. 

## Testing Requirements

### Unit Tests (Required)

- **All new services** must have corresponding unit tests
- **All new business logic** in services must be tested
- Test file naming: `[ClassName]Tests.cs`
- Use xUnit assertions (`Assert.Equal`, `Assert.NotNull`, etc.)
- Mock external dependencies with Moq

### E2E Tests (Optional)

- Add Playwright tests for critical user workflows
- Follow existing test patterns in `tests/playwright/tests/`

### Integration Tests (As Needed)

- For database operations, use InMemory provider
- For API endpoints, use existing patterns in `Privatekonomi.Api.Tests`

## Common Tasks

### Adding a New Feature

1. Create/update models in `Privatekonomi.Core/Models/`
2. Add service interface and implementation in `Privatekonomi.Core/Services/`
3. Register service in `Program.cs`
4. Add unit tests in `tests/Privatekonomi.Core.Tests/`
5. Create UI component in `Privatekonomi.Web/Components/Pages/`
6. Update documentation (README, /docs/ as appropriate)

### Adding a New API Endpoint

1. Create controller in `Privatekonomi.Api/Controllers/`
2. Follow RESTful conventions
3. Add authorization attributes
4. Document with XML comments
5. Add tests in `tests/Privatekonomi.Api.Tests/`

### Database Changes

1. Update model in `Privatekonomi.Core/Models/`
2. Add migration: `dotnet ef migrations add [MigrationName] --project src/Privatekonomi.Core`
3. Update seed data if needed
4. Test with different providers (InMemory, SQLite)

## Configuration

### appsettings.json Structure

```json
{
  "Storage": {
    "Provider": "InMemory|Sqlite|SqlServer|JsonFile",
    "ConnectionString": "connection string or path",
    "SeedTestData": true|false
  }
}
```

See example files:
- `appsettings.JsonFile.example.json`
- `appsettings.SqlServer.example.json`
- `appsettings.RaspberryPi.example.json`

## Resources

### Key Documentation Files

- `README.md` - Project overview and getting started
- `docs/STORAGE_GUIDE.md` - Storage configuration guide
- `docs/DEVELOPER_QUICKSTART.md` - Developer setup guide
- `docs/AUTOMATIC_CATEGORIZATION.md` - Transaction categorization
- `docs/DARK_MODE_IMPLEMENTATION.md` - UI theming guide
- `docs/ASPIRE_GUIDE.md` - .NET Aspire integration
- `docs/CSV_IMPORT_GUIDE.md` - Import functionality
- `docs/PSD2_API_GUIDE.md` - Bank integration

### External Resources

- [MudBlazor Documentation](https://mudblazor.com/)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

## Best Practices

1. **Make minimal changes:** Only modify what's necessary for the task
2. **Follow existing patterns:** Look at similar code before implementing
3. **Test your changes:** Run unit tests and manually test in the UI
4. **Update documentation:** Keep README and guides in sync
5. **Consider Swedish users:** This is a Swedish personal finance app
6. **Support multiple storage providers:** Test with different database configurations
7. **Accessibility matters:** Ensure UI changes support keyboard navigation and screen readers
8. **Security first:** All user data must be properly scoped and protected

## Known Limitations

- Test coverage is currently low (priority: add more unit tests)
- Some nullable reference warnings exist (priority: fix these)
- InMemory database is default for development (easy to switch to SQLite for persistence)

## Getting Help

- Check existing documentation in `docs/`
- Look at similar implementations in the codebase
- Review test files for usage examples
- Check `docs/ISSUE_EXAMPLES.md` for common issue patterns
