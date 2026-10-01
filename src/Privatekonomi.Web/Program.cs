using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Web.Components;
using Privatekonomi.Web.Components.Account;
using Privatekonomi.Web.Services;
using System.Globalization;
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
        // Get port from Aspire's PORT env var, or fall back to configuration, or default to 5274
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5274;
        serverOptions.ListenAnyIP(port);
    });
}

// Configure Swedish culture as default
var swedishCulture = new CultureInfo("sv-SE");
CultureInfo.DefaultThreadCurrentCulture = swedishCulture;
CultureInfo.DefaultThreadCurrentUICulture = swedishCulture;

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add SignalR for real-time budget alerts
builder.Services.AddSignalR();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Blazor Server options for better error handling
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DetailedErrors = true;
    }
    // Increase disconnect and JS interop timeouts to prevent premature disconnects
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    // Increase max buffer size for large component updates
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
});

// Add MudBlazor services with configuration for better accessibility
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

// Configure storage based on appsettings
builder.Services.AddPrivatekonomyStorage(builder.Configuration);

// Add Identity services
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PiUpdateAdmin", policy => policy.RequireAuthenticatedUser()
        .AddRequirements(new PiUpdateAdminRequirement()));
builder.Services.AddScoped<IAuthorizationHandler, PiUpdateAdminHandler>();
builder.Services.AddSingleton<PiUpdateService>();
builder.Services.AddHttpClient("pi-update", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Privatekonomi-PiUpdate");
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<PrivatekonomyContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    options.ResidentKeyRequirement = "required";
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, NoOpEmailSender>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Add HttpContextAccessor for CurrentUserService
builder.Services.AddHttpContextAccessor();

// Conditionally register CurrentUserService based on DevDisableAuth feature flag
// This allows temporarily bypassing authentication in development for faster testing
if (builder.Environment.IsDevelopment() && 
    builder.Configuration.GetValue<bool>("FeatureFlags:DevDisableAuth"))
{
    builder.Services.AddScoped<ICurrentUserService, DevCurrentUserService>();
}
else
{
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
}

// Register services
builder.Services.AddScoped<INavigationPerformanceService, NavigationPerformanceService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryRuleService, CategoryRuleService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IDebtStrategyService, DebtStrategyService>();
builder.Services.AddScoped<IMortgageAnalysisService, MortgageAnalysisService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IBudgetSuggestionService, BudgetSuggestionService>();
builder.Services.AddScoped<IBankSourceService, BankSourceService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddScoped<IChildAllowanceService, ChildAllowanceService>();
builder.Services.AddScoped<IGoalMilestoneService, GoalMilestoneService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IRoundUpService, RoundUpService>();
builder.Services.AddScoped<IPocketService, PocketService>();
builder.Services.AddScoped<ISharedGoalService, SharedGoalService>();
builder.Services.AddScoped<ISocialFeatureService, SocialFeatureService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IHeatmapAnalysisService, HeatmapAnalysisService>();
builder.Services.AddScoped<ISalaryHistoryService, SalaryHistoryService>();
builder.Services.AddScoped<ICurrencyAccountService, CurrencyAccountService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPensionService, PensionService>();
builder.Services.AddScoped<IDividendService, DividendService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IBudgetAlertService, BudgetAlertService>();
builder.Services.AddScoped<ILifeTimelinePlannerService, LifeTimelinePlannerService>();
builder.Services.AddScoped<ISavingsChallengeService, SavingsChallengeService>();
builder.Services.AddScoped<IKonsumentverketComparisonService, KonsumentverketComparisonService>();
builder.Services.AddScoped<IKalpService, KalpService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<ICashFlowScenarioService, CashFlowScenarioService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<DashboardPreferencesService>();
builder.Services.AddScoped<IDashboardLayoutService, DashboardLayoutService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ViewDensityService>();

// Register background services
builder.Services.AddHostedService<Privatekonomi.Web.Services.BudgetAlertBackgroundService>();
builder.Services.AddHostedService<Privatekonomi.Web.Services.WeeklyBudgetDigestService>();
builder.Services.AddHostedService<Privatekonomi.Web.Services.BillReminderBackgroundService>();

// Swedish-specific services
builder.Services.AddScoped<ISieExporter, SieExporter>();
builder.Services.AddScoped<IK4Generator, K4Generator>();
builder.Services.AddScoped<ITaxDeductionService, TaxDeductionService>();
builder.Services.AddScoped<IISKTaxCalculator, ISKTaxCalculator>();

// Register bank API services and dependencies
builder.Services.AddBankApiServices(builder.Configuration);

// Add HttpClient for API calls
builder.Services.AddHttpClient("api", client =>
{
    // Configure API base address based on environment
    // Aspire will override this through service discovery when running with AppHost
    // For Raspberry Pi and standalone deployments, use configuration
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] 
        ?? builder.Configuration["services:api:http:0"] 
        ?? "http://localhost:5277";
    
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Configure default HttpClient to use the API client configuration
builder.Services.AddScoped<HttpClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    // Use the configured "api" client as the default
    return httpClientFactory.CreateClient("api");
});

// Register stock price service with HttpClient
builder.Services.AddHttpClient<IStockPriceService, YahooFinanceStockPriceService>();

var app = builder.Build();

// Configure request localization for Swedish
var supportedCultures = new[] { new CultureInfo("sv-SE") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("sv-SE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Seed the database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var storageSettings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageSettings>>().Value;
        
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // For SQLite and SqlServer, ensure database is created
        if (storageSettings.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) ||
            storageSettings.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Using {Provider} storage, ensuring database is created...", storageSettings.Provider);
            context.Database.EnsureCreated();
        }
        else
        {
            context.Database.EnsureCreated();
        }

        await PasskeySchemaInitializer.EnsurePasskeyTableAsync(context);
        
        // For JsonFile, load existing data
        if (storageSettings.Provider.Equals("JsonFile", StringComparison.OrdinalIgnoreCase))
        {
            var persistenceService = scope.ServiceProvider.GetService<Privatekonomi.Core.Services.Persistence.IDataPersistenceService>();
            if (persistenceService != null && persistenceService.Exists())
            {
                logger.LogInformation("Loading data from JSON files...");
                await persistenceService.LoadAsync(context);
                logger.LogInformation("Data loaded successfully from JSON files");
            }
        }
        
        // Always seed production reference data (challenge templates, etc.)
        logger.LogInformation("Seeding production reference data (challenge templates)...");
        TestDataSeeder.SeedProductionReferenceData(context);
        logger.LogInformation("Production reference data seeded successfully");
        
        // Seed test data only if configured
        if (storageSettings.SeedTestData)
        {
            logger.LogInformation("Seeding test data...");
            await TestDataSeeder.SeedTestDataAsync(context, userManager);
            logger.LogInformation("Database seeding completed successfully");
        }
        else
        {
            logger.LogInformation("Test data seeding is disabled in configuration");
        }
    }
}
catch (Exception ex)
{
    // Log the error but don't crash the application
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database. Application will continue without test data.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();
app.MapGet("/internal/health", (HttpContext context) =>
    context.Connection.RemoteIpAddress is { } address && System.Net.IPAddress.IsLoopback(address)
        ? Results.Ok()
        : Results.NotFound());

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/settings/pi-update/start", async (HttpContext context, IAntiforgery antiforgery, PiUpdateService updates) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
        return Results.BadRequest();

    if (!updates.IsAvailable)
        return Results.NotFound();

    if (!updates.TryRequest())
        return Results.Conflict();

    return Results.Redirect("/settings/pi-update");
}).RequireAuthorization("PiUpdateAdmin");

// Add Identity API endpoints (token-based auth for mobile/API clients)
app.MapGroup("/Account").MapIdentityApi<ApplicationUser>();

app.MapPost("/Account/PasskeyRequestOptions", async (
    HttpContext context,
    IAntiforgery antiforgery,
    SignInManager<ApplicationUser> signInManager) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
        return Results.BadRequest();

    var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user: null);
    return Results.Content(optionsJson, contentType: "application/json");
});

app.MapPost("/Account/Manage/PasskeyCreationOptions", async (
    HttpContext context,
    IAntiforgery antiforgery,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
        return Results.BadRequest();

    var user = await userManager.GetUserAsync(context.User);
    if (user is null)
        return Results.NotFound();

    var userId = await userManager.GetUserIdAsync(user);
    var userName = await userManager.GetUserNameAsync(user) ?? "User";
    var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new()
    {
        Id = userId,
        Name = userName,
        DisplayName = userName
    });

    return Results.Content(optionsJson, contentType: "application/json");
}).RequireAuthorization();

app.MapPost("/Account/Manage/PerformPasskeyCreation", async (
    HttpContext context,
    IAntiforgery antiforgery,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
        return Results.BadRequest();

    var request = await context.Request.ReadFromJsonAsync<PasskeyCredentialRequest>();
    if (string.IsNullOrWhiteSpace(request?.CredentialJson))
        return Results.BadRequest();

    var user = await userManager.GetUserAsync(context.User);
    if (user is null)
        return Results.NotFound();

    var passkeys = await userManager.GetPasskeysAsync(user);
    if (passkeys.Count >= 100)
        return Results.BadRequest("Kontot har nått gränsen för antal passkeys.");

    var result = await signInManager.PerformPasskeyAttestationAsync(request.CredentialJson);
    if (!result.Succeeded)
        return Results.BadRequest("Det gick inte att lägga till passkey.");

    var addResult = await userManager.AddOrUpdatePasskeyAsync(user, result.Passkey);
    return addResult.Succeeded ? Results.Ok() : Results.BadRequest("Det gick inte att spara passkey.");
}).RequireAuthorization();

app.MapPost("/Account/PerformPasskeyLogin", async (
    HttpContext context,
    IAntiforgery antiforgery,
    SignInManager<ApplicationUser> signInManager,
    ILogger<Program> logger) =>
{
    if (!await antiforgery.IsRequestValidAsync(context))
        return Results.BadRequest();

    var request = await context.Request.ReadFromJsonAsync<PasskeyLoginRequest>();
    if (string.IsNullOrWhiteSpace(request?.CredentialJson))
        return Results.BadRequest();

    SignInResult result;
    try
    {
        result = await signInManager.PasskeySignInAsync(request.CredentialJson);
    }
    catch (InvalidOperationException)
    {
        return Results.BadRequest("Passkey-sessionen har upphört. Försök igen.");
    }

    if (!result.Succeeded)
        return Results.Unauthorized();

    PasskeyLog.UserLoggedIn(logger);
    var returnUrl = request.ReturnUrl;
    var isLocalReturnUrl = !string.IsNullOrEmpty(returnUrl) &&
        (returnUrl.StartsWith("~/", StringComparison.Ordinal) ||
         (returnUrl.StartsWith('/') &&
          !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
          !returnUrl.StartsWith("/\\", StringComparison.Ordinal)));

    return Results.Ok(new { redirectUrl = isLocalReturnUrl ? returnUrl : "/" });
});

// Add custom cookie-based login/logout endpoints for Blazor Server
app.MapPost("/Account/PerformLogin", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager,
    ILogger<Program> logger) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();
    var rememberMe = form["RememberMe"].ToString() == "on" || form["RememberMe"].ToString() == "true";
    var returnUrl = form["ReturnUrl"].ToString();

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        return Results.Redirect("/Account/Login?error=empty");
    }

    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);

    if (result.Succeeded)
    {
        logger.LogInformation("User {Email} logged in.", email);
        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
    else if (result.IsLockedOut)
    {
        logger.LogWarning("User {Email} account locked out.", email);
        return Results.Redirect("/Account/Login?error=lockedout");
    }
    else
    {
        logger.LogWarning("Invalid login attempt for {Email}.", email);
        return Results.Redirect("/Account/Login?error=invalid");
    }
});

app.MapPost("/Account/PerformRegister", async (
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<Program> logger) =>
{
    var form = await context.Request.ReadFormAsync();
    var firstName = form["FirstName"].ToString();
    var lastName = form["LastName"].ToString();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();
    var confirmPassword = form["ConfirmPassword"].ToString();

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        return Results.Redirect("/Account/Register?error=empty");
    }

    if (password != confirmPassword)
    {
        return Results.Redirect("/Account/Register?error=passwordmismatch");
    }

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        FirstName = firstName,
        LastName = lastName,
        CreatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(user, password);

    if (result.Succeeded)
    {
        logger.LogInformation("User {Email} created a new account.", email);
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/onboarding");
    }
    else
    {
        var errors = string.Join(",", result.Errors.Select(e => e.Code));
        logger.LogWarning("Failed to create user {Email}: {Errors}", email, errors);
        return Results.Redirect($"/Account/Register?error=create&details={Uri.EscapeDataString(errors)}");
    }
});

// Local function for logout logic
async Task<IResult> PerformLogoutAsync(SignInManager<ApplicationUser> signInManager, ILogger<Program> logger)
{
    await signInManager.SignOutAsync();
    logger.LogInformation("User logged out.");
    return Results.Redirect("/Account/Login");
}

app.MapPost("/Account/PerformLogout", PerformLogoutAsync);
app.MapGet("/Account/PerformLogout", PerformLogoutAsync);

// Map SignalR hubs
app.MapHub<Privatekonomi.Web.Hubs.BudgetAlertHub>("/hubs/budgetalert");

app.Run();

internal sealed record PasskeyCredentialRequest(string CredentialJson);
internal sealed record PasskeyLoginRequest(string CredentialJson, string? ReturnUrl);

internal static partial class PasskeyLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "User logged in with a passkey.")]
    internal static partial void UserLoggedIn(ILogger logger);
}
