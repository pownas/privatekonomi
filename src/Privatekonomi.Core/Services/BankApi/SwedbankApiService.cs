using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Privatekonomi.Core.Services.BankApi;

/// <summary>
/// Swedbank PSD2 API integration
/// Note: This is a simplified implementation. Production use requires:
/// - Valid PSD2 client credentials from Swedbank
/// - eIDAS certificate for authentication
/// - Proper error handling and token refresh logic
/// </summary>
public class SwedbankApiService : BankApiServiceBase
{
    private const string BaseUrl = "https://psd2.api.swedbank.com/sandbox"; // Sandbox URL
    private const string AuthUrl = "https://psd2.auth.swedbank.com";
    
    private readonly string _clientId;
    private readonly string _clientSecret;

    public override string BankName => "Swedbank";

    public SwedbankApiService(PrivatekonomyContext context, HttpClient httpClient, string clientId, string clientSecret) 
        : base(context, httpClient)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public override Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        // OAuth2 authorization code flow
        var authUrl = $"{AuthUrl}/oauth2/authorize?" +
            $"client_id={Uri.EscapeDataString(_clientId)}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"response_type=code&" +
            $"scope=AIS&" + // Account Information Services
            $"state={Uri.EscapeDataString(state)}";
        
        return Task.FromResult(authUrl);
    }

    public override async Task<BankConnection> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        var requestData = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri },
            { "client_id", _clientId },
            { "client_secret", _clientSecret }
        };

        var content = new FormUrlEncodedContent(requestData);
        var response = await HttpClient.PostAsync($"{AuthUrl}/oauth2/token", content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            throw new InvalidOperationException("Failed to obtain access token");

        return new BankConnection
        {
            ApiType = "PSD2",
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
    }

    public override async Task<BankConnection> RefreshTokenAsync(BankConnection connection)
    {
        if (string.IsNullOrEmpty(connection.RefreshToken))
            throw new InvalidOperationException("No refresh token available");

        var requestData = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", connection.RefreshToken },
            { "client_id", _clientId },
            { "client_secret", _clientSecret }
        };

        var content = new FormUrlEncodedContent(requestData);
        var response = await HttpClient.PostAsync($"{AuthUrl}/oauth2/token", content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            throw new InvalidOperationException("Failed to refresh access token");

        connection.AccessToken = tokenResponse.AccessToken;
        connection.RefreshToken = tokenResponse.RefreshToken ?? connection.RefreshToken;
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        connection.UpdatedAt = DateTime.UtcNow;

        return connection;
    }

    public override async Task<List<BankApiAccount>> GetAccountsAsync(BankConnection connection)
    {
        await EnsureValidTokenAsync(connection);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v1/accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var accountsResponse = JsonSerializer.Deserialize<AccountsResponse>(jsonResponse);

        if (accountsResponse?.Accounts == null)
            return new List<BankApiAccount>();

        return accountsResponse.Accounts.Select(a => new BankApiAccount
        {
            AccountId = a.ResourceId ?? a.Iban ?? string.Empty,
            AccountName = a.Name ?? "Konto",
            Iban = a.Iban,
            Currency = a.Currency ?? "SEK",
            AccountType = MapAccountType(a.CashAccountType)
        }).ToList();
    }

    public override async Task<List<BankApiTransaction>> GetTransactionsAsync(
        BankConnection connection,
        string accountId,
        DateTime fromDate,
        DateTime toDate)
    {
        await EnsureValidTokenAsync(connection);

        var dateFrom = fromDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
        var dateTo = toDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
        
        var url = $"{BaseUrl}/v1/accounts/{accountId}/transactions?dateFrom={dateFrom}&dateTo={dateTo}&bookingStatus=booked";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var transactionsResponse = JsonSerializer.Deserialize<TransactionsResponse>(jsonResponse);

        if (transactionsResponse?.Transactions?.Booked == null)
            return new List<BankApiTransaction>();

        return transactionsResponse.Transactions.Booked.Select(t => new BankApiTransaction
        {
            TransactionId = t.TransactionId ?? Guid.NewGuid().ToString(),
            Date = DateTime.Parse(t.ValueDate ?? t.BookingDate ?? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture), CultureInfo.CurrentCulture),
            BookingDate = DateTime.TryParse(t.BookingDate, out var bookingDate) ? bookingDate : null,
            Amount = decimal.Parse(t.TransactionAmount?.Amount ?? "0", CultureInfo.CurrentCulture),
            Currency = t.TransactionAmount?.Currency ?? "SEK",
            Description = t.RemittanceInformationUnstructured ?? t.AdditionalInformation ?? "Transaction",
            Creditor = t.CreditorName,
            Debtor = t.DebtorName,
            Reference = t.EntryReference,
            IsIncome = decimal.Parse(t.TransactionAmount?.Amount ?? "0", CultureInfo.CurrentCulture) > 0,
            AccountId = accountId
        }).ToList();
    }

    private async Task EnsureValidTokenAsync(BankConnection connection)
    {
        if (connection.TokenExpiresAt.HasValue && connection.TokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
        {
            await RefreshTokenAsync(connection);
            Context.BankConnections.Update(connection);
            await Context.SaveChangesAsync();
        }
    }

    private string MapAccountType(string? cashAccountType)
    {
        return cashAccountType?.ToLower(CultureInfo.CurrentCulture) switch
        {
            "cacc" => "checking",
            "svgs" => "savings",
            "card" => "credit_card",
            _ => "checking"
        };
    }

    // DTOs for JSON serialization
    private sealed class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }

    private sealed class AccountsResponse
    {
        public List<AccountDto> Accounts { get; set; } = new();
    }

    private sealed class AccountDto
    {
        public string? ResourceId { get; set; }
        public string? Iban { get; set; }
        public string? Name { get; set; }
        public string? Currency { get; set; }
        public string? CashAccountType { get; set; }
    }

    private sealed class TransactionsResponse
    {
        public TransactionsContainer? Transactions { get; set; }
    }

    private sealed class TransactionsContainer
    {
        public List<TransactionDto> Booked { get; set; } = new();
    }

    private sealed class TransactionDto
    {
        public string? TransactionId { get; set; }
        public string? BookingDate { get; set; }
        public string? ValueDate { get; set; }
        public AmountDto? TransactionAmount { get; set; }
        public string? RemittanceInformationUnstructured { get; set; }
        public string? AdditionalInformation { get; set; }
        public string? CreditorName { get; set; }
        public string? DebtorName { get; set; }
        public string? EntryReference { get; set; }
    }

    private sealed class AmountDto
    {
        public string Amount { get; set; } = "0";
        public string Currency { get; set; } = "SEK";
    }
}
