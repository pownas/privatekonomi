using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Middleware;
using Privatekonomi.Api.Services;
using Privatekonomi.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() || string.Equals(builder.Environment.EnvironmentName, "Local", StringComparison.OrdinalIgnoreCase))
{
    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
}

// Raspberry Pi configuration - ensure we listen on all network interfaces
var isRaspberryPi = Environment.GetEnvironmentVariable("PRIVATEKONOMI_RASPBERRY_PI") == "true";
if (isRaspberryPi)
{
    // When running under Aspire, it manages the ports via WithHttpEndpoint
    // But Aspire binds to localhost by default, so we need to override this
    // We configure Kestrel AFTER AddServiceDefaults to ensure our binding takes precedence
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        // Listen on all network interfaces (0.0.0.0) for Raspberry Pi network access
        // Get port from Aspire's PORT env var, or fall back to configuration, or default to 5277
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5277;
        serverOptions.ListenAnyIP(port);
    });
}

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Configure ProblemDetails
builder.Services.AddProblemDetails();

// Register global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure storage based on appsettings
builder.Services.AddPrivatekonomyStorage(builder.Configuration);

// Register services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryRuleService, CategoryRuleService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IDebtStrategyService, DebtStrategyService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IBudgetSuggestionService, BudgetSuggestionService>();
builder.Services.AddScoped<IBudgetAlertService, BudgetAlertService>();
builder.Services.AddScoped<IBankSourceService, BankSourceService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddScoped<IChildAllowanceService, ChildAllowanceService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IGoalMilestoneService, GoalMilestoneService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IRoundUpService, RoundUpService>();
builder.Services.AddScoped<IPocketService, PocketService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISalaryHistoryService, SalaryHistoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IDashboardLayoutService, DashboardLayoutService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISavingsChallengeService, SavingsChallengeService>();
builder.Services.AddScoped<IBankConnectionService, BankConnectionService>();
builder.Services.AddSingleton<IOAuthStateService, OAuthStateService>();

// HTTP context accessor for current user service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, ApiCurrentUserService>();

// Register bank API services and dependencies
builder.Services.AddBankApiServices(builder.Configuration);

// Configure CORS for Blazor Web
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWeb", policy =>
    {
        policy.WithOrigins("https://localhost:5001", "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var storageSettings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageSettings>>().Value;
    var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
    
    // For SQLite, ensure database is created
    if (storageSettings.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogInformation("Using SQLite storage, ensuring database is created...");
        context.Database.EnsureCreated();
    }
    else
    {
        context.Database.EnsureCreated();
    }
    
    logger.LogInformation("Database initialized with provider: {Provider}", storageSettings.Provider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add exception handler middleware
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseCors("AllowBlazorWeb");
app.UseAuthorization();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();
app.MapGet("/internal/health", (HttpContext context) =>
    context.Connection.RemoteIpAddress is { } address && System.Net.IPAddress.IsLoopback(address)
        ? Results.Ok()
        : Results.NotFound());

app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
