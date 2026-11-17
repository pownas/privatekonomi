using System.Text.Json;

using Microsoft.Extensions.Logging;

using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public class YahooFinanceStockPriceService : IStockPriceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooFinanceStockPriceService> _logger;
    private readonly IInvestmentService _investmentService;
    
    public YahooFinanceStockPriceService(
        HttpClient httpClient, 
        ILogger<YahooFinanceStockPriceService> logger,
        IInvestmentService investmentService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _investmentService = investmentService;
    }
    
    public async Task<bool> UpdatePriceAsync(Investment investment)
    {
        try
        {
            // Try to get the ticker symbol from the investment
            var symbol = GetTickerSymbol(investment);
            if (string.IsNullOrEmpty(symbol))
            {
                _logger.LogWarning("Could not determine ticker symbol for {Name}", investment.Name);
                return false;
            }
            
            // Fetch the current price from Yahoo Finance
            var price = await FetchPriceFromYahooFinanceAsync(symbol);
            if (price.HasValue)
            {
                investment.CurrentPrice = price.Value;
                investment.LastUpdated = DateTime.Now;
                await _investmentService.UpdateInvestmentAsync(investment);
                
                _logger.LogInformation("Updated price for {Name} ({Symbol}): {Price}", 
                    investment.Name, symbol, price.Value);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price for {Name}", investment.Name);
            return false;
        }
    }
    
    public async Task<StockPriceUpdateResult> UpdatePricesAsync(IEnumerable<Investment> investments)
    {
        var result = new StockPriceUpdateResult();
        
        foreach (var investment in investments)
        {
            var success = await UpdatePriceAsync(investment);
            if (success)
            {
                result.SuccessCount++;
            }
            else
            {
                result.FailedCount++;
                result.Errors.Add($"Kunde inte uppdatera {investment.Name}");
            }
            
            // Add a small delay to avoid rate limiting
            await Task.Delay(500);
        }
        
        return result;
    }
    
    private string? GetTickerSymbol(Investment investment)
    {
        // Priority 1: Use short name if it looks like a ticker symbol (typically 3-5 uppercase letters)
        if (!string.IsNullOrEmpty(investment.ShortName) && 
            investment.ShortName.Length >= 2 && 
            investment.ShortName.Length <= 10 &&
            investment.ShortName.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-'))
        {
            return FormatTickerForMarket(investment.ShortName, investment.Market);
        }
        
        // Priority 2: Try to extract from name (e.g., "Microsoft (MSFT)")
        var match = System.Text.RegularExpressions.Regex.Match(investment.Name, @"\(([A-Z]{2,10})\)");
        if (match.Success)
        {
            return FormatTickerForMarket(match.Groups[1].Value, investment.Market);
        }
        
        return null;
    }
    
    private string FormatTickerForMarket(string ticker, string? market)
    {
        // For Swedish stocks, add .ST suffix (Stockholm Stock Exchange)
        if (market?.Contains("Stockholm", StringComparison.OrdinalIgnoreCase) == true ||
            market?.Contains("Sverige", StringComparison.OrdinalIgnoreCase) == true)
        {
            return $"{ticker}.ST";
        }
        
        // For other Nordic markets
        if (market?.Contains("Helsinki", StringComparison.OrdinalIgnoreCase) == true)
        {
            return $"{ticker}.HE";
        }
        if (market?.Contains("Copenhagen", StringComparison.OrdinalIgnoreCase) == true)
        {
            return $"{ticker}.CO";
        }
        if (market?.Contains("Oslo", StringComparison.OrdinalIgnoreCase) == true)
        {
            return $"{ticker}.OL";
        }
        
        // Default: use ticker as-is (for US stocks, etc.)
        return ticker;
    }
    
    private async Task<decimal?> FetchPriceFromYahooFinanceAsync(string symbol)
    {
        try
        {
            // Use Yahoo Finance Query API (v8)
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=1d";
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo Finance API returned status {StatusCode} for symbol {Symbol}", 
                    response.StatusCode, symbol);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            // Navigate to the price in the JSON structure
            var root = doc.RootElement;
            if (root.TryGetProperty("chart", out var chart) &&
                chart.TryGetProperty("result", out var results) &&
                results.GetArrayLength() > 0)
            {
                var result = results[0];
                
                // Try to get the regular market price
                if (result.TryGetProperty("meta", out var meta) &&
                    meta.TryGetProperty("regularMarketPrice", out var priceElement))
                {
                    if (priceElement.TryGetDecimal(out var price))
                    {
                        return price;
                    }
                    else if (priceElement.TryGetDouble(out var priceDouble))
                    {
                        return (decimal)priceDouble;
                    }
                }
                
                // Fallback: try to get the last close price
                if (result.TryGetProperty("indicators", out var indicators) &&
                    indicators.TryGetProperty("quote", out var quotes) &&
                    quotes.GetArrayLength() > 0)
                {
                    var quote = quotes[0];
                    if (quote.TryGetProperty("close", out var closes) &&
                        closes.GetArrayLength() > 0)
                    {
                        var lastClose = closes[closes.GetArrayLength() - 1];
                        if (lastClose.ValueKind != JsonValueKind.Null)
                        {
                            if (lastClose.TryGetDecimal(out var closePrice))
                            {
                                return closePrice;
                            }
                            else if (lastClose.TryGetDouble(out var closePriceDouble))
                            {
                                return (decimal)closePriceDouble;
                            }
                        }
                    }
                }
            }
            
            _logger.LogWarning("Could not extract price from Yahoo Finance response for symbol {Symbol}", symbol);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price from Yahoo Finance for symbol {Symbol}", symbol);
            return null;
        }
    }
}
