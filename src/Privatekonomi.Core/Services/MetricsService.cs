using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using System.Text.Json;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Implementation of metrics service for admin dashboard
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly PrivatekonomyContext _context;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(
        PrivatekonomyContext context,
        ILogger<MetricsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdminMetrics> GetCurrentMetricsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var startOfDay = now.Date;

        var metrics = new AdminMetrics
        {
            UserMetrics = await CalculateUserMetricsAsync(startOfMonth, startOfLastMonth, startOfDay, now),
            EngagementMetrics = await CalculateEngagementMetricsAsync(startOfMonth, now),
            PerformanceMetrics = await CalculatePerformanceMetricsAsync(startOfMonth, now),
            SecurityMetrics = await CalculateSecurityMetricsAsync(startOfMonth, now),
            CalculatedAt = now
        };

        return metrics;
    }

    public async Task<AdminMetrics> GetMetricsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        var metrics = new AdminMetrics
        {
            UserMetrics = await CalculateUserMetricsForPeriodAsync(startDate, endDate),
            EngagementMetrics = await CalculateEngagementMetricsForPeriodAsync(startDate, endDate),
            PerformanceMetrics = await CalculatePerformanceMetricsForPeriodAsync(startDate, endDate),
            SecurityMetrics = await CalculateSecurityMetricsForPeriodAsync(startDate, endDate),
            CalculatedAt = DateTime.UtcNow
        };

        return metrics;
    }

    public async Task<List<MetricsSnapshot>> GetHistoricalMetricsAsync(MetricsPeriodType periodType, int count = 12)
    {
        var snapshots = new List<MetricsSnapshot>();
        var now = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            DateTime periodStart, periodEnd;
            
            switch (periodType)
            {
                case MetricsPeriodType.Monthly:
                    periodStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                    periodEnd = periodStart.AddMonths(1).AddDays(-1);
                    break;
                    
                case MetricsPeriodType.Quarterly:
                    var currentQuarter = (now.Month - 1) / 3;
                    var quarterStartMonth = currentQuarter * 3 + 1;
                    periodStart = new DateTime(now.Year, quarterStartMonth, 1).AddMonths(-i * 3);
                    periodEnd = periodStart.AddMonths(3).AddDays(-1);
                    break;
                    
                case MetricsPeriodType.Weekly:
                    periodStart = now.Date.AddDays(-(int)now.DayOfWeek).AddDays(-i * 7);
                    periodEnd = periodStart.AddDays(6);
                    break;
                    
                case MetricsPeriodType.Daily:
                    periodStart = now.Date.AddDays(-i);
                    periodEnd = periodStart.AddDays(1).AddSeconds(-1);
                    break;
                    
                default:
                    periodStart = new DateTime(now.Year, 1, 1).AddYears(-i);
                    periodEnd = periodStart.AddYears(1).AddDays(-1);
                    break;
            }

            var metrics = await GetMetricsForPeriodAsync(periodStart, periodEnd);
            
            snapshots.Add(new MetricsSnapshot
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                PeriodType = periodType,
                MetricsJson = JsonSerializer.Serialize(metrics),
                CreatedAt = now
            });
        }

        return snapshots.OrderBy(s => s.PeriodStart).ToList();
    }

    private async Task<UserMetrics> CalculateUserMetricsAsync(
        DateTime startOfMonth, 
        DateTime startOfLastMonth, 
        DateTime startOfDay,
        DateTime now)
    {
        // Get all users
        var allUsers = await _context.Users.ToListAsync();
        var totalUsers = allUsers.Count;

        // MAU - users who logged in this month
        var mauUsers = allUsers.Where(u => 
            u.LastLoginAt.HasValue && 
            u.LastLoginAt.Value >= startOfMonth).ToList();
        var mau = mauUsers.Count;

        // Previous MAU
        var previousMau = allUsers.Count(u => 
            u.LastLoginAt.HasValue && 
            u.LastLoginAt.Value >= startOfLastMonth && 
            u.LastLoginAt.Value < startOfMonth);

        // MAU Growth
        var mauGrowth = previousMau > 0 
            ? ((decimal)(mau - previousMau) / previousMau) * 100 
            : 0;

        // DAU - users who logged in today
        var dau = allUsers.Count(u => 
            u.LastLoginAt.HasValue && 
            u.LastLoginAt.Value >= startOfDay);

        // DAU/MAU Ratio
        var dauMauRatio = mau > 0 ? ((decimal)dau / mau) * 100 : 0;

        // New users this month
        var newUsersThisMonth = allUsers.Count(u => u.CreatedAt >= startOfMonth);

        // 30-day retention (users who logged in 30+ days ago and again in last 30 days)
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);
        var usersCreated30To60DaysAgo = allUsers
            .Where(u => u.CreatedAt >= sixtyDaysAgo && u.CreatedAt < thirtyDaysAgo)
            .ToList();
        
        var retainedUsers = usersCreated30To60DaysAgo
            .Count(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= thirtyDaysAgo);
        
        var retentionRate = usersCreated30To60DaysAgo.Any() 
            ? ((decimal)retainedUsers / usersCreated30To60DaysAgo.Count) * 100 
            : 0;

        // Churn rate - users who haven't logged in this month but were active last month
        // Users active last month
        var activeLastMonth = allUsers.Where(u => 
            u.LastLoginAt.HasValue && 
            u.LastLoginAt.Value >= startOfLastMonth && 
            u.LastLoginAt.Value < startOfMonth).ToList();
        
        // Churned users are those active last month but not this month
        var churnedUsers = activeLastMonth.Count(u => 
            !u.LastLoginAt.HasValue || u.LastLoginAt.Value < startOfMonth);
        
        var churnRate = activeLastMonth.Any() 
            ? ((decimal)churnedUsers / activeLastMonth.Count) * 100 
            : 0;

        return new UserMetrics
        {
            MAU = mau,
            PreviousMAU = previousMau,
            MAUGrowthPercent = mauGrowth,
            DAU = dau,
            DAUMAURatio = dauMauRatio,
            RetentionRate30Day = retentionRate,
            MonthlyChurnRate = churnRate,
            TotalUsers = totalUsers,
            NewUsersThisMonth = newUsersThisMonth
        };
    }

    private async Task<UserMetrics> CalculateUserMetricsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        var allUsers = await _context.Users.ToListAsync();
        var periodUsers = allUsers.Where(u => 
            u.LastLoginAt.HasValue && 
            u.LastLoginAt.Value >= startDate && 
            u.LastLoginAt.Value <= endDate).ToList();

        return new UserMetrics
        {
            MAU = periodUsers.Count,
            TotalUsers = allUsers.Count,
            NewUsersThisMonth = allUsers.Count(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate),
            // Simplified metrics for historical periods
            DAU = 0,
            DAUMAURatio = 0,
            RetentionRate30Day = 0,
            MonthlyChurnRate = 0,
            PreviousMAU = 0,
            MAUGrowthPercent = 0
        };
    }

    private async Task<EngagementMetrics> CalculateEngagementMetricsAsync(DateTime startOfMonth, DateTime now)
    {
        var activeUsers = await _context.Users
            .Where(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= startOfMonth)
            .CountAsync();

        var transactionsThisMonth = await _context.Transactions
            .Where(t => t.Date >= startOfMonth && t.Date <= now)
            .CountAsync();

        var transactionsPerUser = activeUsers > 0 
            ? (decimal)transactionsThisMonth / activeUsers 
            : 0;

        // Feature adoption: Calculate percentage of users who have used key features
        var usersWithBudgets = await _context.Budgets
            .Where(b => b.StartDate >= startOfMonth)
            .Select(b => b.UserId)
            .Distinct()
            .CountAsync();

        var usersWithGoals = await _context.Goals
            .Where(g => g.CreatedAt >= startOfMonth)
            .Select(g => g.UserId)
            .Distinct()
            .CountAsync();

        var featureUsers = Math.Max(usersWithBudgets, usersWithGoals);
        var featureAdoption = activeUsers > 0 
            ? ((decimal)featureUsers / activeUsers) * 100 
            : 0;

        // Mock NPS for now (would need user feedback system)
        var nps = 65m;

        // Average session duration (mock - would need real session tracking)
        var avgSessionDuration = 320.0; // ~5 minutes in seconds

        return new EngagementMetrics
        {
            TransactionsPerUser = transactionsPerUser,
            AverageSessionDuration = avgSessionDuration,
            FeatureAdoptionRate = featureAdoption,
            NPS = nps,
            TotalTransactionsThisMonth = transactionsThisMonth,
            ActiveFeaturesCount = 15 // Mock count of active features
        };
    }

    private async Task<EngagementMetrics> CalculateEngagementMetricsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        var activeUsers = await _context.Users
            .Where(u => u.LastLoginAt.HasValue && 
                       u.LastLoginAt.Value >= startDate && 
                       u.LastLoginAt.Value <= endDate)
            .CountAsync();

        var transactions = await _context.Transactions
            .Where(t => t.Date >= startDate && t.Date <= endDate)
            .CountAsync();

        return new EngagementMetrics
        {
            TransactionsPerUser = activeUsers > 0 ? (decimal)transactions / activeUsers : 0,
            TotalTransactionsThisMonth = transactions,
            AverageSessionDuration = 320.0,
            FeatureAdoptionRate = 0,
            NPS = 65m,
            ActiveFeaturesCount = 15
        };
    }

    private async Task<PerformanceMetrics> CalculatePerformanceMetricsAsync(DateTime startOfMonth, DateTime now)
    {
        // Mock performance metrics - would need real monitoring integration
        // In a real implementation, these would come from Application Insights, New Relic, etc.
        
        var errorLogs = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startOfMonth && 
                       EF.Functions.Like(a.Action, "%error%"))
            .CountAsync();

        return new PerformanceMetrics
        {
            UptimePercent = 99.95m,
            AveragePageLoadTime = 1.2,
            LighthouseScore = 94,
            CrashRate = 0.01m,
            ErrorCount = errorLogs
        };
    }

    private async Task<PerformanceMetrics> CalculatePerformanceMetricsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        var errorLogs = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startDate && 
                       a.CreatedAt <= endDate &&
                       EF.Functions.Like(a.Action, "%error%"))
            .CountAsync();

        return new PerformanceMetrics
        {
            UptimePercent = 99.95m,
            AveragePageLoadTime = 1.2,
            LighthouseScore = 94,
            CrashRate = 0.01m,
            ErrorCount = errorLogs
        };
    }

    private async Task<SecurityMetrics> CalculateSecurityMetricsAsync(DateTime startOfMonth, DateTime now)
    {
        var totalUsers = await _context.Users.CountAsync();
        
        // Mock 2FA adoption - would need TwoFactorEnabled from Identity
        var twoFactorUsers = await _context.Users
            .Where(u => u.TwoFactorEnabled)
            .CountAsync();
        
        var twoFactorAdoption = totalUsers > 0 
            ? ((decimal)twoFactorUsers / totalUsers) * 100 
            : 0;

        // Failed login attempts - would need audit logging of login attempts
        var totalLoginAttempts = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startOfMonth && 
                       (EF.Functions.Like(a.Action, "%login%") ||
                        EF.Functions.Like(a.Action, "%signin%")))
            .CountAsync();

        var failedLoginAttempts = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startOfMonth && 
                       EF.Functions.Like(a.Action, "%failed login%"))
            .CountAsync();

        var failedLoginPercent = totalLoginAttempts > 0 
            ? ((decimal)failedLoginAttempts / totalLoginAttempts) * 100 
            : 0;

        // Security incidents
        var securityIncidents = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startOfMonth && 
                       EF.Functions.Like(a.Action, "%security%"))
            .CountAsync();

        return new SecurityMetrics
        {
            TwoFactorAdoptionRate = twoFactorAdoption,
            FailedLoginAttemptsPercent = failedLoginPercent,
            SecurityIncidentsCount = securityIncidents,
            GDPRCompliancePercent = 100m,
            TotalLoginAttempts = totalLoginAttempts,
            FailedLoginAttempts = failedLoginAttempts
        };
    }

    private async Task<SecurityMetrics> CalculateSecurityMetricsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        var totalUsers = await _context.Users.CountAsync();
        var twoFactorUsers = await _context.Users.Where(u => u.TwoFactorEnabled).CountAsync();
        
        var loginAttempts = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startDate && 
                       a.CreatedAt <= endDate &&
                       (EF.Functions.Like(a.Action, "%login%") ||
                        EF.Functions.Like(a.Action, "%signin%")))
            .CountAsync();

        var failedLogins = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startDate && 
                       a.CreatedAt <= endDate &&
                       EF.Functions.Like(a.Action, "%failed login%"))
            .CountAsync();

        return new SecurityMetrics
        {
            TwoFactorAdoptionRate = totalUsers > 0 ? ((decimal)twoFactorUsers / totalUsers) * 100 : 0,
            FailedLoginAttemptsPercent = loginAttempts > 0 ? ((decimal)failedLogins / loginAttempts) * 100 : 0,
            SecurityIncidentsCount = 0,
            GDPRCompliancePercent = 100m,
            TotalLoginAttempts = loginAttempts,
            FailedLoginAttempts = failedLogins
        };
    }
}
