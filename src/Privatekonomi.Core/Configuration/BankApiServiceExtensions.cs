using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Privatekonomi.Core.Data;
using Privatekonomi.Core.Services;
using Privatekonomi.Core.Services.BankApi;

namespace Privatekonomi.Core.Configuration;

/// <summary>
/// Extension methods for registering PSD2 bank API services
/// </summary>
public static class BankApiServiceExtensions
{
    /// <summary>
    /// Registers all PSD2 bank API services and dependencies
    /// </summary>
    public static IServiceCollection AddBankApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register HttpClient factory
        services.AddHttpClient();

        // Register security services
        services.AddMemoryCache();
        services.AddDataProtection();
        services.AddSingleton<ITokenEncryptionService, TokenEncryptionService>();
        services.AddSingleton<IOAuthStateService, OAuthStateService>();

        // Register bank connection service
        services.AddScoped<IBankConnectionService, BankConnectionService>();

        // Register individual bank API services
        RegisterSwedbankService(services, configuration);
        RegisterAvanzaService(services);
        RegisterIcaBankenService(services, configuration);

        // Register bank sync background service
        services.Configure<BankSyncSettings>(
            configuration.GetSection("BankSync"));
        services.AddHostedService<BankSyncBackgroundService>();

        return services;
    }

    private static void RegisterSwedbankService(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IBankApiService>(sp =>
        {
            var context = sp.GetRequiredService<PrivatekonomyContext>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();

            var clientId = configuration["Swedbank:ClientId"] ?? "demo-client-id";
            var clientSecret = configuration["Swedbank:ClientSecret"] ?? "demo-client-secret";

            return new SwedbankApiService(context, httpClient, clientId, clientSecret);
        });
    }

    private static void RegisterAvanzaService(IServiceCollection services)
    {
        services.AddScoped<IBankApiService>(sp =>
        {
            var context = sp.GetRequiredService<PrivatekonomyContext>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();

            return new AvanzaApiService(context, httpClient);
        });
    }

    private static void RegisterIcaBankenService(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IBankApiService>(sp =>
        {
            var context = sp.GetRequiredService<PrivatekonomyContext>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();

            var clientId = configuration["IcaBanken:ClientId"] ?? "demo-client-id";
            var clientSecret = configuration["IcaBanken:ClientSecret"] ?? "demo-client-secret";

            return new IcaBankenApiService(context, httpClient, clientId, clientSecret);
        });
    }
}
