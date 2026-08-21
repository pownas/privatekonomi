using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Privatekonomi.Core.Services.BankApi;

/// <summary>
/// Avanza Bank API integration
/// Note: Avanza uses a proprietary API, not PSD2. This implementation uses:
/// - Username/password authentication (TOTP for 2FA may be required)
/// - Session-based authentication with cookies
/// - API is unofficial and subject to change
/// </summary>
public class AvanzaApiService : BankApiServiceBase
{
    private const string BaseUrl = "https://www.avanza.se";
    private const string ApiVersion = "/_api";
    
    public override string BankName => "Avanza";

    public AvanzaApiService(PrivatekonomyContext context, HttpClient httpClient) 
        : base(context, httpClient)
    {
    }

    public override Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        // Avanza doesn't use OAuth2 - uses username/password
        // Return a custom URL that indicates manual credential entry is needed
        return Task.FromResult($"/bank-connections/avanza/login?redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}");
    }

    public override async Task<BankConnection> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        // For Avanza, "code" contains username:password:totp (if 2FA enabled)
        var parts = code.Split(':');
        if (parts.Length < 2)
            throw new InvalidOperationException("Invalid credentials format");

        var username = parts[0];
        var password = parts[1];
        var totp = parts.Length > 2 ? parts[2] : null;

        var loginData = new
        {
            username,
            password,
            maxInactiveMinutes = 240
        };

        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync($"{BaseUrl}{ApiVersion}/authentication/sessions/usercredentials", content);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // May need TOTP
            if (string.IsNullOrEmpty(totp))
                throw new InvalidOperationException("2FA required. Please provide TOTP code.");
                
            // TODO: Handle TOTP authentication
            throw new NotImplementedException("TOTP authentication not yet implemented");
        }

        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse);

        if (authResponse == null || string.IsNullOrEmpty(authResponse.AuthenticationSession))
            throw new InvalidOperationException("Failed to authenticate");

        // Extract session cookies
        var cookies = response.Headers.GetValues("Set-Cookie").FirstOrDefault();

        return new BankConnection
        {
            ApiType = "Proprietary",
            AccessToken = authResponse.AuthenticationSession,
            RefreshToken = cookies, // Store cookies for session persistence
            TokenExpiresAt = DateTime.UtcNow.AddMinutes(240),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
    }

    public override async Task<BankConnection> RefreshTokenAsync(BankConnection connection)
    {
        // Avanza sessions expire after inactivity, may need to re-authenticate
        // For now, we'll just check if the session is still valid
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{ApiVersion}/customer/all");
        AddAuthHeaders(request, connection);

        var response = await HttpClient.SendAsync(request);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            connection.Status = "Expired";
            throw new InvalidOperationException("Session expired. Please re-authenticate.");
        }

        connection.TokenExpiresAt = DateTime.UtcNow.AddMinutes(240);
        connection.UpdatedAt = DateTime.UtcNow;
        return connection;
    }

    public override async Task<List<BankApiAccount>> GetAccountsAsync(BankConnection connection)
    {
        await EnsureValidSessionAsync(connection);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{ApiVersion}/account-overview/overview/accountlist");
        AddAuthHeaders(request, connection);

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var accountsResponse = JsonSerializer.Deserialize<AvanzaAccountsResponse>(jsonResponse);

        if (accountsResponse?.Accounts == null)
            return new List<BankApiAccount>();

        return accountsResponse.Accounts.Select(a => new BankApiAccount
        {
            AccountId = a.Id,
            AccountName = a.Name ?? "Konto",
            AccountNumber = a.AccountNumber,
            Currency = "SEK",
            Balance = a.TotalBalance,
            AccountType = MapAccountType(a.AccountType)
        }).ToList();
    }

    public override async Task<List<BankApiTransaction>> GetTransactionsAsync(
        BankConnection connection,
        string accountId,
        DateTime fromDate,
        DateTime toDate)
    {
        await EnsureValidSessionAsync(connection);

        // Avanza API uses account ID and date range
        var url = $"{BaseUrl}{ApiVersion}/account-performance/transactions/{accountId}" +
            $"?from={fromDate:yyyy-MM-dd}&to={toDate:yyyy-MM-dd}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeaders(request, connection);

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var transactionsResponse = JsonSerializer.Deserialize<AvanzaTransactionsResponse>(jsonResponse);

        if (transactionsResponse?.Transactions == null)
            return new List<BankApiTransaction>();

        return transactionsResponse.Transactions.Select(t => new BankApiTransaction
        {
            TransactionId = t.TransactionId ?? Guid.NewGuid().ToString(),
            Date = DateTime.Parse(t.TransactionDate, CultureInfo.CurrentCulture),
            Amount = t.Amount,
            Currency = t.Currency ?? "SEK",
            Description = t.Description ?? t.TransactionType ?? "Transaction",
            Reference = t.OrderId,
            IsIncome = t.Amount > 0,
            AccountId = accountId
        }).ToList();
    }

    private async Task EnsureValidSessionAsync(BankConnection connection)
    {
        if (connection.TokenExpiresAt.HasValue && connection.TokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
        {
            await RefreshTokenAsync(connection);
            Context.BankConnections.Update(connection);
            await Context.SaveChangesAsync();
        }
    }

    private void AddAuthHeaders(HttpRequestMessage request, BankConnection connection)
    {
        if (!string.IsNullOrEmpty(connection.AccessToken))
        {
            request.Headers.Add("X-AuthenticationSession", connection.AccessToken);
        }
        
        if (!string.IsNullOrEmpty(connection.RefreshToken))
        {
            request.Headers.Add("Cookie", connection.RefreshToken);
        }
    }

    private string MapAccountType(string? accountType)
    {
        return accountType?.ToLower(CultureInfo.CurrentCulture) switch
        {
            "depå" => "investment",
            "sparkonto" => "savings",
            "isk" => "investment",
            "kf" => "investment",
            _ => "investment"
        };
    }

    // DTOs for JSON serialization
    private sealed class AuthResponse
    {
        public string AuthenticationSession { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public bool TwoFactorLogin { get; set; }
    }

    private sealed class AvanzaAccountsResponse
    {
        public List<AvanzaAccountDto> Accounts { get; set; } = new();
    }

    private sealed class AvanzaAccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountType { get; set; }
        public decimal TotalBalance { get; set; }
    }

    private sealed class AvanzaTransactionsResponse
    {
        public List<AvanzaTransactionDto> Transactions { get; set; } = new();
    }

    private sealed class AvanzaTransactionDto
    {
        public string? TransactionId { get; set; }
        public string TransactionDate { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Description { get; set; }
        public string? TransactionType { get; set; }
        public string? OrderId { get; set; }
    }
}
