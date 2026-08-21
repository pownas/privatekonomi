using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Data;

public class PrivatekonomyContext : IdentityDbContext<ApplicationUser>
{
    public PrivatekonomyContext(DbContextOptions<PrivatekonomyContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<TransactionCategory> TransactionCategories { get; set; }
    public DbSet<CategoryRule> CategoryRules { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Investment> Investments { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<BudgetCategory> BudgetCategories { get; set; }
    public DbSet<BankSource> BankSources { get; set; }
    public DbSet<BankConnection> BankConnections { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Household> Households { get; set; }
    public DbSet<HouseholdMember> HouseholdMembers { get; set; }
    public DbSet<SharedExpense> SharedExpenses { get; set; }
    public DbSet<ExpenseShare> ExpenseShares { get; set; }
    public DbSet<HouseholdActivity> HouseholdActivities { get; set; }
    public DbSet<HouseholdActivityImage> HouseholdActivityImages { get; set; }
    public DbSet<HouseholdTask> HouseholdTasks { get; set; }
    public DbSet<HouseholdBudgetShare> HouseholdBudgetShares { get; set; }
    public DbSet<DebtSettlement> DebtSettlements { get; set; }
    public DbSet<ChildAllowance> ChildAllowances { get; set; }
    public DbSet<AllowanceTransaction> AllowanceTransactions { get; set; }
    public DbSet<AllowanceTask> AllowanceTasks { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<GoalMilestone> GoalMilestones { get; set; }
    public DbSet<SalaryHistory> SalaryHistories { get; set; }
    
    // Pockets for savings accounts
    public DbSet<Pocket> Pockets { get; set; }
    public DbSet<PocketTransaction> PocketTransactions { get; set; }
    
    // Shared Goals
    public DbSet<SharedGoal> SharedGoals { get; set; }
    public DbSet<SharedGoalParticipant> SharedGoalParticipants { get; set; }
    public DbSet<SharedGoalProposal> SharedGoalProposals { get; set; }
    public DbSet<SharedGoalProposalVote> SharedGoalProposalVotes { get; set; }
    public DbSet<SharedGoalTransaction> SharedGoalTransactions { get; set; }
    public DbSet<SharedGoalNotification> SharedGoalNotifications { get; set; }
    
    // Swedish-specific models
    public DbSet<TaxDeduction> TaxDeductions { get; set; }
    public DbSet<CapitalGain> CapitalGains { get; set; }
    public DbSet<CommuteDeduction> CommuteDeductions { get; set; }
    public DbSet<CreditRating> CreditRatings { get; set; }
    
    // Net Worth Tracking
    public DbSet<NetWorthSnapshot> NetWorthSnapshots { get; set; }
    
    // Currency Accounts
    public DbSet<CurrencyAccount> CurrencyAccounts { get; set; }
    
    // Receipts, Bills and Subscriptions
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<ReceiptLineItem> ReceiptLineItems { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPriceHistory> SubscriptionPriceHistory { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<BillReminder> BillReminders { get; set; }
    public DbSet<BillSchedule> BillSchedules { get; set; }
    
    // Investment-related entities
    public DbSet<Pension> Pensions { get; set; }
    public DbSet<Dividend> Dividends { get; set; }
    public DbSet<InvestmentTransaction> InvestmentTransactions { get; set; }
    public DbSet<PortfolioAllocation> PortfolioAllocations { get; set; }
    
    // ML-related entities
    public DbSet<MLModel> MLModels { get; set; }
    public DbSet<UserFeedback> UserFeedbacks { get; set; }
    
    // Savings Challenges
    public DbSet<SavingsChallenge> SavingsChallenges { get; set; }
    public DbSet<SavingsChallengeProgress> SavingsChallengeProgress { get; set; }
    public DbSet<ChallengeTemplate> ChallengeTemplates { get; set; }
    
    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<DoNotDisturbSchedule> DoNotDisturbSchedules { get; set; }
    public DbSet<NotificationIntegration> NotificationIntegrations { get; set; }
    
    // Life Timeline Planning
    public DbSet<LifeTimelineMilestone> LifeTimelineMilestones { get; set; }
    public DbSet<LifeTimelineScenario> LifeTimelineScenarios { get; set; }
    
    // Social Features
    public DbSet<GoalShare> GoalShares { get; set; }
    public DbSet<SavingsGroup> SavingsGroups { get; set; }
    public DbSet<SavingsGroupMember> SavingsGroupMembers { get; set; }
    public DbSet<GroupComment> GroupComments { get; set; }
    public DbSet<CommentLike> CommentLikes { get; set; }
    public DbSet<GroupGoal> GroupGoals { get; set; }
    public DbSet<UserPrivacySettings> UserPrivacySettings { get; set; }
    
    // Dashboard Layouts
    public DbSet<DashboardLayout> DashboardLayouts { get; set; }
    public DbSet<WidgetConfiguration> WidgetConfigurations { get; set; }
    
    // Round-up Savings
    public DbSet<RoundUpSettings> RoundUpSettings { get; set; }
    public DbSet<RoundUpTransaction> RoundUpTransactions { get; set; }
    
    // Budget Alerts
    public DbSet<BudgetAlert> BudgetAlerts { get; set; }
    public DbSet<BudgetAlertSettings> BudgetAlertSettings { get; set; }
    public DbSet<BudgetFreeze> BudgetFreezes { get; set; }
    
    // Budget Suggestions
    public DbSet<BudgetSuggestion> BudgetSuggestions { get; set; }
    public DbSet<BudgetSuggestionItem> BudgetSuggestionItems { get; set; }
    public DbSet<BudgetAdjustment> BudgetAdjustments { get; set; }
    
    // Reminders
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<ReminderSettings> ReminderSettings { get; set; }
    
    // RBAC - Role-Based Access Control
    public DbSet<HouseholdRole> HouseholdRoles { get; set; }
    public DbSet<RolePrivilege> RolePermissions { get; set; }
    public DbSet<AuditLogEntry> AuditLogEntries { get; set; }
    
    // Monthly Reports
    public DbSet<MonthlyReport> MonthlyReports { get; set; }
    public DbSet<ReportDelivery> ReportDeliveries { get; set; }
    public DbSet<ReportPreference> ReportPreferences { get; set; }
    
    // Import Jobs
    public DbSet<ImportJob> ImportJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BankSource>(entity =>
        {
            entity.HasKey(e => e.BankSourceId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
            entity.Property(e => e.Logo).HasMaxLength(500);
            entity.Property(e => e.AccountType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Institution).HasMaxLength(200);
            entity.Property(e => e.InitialBalance).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo });
            
            // Ignore computed property
            entity.Ignore(e => e.CurrentBalance);
        });

        builder.Entity<BankConnection>(entity =>
        {
            entity.HasKey(e => e.BankConnectionId);
            entity.Property(e => e.ApiType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ExternalAccountId).HasMaxLength(100);
            entity.Property(e => e.AccessToken).HasMaxLength(2000);
            entity.Property(e => e.RefreshToken).HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.BankSource)
                .WithMany()
                .HasForeignKey(e => e.BankSourceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.BankSourceId);
            entity.HasIndex(e => e.ExternalAccountId);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.CreatedAt);
        });

        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
            entity.Property(e => e.AccountNumber).HasMaxLength(10);
            entity.Property(e => e.DefaultBudgetMonthly).HasPrecision(18, 2);
            entity.Property(e => e.TaxRelated).IsRequired();
            entity.Property(e => e.IsSystemCategory).IsRequired();
            entity.Property(e => e.OriginalName).HasMaxLength(100);
            entity.Property(e => e.OriginalColor).HasMaxLength(7);
            entity.Property(e => e.OriginalAccountNumber).HasMaxLength(10);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Self-referencing relationship for hierarchical categories
            entity.HasOne(e => e.Parent)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CategoryRule>(entity =>
        {
            entity.HasKey(e => e.CategoryRuleId);
            entity.Property(e => e.Pattern).IsRequired().HasMaxLength(500);
            entity.Property(e => e.MatchType).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CaseSensitive).IsRequired();
            entity.Property(e => e.Field).IsRequired();
            entity.Property(e => e.RuleType).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // User relationship (nullable for system rules)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Self-referencing relationship for rule overrides
            entity.HasOne(e => e.OverridesSystemRule)
                .WithMany()
                .HasForeignKey(e => e.OverridesSystemRuleId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.IsActive, e.Priority });
            entity.HasIndex(e => e.RuleType);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.RuleType, e.UserId });
        });

        builder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Payee).HasMaxLength(200);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.ImportSource).HasMaxLength(100);
            entity.Property(e => e.Imported).IsRequired();
            entity.Property(e => e.Cleared).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            // Additional properties from OpenAPI spec
            entity.Property(e => e.Payee).HasMaxLength(200);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            
            entity.HasOne(e => e.BankSource)
                .WithMany(b => b.Transactions)
                .HasForeignKey(e => e.BankSourceId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Pocket)
                .WithMany()
                .HasForeignKey(e => e.PocketId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for performance optimization
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PocketId);
            entity.HasIndex(e => e.HouseholdId);
            entity.HasIndex(e => new { e.BankSourceId, e.Date });
            entity.HasIndex(e => new { e.HouseholdId, e.Date });
            entity.HasIndex(e => e.Payee);
            
            // Temporal indexes for efficient historical queries
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo });
        });

        builder.Entity<TransactionCategory>(entity =>
        {
            entity.HasKey(e => e.TransactionCategoryId);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Percentage).HasPrecision(5, 2);
            
            entity.HasOne(e => e.Transaction)
                .WithMany(t => t.TransactionCategories)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.TransactionCategories)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.LoanId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.InterestRate).HasPrecision(5, 2);
            entity.Property(e => e.Amortization).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.MinimumPayment).HasPrecision(18, 2);
            entity.Property(e => e.InstallmentFee).HasPrecision(18, 2);
            entity.Property(e => e.ExtraMonthlyPayment).HasPrecision(18, 2);
            entity.Property(e => e.Priority).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo });
            
            // Ignore computed properties
            entity.Ignore(e => e.CurrentBalance);
            entity.Ignore(e => e.UtilizationRate);
        });
        
        builder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.AssetId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PurchaseValue).HasPrecision(18, 2);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo });
            
            // Ignore computed properties
            entity.Ignore(e => e.ValueChange);
            entity.Ignore(e => e.ValueChangePercentage);
        });
        
        builder.Entity<Investment>(entity =>
        {
            entity.HasKey(e => e.InvestmentId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 2);
            entity.Property(e => e.PurchaseDate).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            // New properties for bank and account information
            entity.Property(e => e.AccountNumber).HasMaxLength(50);
            entity.Property(e => e.ShortName).HasMaxLength(50);
            entity.Property(e => e.ISIN).HasMaxLength(12);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.Country).HasMaxLength(2);
            entity.Property(e => e.Market).HasMaxLength(50);
            
            // Relation to BankSource
            entity.HasOne(e => e.BankSource)
                .WithMany()
                .HasForeignKey(e => e.BankSourceId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Indexes for faster searching
            entity.HasIndex(e => e.ISIN);
            entity.HasIndex(e => e.AccountNumber);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo });
            
            // Ignore computed properties
            entity.Ignore(e => e.TotalValue);
            entity.Ignore(e => e.TotalCost);
            entity.Ignore(e => e.ProfitLoss);
            entity.Ignore(e => e.ProfitLossPercentage);
        });

        builder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.BudgetId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.Period).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo });
            
            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BudgetCategory>(entity =>
        {
            entity.HasKey(e => e.BudgetCategoryId);
            entity.Property(e => e.PlannedAmount).HasPrecision(18, 2);
            entity.Property(e => e.RecurrencePeriodMonths).IsRequired().HasDefaultValue(1);
            
            entity.HasOne(e => e.Budget)
                .WithMany(b => b.BudgetCategories)
                .HasForeignKey(e => e.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Ignore computed property
            entity.Ignore(e => e.MonthlyAmount);
        });

        // Budget Suggestion configuration
        builder.Entity<BudgetSuggestion>(entity =>
        {
            entity.HasKey(e => e.BudgetSuggestionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalIncome).HasPrecision(18, 2);
            entity.Property(e => e.DistributionModel).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.AppliedBudget)
                .WithMany()
                .HasForeignKey(e => e.AppliedBudgetId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsAccepted);
        });

        builder.Entity<BudgetSuggestionItem>(entity =>
        {
            entity.HasKey(e => e.BudgetSuggestionItemId);
            entity.Property(e => e.SuggestedAmount).HasPrecision(18, 2);
            entity.Property(e => e.AdjustedAmount).HasPrecision(18, 2);
            entity.Property(e => e.Percentage).HasPrecision(5, 2);
            entity.Property(e => e.RecurrencePeriodMonths).IsRequired().HasDefaultValue(1);
            
            entity.HasOne(e => e.BudgetSuggestion)
                .WithMany(s => s.Items)
                .HasForeignKey(e => e.BudgetSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BudgetAdjustment>(entity =>
        {
            entity.HasKey(e => e.BudgetAdjustmentId);
            entity.Property(e => e.PreviousAmount).HasPrecision(18, 2);
            entity.Property(e => e.NewAmount).HasPrecision(18, 2);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.AdjustedAt).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            
            entity.HasOne(e => e.BudgetSuggestion)
                .WithMany(s => s.Adjustments)
                .HasForeignKey(e => e.BudgetSuggestionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.TransferToCategory)
                .WithMany()
                .HasForeignKey(e => e.TransferToCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Goal configuration
        builder.Entity<Goal>(entity =>
        {
            entity.HasKey(e => e.GoalId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TargetAmount).HasPrecision(18, 2);
            entity.Property(e => e.CurrentAmount).HasPrecision(18, 2);
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            entity.HasOne(e => e.FundedFromBankSource)
                .WithMany()
                .HasForeignKey(e => e.FundedFromBankSourceId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo });
        });
        
        // GoalMilestone configuration
        builder.Entity<GoalMilestone>(entity =>
        {
            entity.HasKey(e => e.GoalMilestoneId);
            entity.Property(e => e.TargetAmount).HasPrecision(18, 2);
            entity.Property(e => e.Percentage).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsReached).IsRequired();
            entity.Property(e => e.IsAutomatic).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Goal)
                .WithMany()
                .HasForeignKey(e => e.GoalId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.GoalId);
            entity.HasIndex(e => e.IsReached);
            entity.HasIndex(e => new { e.GoalId, e.Percentage });
        });
        
        // SalaryHistory configuration
        builder.Entity<SalaryHistory>(entity =>
        {
            entity.HasKey(e => e.SalaryHistoryId);
            entity.Property(e => e.MonthlySalary).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Period).IsRequired();
            entity.Property(e => e.JobTitle).HasMaxLength(200);
            entity.Property(e => e.Employer).HasMaxLength(200);
            entity.Property(e => e.EmploymentType).HasMaxLength(50);
            entity.Property(e => e.WorkPercentage).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.IsCurrent).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Period);
            entity.HasIndex(e => new { e.UserId, e.Period });
            entity.HasIndex(e => e.IsCurrent);
        });

        // Pocket configuration
        builder.Entity<Pocket>(entity =>
        {
            entity.HasKey(e => e.PocketId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TargetAmount).HasPrecision(18, 2);
            entity.Property(e => e.CurrentAmount).HasPrecision(18, 2);
            entity.Property(e => e.MonthlyAllocation).HasPrecision(18, 2);
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            entity.HasOne(e => e.BankSource)
                .WithMany()
                .HasForeignKey(e => e.BankSourceId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BankSourceId);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo });
            
            // Ignore computed properties
            entity.Ignore(e => e.ProgressPercentage);
            entity.Ignore(e => e.RemainingAmount);
        });

        // PocketTransaction configuration
        builder.Entity<PocketTransaction>(entity =>
        {
            entity.HasKey(e => e.PocketTransactionId);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TransactionDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Pocket)
                .WithMany(p => p.PocketTransactions)
                .HasForeignKey(e => e.PocketId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.RelatedPocket)
                .WithMany()
                .HasForeignKey(e => e.RelatedPocketId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.PocketId);
            entity.HasIndex(e => e.TransactionDate);
        });

        // Seed initial categories with BAS 2026-inspired account numbers
        // Based on BAS 2026, adapted for personal finance (privatekonomi)
        // Account number ranges:
        // - 3000-3999: Income (Intäkter) - corresponds to BAS class 3
        // - 4000-4999: Housing & Living (Boende) - corresponds to BAS class 4-5 (operational costs)
        // - 5000-5999: Food & Consumption (Mat & Förbrukning) - corresponds to BAS class 6 (other external costs)
        // - 6000-6999: Transportation & Other Costs (Transport & Övriga kostnader) - corresponds to BAS class 7 (personnel costs adapted)
        // - 7000-7999: Entertainment & Health (Nöje & Hälsa) - corresponds to BAS class 8 (other costs)
        // - 8000-8999: Savings & Investments (Sparande) - corresponds to BAS class 1 (assets)
        // 
        // Note: BAS 2026 contains ~255 changes from BAS 2025, mostly business-specific (reverse charge VAT,
        // periodization funds, shareholder loans). The personal finance categories (3000-8999) are not affected
        // by these business-focused changes.
        builder.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Mat & Dryck", AccountNumber = "5000", Color = "#FF6B6B", TaxRelated = false, IsSystemCategory = true, OriginalName = "Mat & Dryck", OriginalColor = "#FF6B6B", OriginalAccountNumber = "5000", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 2, Name = "Transport", AccountNumber = "6000", Color = "#4ECDC4", TaxRelated = false, IsSystemCategory = true, OriginalName = "Transport", OriginalColor = "#4ECDC4", OriginalAccountNumber = "6000", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 3, Name = "Boende", AccountNumber = "4000", Color = "#45B7D1", TaxRelated = false, IsSystemCategory = true, OriginalName = "Boende", OriginalColor = "#45B7D1", OriginalAccountNumber = "4000", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 4, Name = "Nöje", AccountNumber = "7000", Color = "#FFA07A", TaxRelated = false, IsSystemCategory = true, OriginalName = "Nöje", OriginalColor = "#FFA07A", OriginalAccountNumber = "7000", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 5, Name = "Shopping", AccountNumber = "5500", Color = "#98D8C8", TaxRelated = false, IsSystemCategory = true, OriginalName = "Shopping", OriginalColor = "#98D8C8", OriginalAccountNumber = "5500", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 6, Name = "Hälsa", AccountNumber = "7500", Color = "#6BCF7F", TaxRelated = false, IsSystemCategory = true, OriginalName = "Hälsa", OriginalColor = "#6BCF7F", OriginalAccountNumber = "7500", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 7, Name = "Lön", AccountNumber = "3000", Color = "#4CAF50", TaxRelated = false, IsSystemCategory = true, OriginalName = "Lön", OriginalColor = "#4CAF50", OriginalAccountNumber = "3000", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 8, Name = "Sparande", AccountNumber = "8000", Color = "#2196F3", TaxRelated = false, IsSystemCategory = true, OriginalName = "Sparande", OriginalColor = "#2196F3", OriginalAccountNumber = "8000", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 9, Name = "Övrigt", AccountNumber = "6900", Color = "#9E9E9E", TaxRelated = false, IsSystemCategory = true, OriginalName = "Övrigt", OriginalColor = "#9E9E9E", OriginalAccountNumber = "6900", CreatedAt = DateTime.UtcNow },
            
            // Subcategories for Mat & Dryck (5000)
            new Category { CategoryId = 10, Name = "Livsmedel", AccountNumber = "5100", Color = "#FF6B6B", ParentId = 1, TaxRelated = false, IsSystemCategory = true, OriginalName = "Livsmedel", OriginalColor = "#FF6B6B", OriginalAccountNumber = "5100", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 11, Name = "Restaurang", AccountNumber = "5200", Color = "#FF5252", ParentId = 1, TaxRelated = false, IsSystemCategory = true, OriginalName = "Restaurang", OriginalColor = "#FF5252", OriginalAccountNumber = "5200", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 12, Name = "Café", AccountNumber = "5300", Color = "#FF8A80", ParentId = 1, TaxRelated = false, IsSystemCategory = true, OriginalName = "Café", OriginalColor = "#FF8A80", OriginalAccountNumber = "5300", CreatedAt = DateTime.UtcNow },
            
            // Subcategories for Transport (6000)
            new Category { CategoryId = 13, Name = "Kollektivtrafik", AccountNumber = "6100", Color = "#4ECDC4", ParentId = 2, TaxRelated = false, IsSystemCategory = true, OriginalName = "Kollektivtrafik", OriginalColor = "#4ECDC4", OriginalAccountNumber = "6100", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 14, Name = "Bensin", AccountNumber = "6200", Color = "#26A69A", ParentId = 2, TaxRelated = false, IsSystemCategory = true, OriginalName = "Bensin", OriginalColor = "#26A69A", OriginalAccountNumber = "6200", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 15, Name = "Parkering", AccountNumber = "6500", Color = "#80CBC4", ParentId = 2, TaxRelated = false, IsSystemCategory = true, OriginalName = "Parkering", OriginalColor = "#80CBC4", OriginalAccountNumber = "6500", CreatedAt = DateTime.UtcNow },
            
            // Subcategories for Boende (4000)
            new Category { CategoryId = 16, Name = "Hyra/Avgift", AccountNumber = "4100", Color = "#45B7D1", ParentId = 3, TaxRelated = false, IsSystemCategory = true, OriginalName = "Hyra/Avgift", OriginalColor = "#45B7D1", OriginalAccountNumber = "4100", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 17, Name = "El", AccountNumber = "4200", Color = "#29B6F6", ParentId = 3, TaxRelated = false, IsSystemCategory = true, OriginalName = "El", OriginalColor = "#29B6F6", OriginalAccountNumber = "4200", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 18, Name = "Bredband", AccountNumber = "4300", Color = "#81D4FA", ParentId = 3, TaxRelated = false, IsSystemCategory = true, OriginalName = "Bredband", OriginalColor = "#81D4FA", OriginalAccountNumber = "4300", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 19, Name = "Hemförsäkring", AccountNumber = "4400", Color = "#4FC3F7", ParentId = 3, TaxRelated = false, IsSystemCategory = true, OriginalName = "Hemförsäkring", OriginalColor = "#4FC3F7", OriginalAccountNumber = "4400", CreatedAt = DateTime.UtcNow },
            
            // Subcategories for Nöje (7000)
            new Category { CategoryId = 20, Name = "Streaming", AccountNumber = "7100", Color = "#FFA07A", ParentId = 4, TaxRelated = false, IsSystemCategory = true, OriginalName = "Streaming", OriginalColor = "#FFA07A", OriginalAccountNumber = "7100", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 21, Name = "Gym", AccountNumber = "7300", Color = "#FF8A65", ParentId = 4, TaxRelated = false, IsSystemCategory = true, OriginalName = "Gym", OriginalColor = "#FF8A65", OriginalAccountNumber = "7300", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 22, Name = "Resor", AccountNumber = "7400", Color = "#FFAB91", ParentId = 4, TaxRelated = false, IsSystemCategory = true, OriginalName = "Resor", OriginalColor = "#FFAB91", OriginalAccountNumber = "7400", CreatedAt = DateTime.UtcNow },
            
            // Subcategories for Shopping (5500)
            new Category { CategoryId = 23, Name = "Kläder", AccountNumber = "5510", Color = "#98D8C8", ParentId = 5, TaxRelated = false, IsSystemCategory = true, OriginalName = "Kläder", OriginalColor = "#98D8C8", OriginalAccountNumber = "5510", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 24, Name = "Hygienartiklar", AccountNumber = "5520", Color = "#80CBC4", ParentId = 5, TaxRelated = false, IsSystemCategory = true, OriginalName = "Hygienartiklar", OriginalColor = "#80CBC4", OriginalAccountNumber = "5520", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 25, Name = "Elektronik", AccountNumber = "5550", Color = "#B2DFDB", ParentId = 5, TaxRelated = false, IsSystemCategory = true, OriginalName = "Elektronik", OriginalColor = "#B2DFDB", OriginalAccountNumber = "5550", CreatedAt = DateTime.UtcNow },
            
            // Subcategories for Hälsa (7500) - Reusing 7500 range for health subcategories
            new Category { CategoryId = 26, Name = "Tandvård", AccountNumber = "7510", Color = "#6BCF7F", ParentId = 6, TaxRelated = false, IsSystemCategory = true, OriginalName = "Tandvård", OriginalColor = "#6BCF7F", OriginalAccountNumber = "7510", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 27, Name = "Läkarvård", AccountNumber = "7520", Color = "#81C784", ParentId = 6, TaxRelated = false, IsSystemCategory = true, OriginalName = "Läkarvård", OriginalColor = "#81C784", OriginalAccountNumber = "7520", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 28, Name = "Medicin", AccountNumber = "7530", Color = "#A5D6A7", ParentId = 6, TaxRelated = false, IsSystemCategory = true, OriginalName = "Medicin", OriginalColor = "#A5D6A7", OriginalAccountNumber = "7530", CreatedAt = DateTime.UtcNow },
            
            // Subcategories for Lön (3000)
            new Category { CategoryId = 29, Name = "Bonus", AccountNumber = "3010", Color = "#66BB6A", ParentId = 7, TaxRelated = false, IsSystemCategory = true, OriginalName = "Bonus", OriginalColor = "#66BB6A", OriginalAccountNumber = "3010", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 30, Name = "Semesterersättning", AccountNumber = "3020", Color = "#81C784", ParentId = 7, TaxRelated = false, IsSystemCategory = true, OriginalName = "Semesterersättning", OriginalColor = "#81C784", OriginalAccountNumber = "3020", CreatedAt = DateTime.UtcNow },
            
            // Subcategories for Sparande (8000)
            new Category { CategoryId = 31, Name = "Buffert", AccountNumber = "8100", Color = "#2196F3", ParentId = 8, TaxRelated = false, IsSystemCategory = true, OriginalName = "Buffert", OriginalColor = "#2196F3", OriginalAccountNumber = "8100", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 32, Name = "Månadsspar Fonder", AccountNumber = "8200", Color = "#42A5F5", ParentId = 8, TaxRelated = false, IsSystemCategory = true, OriginalName = "Månadsspar Fonder", OriginalColor = "#42A5F5", OriginalAccountNumber = "8200", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 33, Name = "ISK", AccountNumber = "8300", Color = "#64B5F6", ParentId = 8, TaxRelated = false, IsSystemCategory = true, OriginalName = "ISK", OriginalColor = "#64B5F6", OriginalAccountNumber = "8300", CreatedAt = DateTime.UtcNow },
            new Category { CategoryId = 34, Name = "Pensionssparande", AccountNumber = "8400", Color = "#90CAF9", ParentId = 8, TaxRelated = false, IsSystemCategory = true, OriginalName = "Pensionssparande", OriginalColor = "#90CAF9", OriginalAccountNumber = "8400", CreatedAt = DateTime.UtcNow }
        );

        // Seed initial bank sources
        builder.Entity<BankSource>().HasData(
            new BankSource { BankSourceId = 1, Name = "ICA-banken", Color = "#DC143C", AccountType = "checking", Currency = "SEK", InitialBalance = 0, CreatedAt = DateTime.UtcNow, ValidFrom = DateTime.UtcNow, ValidTo = null }, // röd (Crimson)
            new BankSource { BankSourceId = 2, Name = "Swedbank", Color = "#FF8C00", AccountType = "checking", Currency = "SEK", InitialBalance = 0, CreatedAt = DateTime.UtcNow, ValidFrom = DateTime.UtcNow, ValidTo = null }, // mörk orange (Dark Orange)
            new BankSource { BankSourceId = 3, Name = "SEB", Color = "#0066CC", AccountType = "checking", Currency = "SEK", InitialBalance = 0, CreatedAt = DateTime.UtcNow, ValidFrom = DateTime.UtcNow, ValidTo = null }, // blå
            new BankSource { BankSourceId = 4, Name = "Nordea", Color = "#00A9CE", AccountType = "checking", Currency = "SEK", InitialBalance = 0, CreatedAt = DateTime.UtcNow, ValidFrom = DateTime.UtcNow, ValidTo = null }, // ljusblå
            new BankSource { BankSourceId = 5, Name = "Handelsbanken", Color = "#003366", AccountType = "checking", Currency = "SEK", InitialBalance = 0, CreatedAt = DateTime.UtcNow, ValidFrom = DateTime.UtcNow, ValidTo = null }, // mörk blå
            new BankSource { BankSourceId = 6, Name = "Avanza", Color = "#006400", AccountType = "investment", Currency = "SEK", InitialBalance = 0, CreatedAt = DateTime.UtcNow, ValidFrom = DateTime.UtcNow, ValidTo = null } // mörkgrön (Dark Green)
        );

        // Swedish-specific entities configuration
        builder.Entity<TaxDeduction>(entity =>
        {
            entity.HasKey(e => e.TaxDeductionId);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.DeductibleAmount).HasPrecision(18, 2);
            entity.Property(e => e.ServiceProvider).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OrganizationNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.WorkDescription).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Transaction)
                .WithMany()
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.TaxYear);
            entity.HasIndex(e => e.Type);
        });
        
        builder.Entity<CapitalGain>(entity =>
        {
            entity.HasKey(e => e.CapitalGainId);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.PurchasePricePerUnit).HasPrecision(18, 2);
            entity.Property(e => e.TotalPurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.SalePricePerUnit).HasPrecision(18, 2);
            entity.Property(e => e.TotalSalePrice).HasPrecision(18, 2);
            entity.Property(e => e.Gain).HasPrecision(18, 2);
            entity.Property(e => e.SecurityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SecurityName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ISIN).HasMaxLength(12);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Investment)
                .WithMany()
                .HasForeignKey(e => e.InvestmentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => e.TaxYear);
            entity.HasIndex(e => e.IsISK);
            entity.HasIndex(e => e.SaleDate);
        });
        
        builder.Entity<CommuteDeduction>(entity =>
        {
            entity.HasKey(e => e.CommuteDeductionId);
            entity.Property(e => e.FromAddress).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ToAddress).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DistanceKm).HasPrecision(10, 2);
            entity.Property(e => e.TransportMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.DeductibleAmount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.TaxYear);
            entity.HasIndex(e => e.Date);
            
            entity.Ignore(e => e.TotalDistanceKm);
        });
        
        builder.Entity<CreditRating>(entity =>
        {
            entity.HasKey(e => e.CreditRatingId);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Rating).IsRequired().HasMaxLength(10);
            entity.Property(e => e.TotalDebt).HasPrecision(18, 2);
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.CreditUtilization).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.CheckedDate);
        });
        
        // Swedish-specific Investment properties
        builder.Entity<Investment>(entity =>
        {
            entity.Property(e => e.AccountType).HasMaxLength(20);
            entity.Property(e => e.SchablonTax).HasPrecision(18, 2);
            
            entity.HasIndex(e => e.AccountType);
            entity.HasIndex(e => e.SchablonTaxYear);
        });
        
        // Swedish-specific Transaction properties
        builder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.RecipientBankgiro).HasMaxLength(20);
            entity.Property(e => e.RecipientPlusgiro).HasMaxLength(20);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(50);
            entity.Property(e => e.OCR).HasMaxLength(50);
            
            entity.HasIndex(e => e.PaymentMethod);
            entity.HasIndex(e => e.IsRecurring);
        });
        
        // Swedish-specific Loan properties
        builder.Entity<Loan>(entity =>
        {
            entity.Property(e => e.PropertyAddress).HasMaxLength(200);
            entity.Property(e => e.PropertyValue).HasPrecision(18, 2);
            entity.Property(e => e.LoanProvider).HasMaxLength(100);
            entity.Property(e => e.CsnLoanType).HasMaxLength(50);
            entity.Property(e => e.CsnMonthlyPayment).HasPrecision(18, 2);
            entity.Property(e => e.CsnRemainingAmount).HasPrecision(18, 2);
            
            entity.HasIndex(e => e.Type);
            entity.Ignore(e => e.LTV);
        });

        // Household configuration
        builder.Entity<Household>(entity =>
        {
            entity.HasKey(e => e.HouseholdId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).IsRequired();
        });

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.HouseholdMember)
                .WithOne(hm => hm.User)
                .HasForeignKey<ApplicationUser>(e => e.HouseholdMemberId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<HouseholdMember>(entity =>
        {
            entity.HasKey(e => e.HouseholdMemberId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.JoinedDate).IsRequired();
            
            entity.HasOne(e => e.Household)
                .WithMany(h => h.Members)
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SharedExpense>(entity =>
        {
            entity.HasKey(e => e.SharedExpenseId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.ExpenseDate).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.SplitMethod).IsRequired();
            
            entity.HasOne(e => e.Household)
                .WithMany(h => h.SharedExpenses)
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ExpenseShare>(entity =>
        {
            entity.HasKey(e => e.ExpenseShareId);
            entity.Property(e => e.ShareAmount).HasPrecision(18, 2);
            entity.Property(e => e.SharePercentage).HasPrecision(5, 2);
            entity.Property(e => e.RoomSize).HasPrecision(10, 2);
            
            entity.HasOne(e => e.SharedExpense)
                .WithMany(se => se.ExpenseShares)
                .HasForeignKey(e => e.SharedExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.HouseholdMember)
                .WithMany(hm => hm.ExpenseShares)
                .HasForeignKey(e => e.HouseholdMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Child Allowance configuration
        builder.Entity<ChildAllowance>(entity =>
        {
            entity.HasKey(e => e.ChildAllowanceId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);
            entity.Property(e => e.Frequency).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.HouseholdMember)
                .WithMany()
                .HasForeignKey(e => e.HouseholdMemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AllowanceTransaction>(entity =>
        {
            entity.HasKey(e => e.AllowanceTransactionId);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.TransactionDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.ChildAllowance)
                .WithMany(ca => ca.AllowanceTransactions)
                .HasForeignKey(e => e.ChildAllowanceId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.AllowanceTask)
                .WithMany(at => at.AllowanceTransactions)
                .HasForeignKey(e => e.AllowanceTaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AllowanceTask>(entity =>
        {
            entity.HasKey(e => e.AllowanceTaskId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RewardAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.DueDate).IsRequired();
            entity.Property(e => e.ApprovedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.ChildAllowance)
                .WithMany(ca => ca.AllowanceTasks)
                .HasForeignKey(e => e.ChildAllowanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Shared Goals configuration
        builder.Entity<SharedGoal>(entity =>
        {
            entity.HasKey(e => e.SharedGoalId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TargetAmount).HasPrecision(18, 2);
            entity.Property(e => e.CurrentAmount).HasPrecision(18, 2);
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.Status);
        });

        builder.Entity<SharedGoalParticipant>(entity =>
        {
            entity.HasKey(e => e.SharedGoalParticipantId);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.InvitationStatus).IsRequired();
            entity.Property(e => e.JoinedAt).IsRequired();
            
            entity.HasOne(e => e.SharedGoal)
                .WithMany(sg => sg.Participants)
                .HasForeignKey(e => e.SharedGoalId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.InvitedByUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => e.SharedGoalId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.InvitationStatus);
            entity.HasIndex(e => new { e.SharedGoalId, e.UserId }).IsUnique();
        });

        builder.Entity<SharedGoalProposal>(entity =>
        {
            entity.HasKey(e => e.SharedGoalProposalId);
            entity.Property(e => e.ProposalType).IsRequired();
            entity.Property(e => e.CurrentValue).HasMaxLength(500);
            entity.Property(e => e.ProposedValue).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.SharedGoal)
                .WithMany(sg => sg.Proposals)
                .HasForeignKey(e => e.SharedGoalId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.ProposedByUser)
                .WithMany()
                .HasForeignKey(e => e.ProposedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => e.SharedGoalId);
            entity.HasIndex(e => e.Status);
        });

        builder.Entity<SharedGoalProposalVote>(entity =>
        {
            entity.HasKey(e => e.SharedGoalProposalVoteId);
            entity.Property(e => e.Vote).IsRequired();
            entity.Property(e => e.VotedAt).IsRequired();
            entity.Property(e => e.Comment).HasMaxLength(500);
            
            entity.HasOne(e => e.SharedGoalProposal)
                .WithMany(p => p.Votes)
                .HasForeignKey(e => e.SharedGoalProposalId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => e.SharedGoalProposalId);
            entity.HasIndex(e => new { e.SharedGoalProposalId, e.UserId }).IsUnique();
        });

        builder.Entity<SharedGoalTransaction>(entity =>
        {
            entity.HasKey(e => e.SharedGoalTransactionId);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TransactionDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.SharedGoal)
                .WithMany(sg => sg.Transactions)
                .HasForeignKey(e => e.SharedGoalId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => e.SharedGoalId);
            entity.HasIndex(e => e.TransactionDate);
        });

        builder.Entity<SharedGoalNotification>(entity =>
        {
            entity.HasKey(e => e.SharedGoalNotificationId);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IsRead).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.SharedGoal)
                .WithMany(sg => sg.Notifications)
                .HasForeignKey(e => e.SharedGoalId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.SharedGoalId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });
        
        // Net Worth Snapshot configuration
        builder.Entity<NetWorthSnapshot>(entity =>
        {
            entity.HasKey(e => e.NetWorthSnapshotId);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.TotalAssets).HasPrecision(18, 2);
            entity.Property(e => e.TotalLiabilities).HasPrecision(18, 2);
            entity.Property(e => e.NetWorth).HasPrecision(18, 2);
            entity.Property(e => e.BankBalance).HasPrecision(18, 2);
            entity.Property(e => e.InvestmentValue).HasPrecision(18, 2);
            entity.Property(e => e.PhysicalAssetValue).HasPrecision(18, 2);
            entity.Property(e => e.LoanBalance).HasPrecision(18, 2);
            entity.Property(e => e.IsManual).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => new { e.UserId, e.Date });
        });
        
        // Currency Account configuration
        builder.Entity<CurrencyAccount>(entity =>
        {
            entity.HasKey(e => e.CurrencyAccountId);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            entity.Property(e => e.AccountNumber).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Currency);
            
            // Ignore computed property
            entity.Ignore(e => e.ValueInSEK);
        });
        
        // Receipt configuration
        builder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId);
            entity.Property(e => e.Merchant).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ReceiptDate).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.ReceiptType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.ReceiptNumber).HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.Transaction)
                .WithMany(t => t.Receipts)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ReceiptDate);
            entity.HasIndex(e => e.Merchant);
            entity.HasIndex(e => e.TransactionId);
        });
        
        // ReceiptLineItem configuration
        builder.Entity<ReceiptLineItem>(entity =>
        {
            entity.HasKey(e => e.ReceiptLineItemId);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            entity.Property(e => e.TaxRate).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Receipt)
                .WithMany(r => r.ReceiptLineItems)
                .HasForeignKey(e => e.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.ReceiptId);
        });
        
        // Subscription configuration
        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.BillingFrequency).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CancellationUrl).HasMaxLength(500);
            entity.Property(e => e.ManagementUrl).HasMaxLength(500);
            entity.Property(e => e.AccountEmail).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.NextBillingDate);
        });
        
        // SubscriptionPriceHistory configuration
        builder.Entity<SubscriptionPriceHistory>(entity =>
        {
            entity.HasKey(e => e.SubscriptionPriceHistoryId);
            entity.Property(e => e.OldPrice).HasPrecision(18, 2);
            entity.Property(e => e.NewPrice).HasPrecision(18, 2);
            entity.Property(e => e.ChangeDate).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.NotificationSent).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.PriceHistory)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.ChangeDate);
        });
        
        // Bill configuration
        builder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.BillId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.IssueDate).IsRequired();
            entity.Property(e => e.DueDate).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsRecurring).IsRequired();
            entity.Property(e => e.RecurringFrequency).HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.OCR).HasMaxLength(50);
            entity.Property(e => e.Bankgiro).HasMaxLength(20);
            entity.Property(e => e.Plusgiro).HasMaxLength(20);
            entity.Property(e => e.Payee).HasMaxLength(200);
            entity.Property(e => e.DocumentPath).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(500);
            
        // Pension configuration
        builder.Entity<Pension>(entity =>
        {
            entity.HasKey(e => e.PensionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PensionType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
            entity.Property(e => e.TotalContributions).HasPrecision(18, 2);
            entity.Property(e => e.MonthlyContribution).HasPrecision(18, 2);
            entity.Property(e => e.ExpectedMonthlyPension).HasPrecision(18, 2);
            entity.Property(e => e.AccountNumber).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.LastUpdated).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PensionType);
            
            // Ignore computed properties
            entity.Ignore(e => e.TotalReturn);
            entity.Ignore(e => e.ReturnPercentage);
        });
        
        // Dividend configuration
        builder.Entity<Dividend>(entity =>
        {
            entity.HasKey(e => e.DividendId);
            entity.Property(e => e.AmountPerShare).HasPrecision(18, 4);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.SharesHeld).HasPrecision(18, 4);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.TaxWithheld).HasPrecision(18, 2);
            entity.Property(e => e.ReinvestedShares).HasPrecision(18, 4);
            entity.Property(e => e.ReinvestmentPrice).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PaymentDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Investment)
                .WithMany()
                .HasForeignKey(e => e.InvestmentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.InvestmentId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PaymentDate);
        });
        
        // InvestmentTransaction configuration
        builder.Entity<InvestmentTransaction>(entity =>
        {
            entity.HasKey(e => e.InvestmentTransactionId);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.PricePerShare).HasPrecision(18, 4);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Fees).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.TransactionDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Investment)
                .WithMany()
                .HasForeignKey(e => e.InvestmentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.InvestmentId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TransactionDate);
            
            // Ignore computed property
            entity.Ignore(e => e.TotalCost);
        });
        
        entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Transaction)
                .WithMany()
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.IsRecurring);
        });
        
        // BillReminder configuration
        builder.Entity<BillReminder>(entity =>
        {
            entity.HasKey(e => e.BillReminderId);
            entity.Property(e => e.ReminderDate).IsRequired();
            entity.Property(e => e.IsSent).IsRequired();
            entity.Property(e => e.ReminderMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Bill)
                .WithMany(b => b.Reminders)
                .HasForeignKey(e => e.BillId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.BillId);
            entity.HasIndex(e => e.ReminderDate);
            entity.HasIndex(e => e.IsSent);
        });
        
        // BillSchedule configuration
        builder.Entity<BillSchedule>(entity =>
        {
            entity.HasKey(e => e.BillScheduleId);
            entity.Property(e => e.Frequency).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.NextOccurrenceDate).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Bill)
                .WithMany()
                .HasForeignKey(e => e.BillId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.BillId);
            entity.HasIndex(e => e.NextOccurrenceDate);
            entity.HasIndex(e => e.IsActive);
        });
        
        // PortfolioAllocation configuration
        builder.Entity<PortfolioAllocation>(entity =>
        {
            entity.HasKey(e => e.PortfolioAllocationId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AssetClass).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TargetPercentage).HasPrecision(5, 2);
            entity.Property(e => e.MinPercentage).HasPrecision(5, 2);
            entity.Property(e => e.MaxPercentage).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
        });

        // ML Model configuration
        builder.Entity<MLModel>(entity =>
        {
            entity.HasKey(e => e.ModelId);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ModelPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TrainedAt).IsRequired();
            entity.Property(e => e.TrainingRecordsCount).IsRequired();
            entity.Property(e => e.Accuracy).IsRequired();
            entity.Property(e => e.Precision).IsRequired();
            entity.Property(e => e.Recall).IsRequired();
            entity.Property(e => e.Metrics).HasMaxLength(2000);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
        });
        
        // LifeTimelineMilestone configuration
        builder.Entity<LifeTimelineMilestone>(entity =>
        {
            entity.HasKey(e => e.LifeTimelineMilestoneId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MilestoneType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PlannedDate).IsRequired();
            entity.Property(e => e.EstimatedCost).HasPrecision(18, 2);
            entity.Property(e => e.RequiredMonthlySavings).HasPrecision(18, 2);
            entity.Property(e => e.ProgressPercentage).HasPrecision(5, 2);
            entity.Property(e => e.CurrentSavings).HasPrecision(18, 2);
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.IsCompleted).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            
            entity.HasIndex(e => e.PlannedDate);
            entity.HasIndex(e => e.MilestoneType);
            entity.HasIndex(e => new { e.UserId, e.PlannedDate });
        });

        // User Feedback configuration
        builder.Entity<UserFeedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.TransactionId).IsRequired();
            entity.Property(e => e.PredictedCategory).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PredictedConfidence).IsRequired();
            entity.Property(e => e.ActualCategory).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WasCorrectionNeeded).IsRequired();
            entity.Property(e => e.FeedbackDate).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Transaction)
                .WithMany()
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.FeedbackDate);
        });

        // LifeTimelineScenario configuration
        builder.Entity<LifeTimelineScenario>(entity =>
        {
            entity.HasKey(e => e.LifeTimelineScenarioId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExpectedReturnRate).HasPrecision(5, 2);
            entity.Property(e => e.MonthlySavings).HasPrecision(18, 2);
            entity.Property(e => e.RetirementAge).IsRequired();
            entity.Property(e => e.ExpectedMonthlyPension).HasPrecision(18, 2);
            entity.Property(e => e.ProjectedRetirementWealth).HasPrecision(18, 2);
            entity.Property(e => e.InflationRate).HasPrecision(5, 2);
            entity.Property(e => e.SalaryIncreaseRate).HasPrecision(5, 2);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.IsBaseline).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
        });
        
        // RBAC - HouseholdRole configuration
        builder.Entity<HouseholdRole>(entity =>
        {
            entity.HasKey(e => e.HouseholdRoleId);
            entity.Property(e => e.RoleType).IsRequired();
            entity.Property(e => e.IsDelegated).IsRequired();
            entity.Property(e => e.DelegatedBy).HasMaxLength(450);
            entity.Property(e => e.AssignedBy).HasMaxLength(450);
            entity.Property(e => e.RevokedBy).HasMaxLength(450);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.AssignedDate).IsRequired();
            
            entity.HasOne(e => e.HouseholdMember)
                .WithMany(m => m.Roles)
                .HasForeignKey(e => e.HouseholdMemberId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.HouseholdMemberId);
            entity.HasIndex(e => e.RoleType);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.HouseholdMemberId, e.IsActive });
            entity.HasIndex(e => e.DelegationEndDate);
        });
        
        // RBAC - RolePrivilege configuration
        builder.Entity<RolePrivilege>(entity =>
        {
            entity.HasKey(e => e.RolePermissionId);
            entity.Property(e => e.RoleType).IsRequired();
            entity.Property(e => e.PermissionKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsAllowed).IsRequired();
            entity.Property(e => e.AmountLimit).HasPrecision(18, 2);
            
            entity.HasIndex(e => e.RoleType);
            entity.HasIndex(e => e.PermissionKey);
            entity.HasIndex(e => new { e.RoleType, e.PermissionKey });
        });
        
        // RBAC - AuditLogEntry configuration
        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.AuditLogEntryId);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.UserEmail).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Severity).IsRequired();
            entity.Property(e => e.ResourceType).HasMaxLength(100);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.OldValue).HasMaxLength(1000);
            entity.Property(e => e.NewValue).HasMaxLength(1000);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.HouseholdId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => new { e.HouseholdId, e.Timestamp });
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
        });

        // DashboardLayout configuration
        builder.Entity<DashboardLayout>(entity =>
        {
            entity.HasKey(e => e.LayoutId);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsDefault).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Widgets)
                .WithOne(w => w.Layout)
                .HasForeignKey(w => w.LayoutId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsDefault });
        });

        // WidgetConfiguration configuration
        builder.Entity<WidgetConfiguration>(entity =>
        {
            entity.HasKey(e => e.WidgetConfigId);
            entity.Property(e => e.LayoutId).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Row).IsRequired();
            entity.Property(e => e.Column).IsRequired();
            entity.Property(e => e.Width).IsRequired();
            entity.Property(e => e.Height).IsRequired();
            entity.Property(e => e.Settings).HasMaxLength(2000);
            
            entity.HasIndex(e => e.LayoutId);
        });
        
        // MonthlyReport configuration
        builder.Entity<MonthlyReport>(entity =>
        {
            entity.HasKey(e => e.MonthlyReportId);
            entity.Property(e => e.ReportMonth).IsRequired();
            entity.Property(e => e.TotalIncome).HasPrecision(18, 2);
            entity.Property(e => e.TotalExpenses).HasPrecision(18, 2);
            entity.Property(e => e.NetFlow).HasPrecision(18, 2);
            entity.Property(e => e.IncomeChangePercent).HasPrecision(10, 2);
            entity.Property(e => e.ExpenseChangePercent).HasPrecision(10, 2);
            entity.Property(e => e.CategorySummariesJson).HasMaxLength(10000);
            entity.Property(e => e.TopMerchantsJson).HasMaxLength(5000);
            entity.Property(e => e.BudgetOutcomeJson).HasMaxLength(5000);
            entity.Property(e => e.InsightsJson).HasMaxLength(5000);
            entity.Property(e => e.GeneratedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Temporal tracking
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ReportMonth);
            entity.HasIndex(e => new { e.UserId, e.ReportMonth });
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidTo);
        });
        
        // ReportDelivery configuration
        builder.Entity<ReportDelivery>(entity =>
        {
            entity.HasKey(e => e.ReportDeliveryId);
            entity.Property(e => e.DeliveryMethod).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.EmailAddress).HasMaxLength(256);
            entity.Property(e => e.ScheduledAt).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.RetryCount).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.MonthlyReport)
                .WithMany(r => r.Deliveries)
                .HasForeignKey(e => e.MonthlyReportId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.MonthlyReportId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledAt);
        });
        
        // ReportPreference configuration
        builder.Entity<ReportPreference>(entity =>
        {
            entity.HasKey(e => e.ReportPreferenceId);
            entity.Property(e => e.SendEmail).IsRequired();
            entity.Property(e => e.ShowInApp).IsRequired();
            entity.Property(e => e.EmailAddress).HasMaxLength(256);
            entity.Property(e => e.PreferredDeliveryDay).IsRequired();
            entity.Property(e => e.IncludeCategoryDetails).IsRequired();
            entity.Property(e => e.IncludeTopMerchants).IsRequired();
            entity.Property(e => e.IncludeBudgetComparison).IsRequired();
            entity.Property(e => e.IncludeTrendAnalysis).IsRequired();
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId).IsUnique();
        });
        
        // ImportJob configuration
        builder.Entity<ImportJob>(entity =>
        {
            entity.HasKey(e => e.ImportJobId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BankName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(10);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.TotalRows).IsRequired();
            entity.Property(e => e.ImportedCount).IsRequired();
            entity.Property(e => e.DuplicateCount).IsRequired();
            entity.Property(e => e.ErrorCount).IsRequired();
            entity.Property(e => e.ErrorMessages).HasMaxLength(5000);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.Status });
        });
    }
}
