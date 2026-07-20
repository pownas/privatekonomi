using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Globalization;

namespace Privatekonomi.Core.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly PrivatekonomyContext _context;
    private readonly IAuditLogService _auditLogService;

    // Known subscription patterns for auto-detection
    private static readonly string[] KnownSubscriptions = new[]
    {
        "netflix", "spotify", "hbo", "disney", "viaplay", "cmore", "apple music",
        "youtube premium", "google one", "icloud", "dropbox", "onedrive",
        "microsoft 365", "office 365", "adobe", "canva", "zoom",
        "gym", "fitness", "fitnesscenter", "24seven", "sats", "nordic wellness",
        "swish", "bankgiro", "autogiro"
    };

    public SubscriptionService(PrivatekonomyContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync(string userId)
    {
        return await _context.Subscriptions
            .Where(s => s.UserId == userId)
            .Include(s => s.Category)
            .Include(s => s.PriceHistory)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetActiveSubscriptionsAsync(string userId)
    {
        return await _context.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .Include(s => s.Category)
            .Include(s => s.PriceHistory)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Subscription?> GetSubscriptionByIdAsync(int subscriptionId, string userId)
    {
        return await _context.Subscriptions
            .Where(s => s.SubscriptionId == subscriptionId && s.UserId == userId)
            .Include(s => s.Category)
            .Include(s => s.PriceHistory)
            .FirstOrDefaultAsync();
    }

    public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
    {
        subscription.CreatedAt = DateTime.UtcNow;
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        
        await _auditLogService.LogAsync("Create", "Subscription", subscription.SubscriptionId, 
            $"Created subscription: {subscription.Name} - {subscription.Price:C} {subscription.BillingFrequency}", subscription.UserId);
        
        return subscription;
    }

    public async Task<Subscription> UpdateSubscriptionAsync(Subscription subscription)
    {
        subscription.UpdatedAt = DateTime.UtcNow;
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
        
        await _auditLogService.LogAsync("Update", "Subscription", subscription.SubscriptionId, 
            $"Updated subscription: {subscription.Name}", subscription.UserId);
        
        return subscription;
    }

    public async Task DeleteSubscriptionAsync(int subscriptionId, string userId)
    {
        var subscription = await GetSubscriptionByIdAsync(subscriptionId, userId);
        if (subscription != null)
        {
            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
            
            await _auditLogService.LogAsync("Delete", "Subscription", subscriptionId, 
                $"Deleted subscription: {subscription.Name}", userId);
        }
    }

    public async Task<decimal> GetMonthlySubscriptionCostAsync(string userId)
    {
        var subscriptions = await GetActiveSubscriptionsAsync(userId);
        decimal monthlyCost = 0;

        foreach (var subscription in subscriptions)
        {
            monthlyCost += subscription.BillingFrequency.ToLowerInvariant() switch
            {
                "monthly" => subscription.Price,
                "yearly" => subscription.Price / 12,
                "quarterly" => subscription.Price / 3,
                _ => 0
            };
        }

        return monthlyCost;
    }

    public async Task<decimal> GetYearlySubscriptionCostAsync(string userId)
    {
        var subscriptions = await GetActiveSubscriptionsAsync(userId);
        decimal yearlyCost = 0;

        foreach (var subscription in subscriptions)
        {
            yearlyCost += subscription.BillingFrequency.ToLowerInvariant() switch
            {
                "monthly" => subscription.Price * 12,
                "yearly" => subscription.Price,
                "quarterly" => subscription.Price * 4,
                _ => 0
            };
        }

        return yearlyCost;
    }

    public async Task<List<Subscription>> GetSubscriptionsDueSoonAsync(string userId, int daysAhead)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive && 
                       s.NextBillingDate != null && s.NextBillingDate <= cutoffDate)
            .Include(s => s.Category)
            .OrderBy(s => s.NextBillingDate)
            .ToListAsync();
    }

    public async Task<List<SubscriptionPriceHistory>> GetPriceChangesAsync(string userId)
    {
        return await _context.SubscriptionPriceHistory
            .Include(ph => ph.Subscription)
            .Where(ph => ph.Subscription != null && ph.Subscription.UserId == userId)
            .OrderByDescending(ph => ph.ChangeDate)
            .ToListAsync();
    }

    public async Task AddPriceChangeAsync(int subscriptionId, decimal newPrice, string? reason)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            throw new InvalidOperationException("Subscription not found");

        var priceHistory = new SubscriptionPriceHistory
        {
            SubscriptionId = subscriptionId,
            OldPrice = subscription.Price,
            NewPrice = newPrice,
            ChangeDate = DateTime.UtcNow,
            Reason = reason,
            NotificationSent = false,
            CreatedAt = DateTime.UtcNow
        };

        subscription.Price = newPrice;
        subscription.UpdatedAt = DateTime.UtcNow;

        _context.SubscriptionPriceHistory.Add(priceHistory);
        await _context.SaveChangesAsync();
        
        await _auditLogService.LogAsync("PriceChange", "Subscription", subscriptionId, 
            $"Price changed for {subscription.Name}: {priceHistory.OldPrice:C} -> {newPrice:C}", subscription.UserId);
    }

    public async Task<List<Subscription>> GetUnusedSubscriptionsAsync(string userId, int daysUnused = 45)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysUnused);
        return await _context.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive &&
                       (s.LastUsedDate == null || s.LastUsedDate < cutoffDate))
            .Include(s => s.Category)
            .OrderBy(s => s.LastUsedDate)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetSubscriptionsWithUpcomingCancellationDeadlineAsync(string userId, int daysAhead = 30)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive &&
                       s.CancellationDeadline != null &&
                       s.CancellationDeadline <= futureDate &&
                       s.CancellationDeadline >= DateTime.UtcNow)
            .Include(s => s.Category)
            .OrderBy(s => s.CancellationDeadline)
            .ToListAsync();
    }

    public async Task UpdateLastUsedDateAsync(int subscriptionId, DateTime? lastUsedDate)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            throw new InvalidOperationException("Subscription not found");

        subscription.LastUsedDate = lastUsedDate;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        await _auditLogService.LogAsync("UpdateLastUsed", "Subscription", subscriptionId,
            $"Updated last used date for {subscription.Name} to {lastUsedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "N/A"}", subscription.UserId);
    }

    public async Task<List<Subscription>> DetectSubscriptionsFromTransactionsAsync(string userId)
    {
        var detectedSubscriptions = new List<Subscription>();
        
        // Get all existing subscriptions for this user
        var existingSubscriptions = await GetActiveSubscriptionsAsync(userId);
        var existingNames = existingSubscriptions.Select(s => s.Name.ToLowerInvariant()).ToHashSet();
        
        // Get transactions from the last year
        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.Date >= oneYearAgo && !t.IsIncome)
            .OrderBy(t => t.Date)
            .ToListAsync();
        
        // Group transactions by payee/description
        var groupedTransactions = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.Payee ?? t.Description))
            .GroupBy(t => (t.Payee ?? t.Description).ToLowerInvariant().Trim())
            .Where(g => g.Count() >= 3) // At least 3 occurrences to consider it recurring
            .ToList();
        
        foreach (var group in groupedTransactions)
        {
            var transactionList = group.OrderBy(t => t.Date).ToList();
            
            // Check if amounts are consistent (within 10% variance)
            var avgAmount = transactionList.Average(t => t.Amount);
            var isConsistentAmount = transactionList.All(t => 
                Math.Abs(t.Amount - avgAmount) / avgAmount < 0.1m);
            
            if (!isConsistentAmount)
                continue;
            
            // Check if timing is regular (monthly, yearly, quarterly)
            var intervals = new List<int>();
            for (int i = 1; i < transactionList.Count; i++)
            {
                var daysBetween = (transactionList[i].Date - transactionList[i - 1].Date).Days;
                intervals.Add(daysBetween);
            }
            
            if (intervals.Count == 0)
                continue;
            
            var avgInterval = intervals.Average();
            var isRegular = intervals.All(i => Math.Abs(i - avgInterval) < 7); // Within a week variance
            
            if (!isRegular)
                continue;
            
            // Determine billing frequency
            string frequency = avgInterval switch
            {
                >= 350 and <= 380 => "Yearly",
                >= 85 and <= 95 => "Quarterly",
                >= 25 and <= 35 => "Monthly",
                _ => "Monthly" // Default
            };
            
            // Check if this subscription name is already registered
            var name = group.Key;
            if (existingNames.Contains(name))
                continue;
            
            // Check if it matches known subscription patterns
            var isKnownSubscription = KnownSubscriptions.Any(known => 
                name.Contains(known, StringComparison.OrdinalIgnoreCase));
            
            if (isKnownSubscription || group.Count() >= 4) // Either known or 4+ occurrences
            {
                var lastTransaction = transactionList.Last();
                detectedSubscriptions.Add(new Subscription
                {
                    Name = FormatSubscriptionName(name),
                    Description = $"Auto-discovered from recurring transactions",
                    Price = avgAmount,
                    Currency = lastTransaction.Currency,
                    BillingFrequency = frequency,
                    StartDate = transactionList.First().Date,
                    NextBillingDate = CalculateNextBillingDate(lastTransaction.Date, frequency),
                    IsActive = true,
                    AutoDetected = true,
                    DetectedFromTransactionId = lastTransaction.TransactionId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        
        return detectedSubscriptions;
    }

    public async Task<Subscription?> CreateSubscriptionFromTransactionAsync(int transactionId, string userId)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == userId);
        
        if (transaction == null)
            return null;
        
        var subscription = new Subscription
        {
            Name = FormatSubscriptionName(transaction.Payee ?? transaction.Description),
            Description = "Created from transaction",
            Price = transaction.Amount,
            Currency = transaction.Currency,
            BillingFrequency = "Monthly",
            StartDate = transaction.Date,
            NextBillingDate = transaction.Date.AddMonths(1),
            IsActive = true,
            AutoDetected = true,
            DetectedFromTransactionId = transactionId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        
        await _auditLogService.LogAsync("Create", "Subscription", subscription.SubscriptionId,
            $"Created subscription from transaction: {subscription.Name}", userId);
        
        return subscription;
    }

    private static string FormatSubscriptionName(string name)
    {
        // Clean up and capitalize the name
        name = name.Trim();
        if (name.Length > 0)
        {
            // Capitalize first letter
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }
        return name;
    }

    private static DateTime? CalculateNextBillingDate(DateTime lastDate, string frequency)
    {
        return frequency switch
        {
            "Monthly" => lastDate.AddMonths(1),
            "Quarterly" => lastDate.AddMonths(3),
            "Yearly" => lastDate.AddYears(1),
            _ => lastDate.AddMonths(1)
        };
    }
}
