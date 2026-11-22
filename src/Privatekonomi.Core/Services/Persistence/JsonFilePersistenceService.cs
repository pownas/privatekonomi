using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services.DataMigration;

namespace Privatekonomi.Core.Services.Persistence;

/// <summary>
/// JSON file-based persistence service for InMemory database
/// </summary>
public class JsonFilePersistenceService : IDataPersistenceService
{
    private readonly string _dataDirectory;
    private readonly ILogger<JsonFilePersistenceService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonFilePersistenceService(
        IOptions<StorageSettings> storageSettings,
        ILogger<JsonFilePersistenceService> logger)
    {
        _logger = logger;
        
        // Get data directory from ConnectionString or use default
        _dataDirectory = string.IsNullOrEmpty(storageSettings.Value.ConnectionString)
            ? Path.Combine(Directory.GetCurrentDirectory(), "data")
            : storageSettings.Value.ConnectionString;
        
        // Ensure directory exists
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
        
        // Configure JSON serialization
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public bool Exists()
    {
        var files = new[]
        {
            "categories.json",
            "transactions.json",
            "budgets.json"
        };

        if (files.Any(file => File.Exists(Path.Combine(_dataDirectory, file))))
        {
            return true;
        }

        // Also check for monthly transaction files
        return Directory.Exists(_dataDirectory) &&
               Directory.GetFiles(_dataDirectory, "transactions-*.json").Length > 0;
    }

    public async Task SaveAsync(PrivatekonomyContext context)
    {
        try
        {
            _logger.LogInformation("Saving data to JSON files in {Directory}", _dataDirectory);

            // Save each entity type to a separate JSON file
            await SaveEntityAsync(context.Categories.ToList(), "categories.json");
            // Transactions are split into monthly files (e.g. transactions-2025-01.json)
            await SaveEntityMonthlyAsync(context.Transactions.ToList(), "transactions", t => t.Date);
            await SaveEntityAsync(context.TransactionCategories.ToList(), "transactioncategories.json");
            await SaveEntityAsync(context.CategoryRules.ToList(), "categoryrules.json");
            await SaveEntityAsync(context.Budgets.ToList(), "budgets.json");
            await SaveEntityAsync(context.BudgetCategories.ToList(), "budgetcategories.json");
            await SaveEntityAsync(context.Investments.ToList(), "investments.json");
            await SaveEntityAsync(context.Assets.ToList(), "assets.json");
            await SaveEntityAsync(context.Loans.ToList(), "loans.json");
            await SaveEntityAsync(context.Goals.ToList(), "goals.json");
            await SaveEntityAsync(context.BankSources.ToList(), "banksources.json");
            await SaveEntityAsync(context.BankConnections.ToList(), "bankconnections.json");
            await SaveEntityAsync(context.Households.ToList(), "households.json");
            await SaveEntityAsync(context.HouseholdMembers.ToList(), "householdmembers.json");
            await SaveEntityAsync(context.SharedExpenses.ToList(), "sharedexpenses.json");
            await SaveEntityAsync(context.ExpenseShares.ToList(), "expenseshares.json");
            await SaveEntityAsync(context.ChildAllowances.ToList(), "childallowances.json");
            await SaveEntityMonthlyAsync(context.AllowanceTransactions.ToList(), "allowancetransactions", t => t.TransactionDate);
            await SaveEntityAsync(context.AllowanceTasks.ToList(), "allowancetasks.json");
            await SaveEntityAsync(context.SalaryHistories.ToList(), "salaryhistories.json");
            await SaveEntityAsync(context.Pockets.ToList(), "pockets.json");
            await SaveEntityMonthlyAsync(context.PocketTransactions.ToList(), "pockettransactions", t => t.TransactionDate);
            await SaveEntityAsync(context.SharedGoals.ToList(), "sharedgoals.json");
            await SaveEntityAsync(context.SharedGoalParticipants.ToList(), "sharedgoalparticipants.json");
            await SaveEntityAsync(context.SharedGoalProposals.ToList(), "sharedgoalproposals.json");
            await SaveEntityAsync(context.SharedGoalProposalVotes.ToList(), "sharedgoalproposalvotes.json");
            await SaveEntityMonthlyAsync(context.SharedGoalTransactions.ToList(), "sharedgoaltransactions", t => t.TransactionDate);
            await SaveEntityAsync(context.SharedGoalNotifications.ToList(), "sharedgoalnotifications.json");
            await SaveEntityAsync(context.TaxDeductions.ToList(), "taxdeductions.json");
            await SaveEntityAsync(context.CapitalGains.ToList(), "capitalgains.json");
            await SaveEntityAsync(context.CommuteDeductions.ToList(), "commutedeductions.json");
            await SaveEntityAsync(context.CreditRatings.ToList(), "creditratings.json");
            await SaveEntityAsync(context.NetWorthSnapshots.ToList(), "networthsnapshots.json");
            await SaveEntityAsync(context.CurrencyAccounts.ToList(), "currencyaccounts.json");
            await SaveEntityAsync(context.AuditLogs.ToList(), "auditlogs.json");

            _logger.LogInformation("Data saved successfully to JSON files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data to JSON files");
            throw;
        }
    }

    public async Task LoadAsync(PrivatekonomyContext context)
    {
        try
        {
            _logger.LogInformation("Loading data from JSON files in {Directory}", _dataDirectory);

            var migrationSnapshotPath = Path.Combine(_dataDirectory, "privatekonomi-migration.json");
            if (File.Exists(migrationSnapshotPath))
            {
                await LoadMigrationSnapshotAsync(context, migrationSnapshotPath);
                _logger.LogInformation("Data loaded successfully from migration snapshot");
                return;
            }

            // Load in order to respect foreign key relationships
            await LoadEntityAsync<Category>(context, context.Categories, "categories.json");
            await LoadEntityAsync<BankSource>(context, context.BankSources, "banksources.json");
            await LoadEntityAsync<BankConnection>(context, context.BankConnections, "bankconnections.json");
            await LoadEntityAsync<Household>(context, context.Households, "households.json");
            await LoadEntityAsync<HouseholdMember>(context, context.HouseholdMembers, "householdmembers.json");
            // Transactions are stored in monthly files; also supports loading legacy single file
            await LoadEntityMonthlyAsync<Transaction>(context, context.Transactions, "transactions");
            await LoadEntityAsync<TransactionCategory>(context, context.TransactionCategories, "transactioncategories.json");
            await LoadEntityAsync<CategoryRule>(context, context.CategoryRules, "categoryrules.json");
            await LoadEntityAsync<Budget>(context, context.Budgets, "budgets.json");
            await LoadEntityAsync<BudgetCategory>(context, context.BudgetCategories, "budgetcategories.json");
            await LoadEntityAsync<Investment>(context, context.Investments, "investments.json");
            await LoadEntityAsync<Asset>(context, context.Assets, "assets.json");
            await LoadEntityAsync<Loan>(context, context.Loans, "loans.json");
            await LoadEntityAsync<Goal>(context, context.Goals, "goals.json");
            await LoadEntityAsync<SharedExpense>(context, context.SharedExpenses, "sharedexpenses.json");
            await LoadEntityAsync<ExpenseShare>(context, context.ExpenseShares, "expenseshares.json");
            await LoadEntityAsync<ChildAllowance>(context, context.ChildAllowances, "childallowances.json");
            await LoadEntityMonthlyAsync<AllowanceTransaction>(context, context.AllowanceTransactions, "allowancetransactions");
            await LoadEntityAsync<AllowanceTask>(context, context.AllowanceTasks, "allowancetasks.json");
            await LoadEntityAsync<SalaryHistory>(context, context.SalaryHistories, "salaryhistories.json");
            await LoadEntityAsync<Pocket>(context, context.Pockets, "pockets.json");
            await LoadEntityMonthlyAsync<PocketTransaction>(context, context.PocketTransactions, "pockettransactions");
            await LoadEntityAsync<SharedGoal>(context, context.SharedGoals, "sharedgoals.json");
            await LoadEntityAsync<SharedGoalParticipant>(context, context.SharedGoalParticipants, "sharedgoalparticipants.json");
            await LoadEntityAsync<SharedGoalProposal>(context, context.SharedGoalProposals, "sharedgoalproposals.json");
            await LoadEntityAsync<SharedGoalProposalVote>(context, context.SharedGoalProposalVotes, "sharedgoalproposalvotes.json");
            await LoadEntityMonthlyAsync<SharedGoalTransaction>(context, context.SharedGoalTransactions, "sharedgoaltransactions");
            await LoadEntityAsync<SharedGoalNotification>(context, context.SharedGoalNotifications, "sharedgoalnotifications.json");
            await LoadEntityAsync<TaxDeduction>(context, context.TaxDeductions, "taxdeductions.json");
            await LoadEntityAsync<CapitalGain>(context, context.CapitalGains, "capitalgains.json");
            await LoadEntityAsync<CommuteDeduction>(context, context.CommuteDeductions, "commutedeductions.json");
            await LoadEntityAsync<CreditRating>(context, context.CreditRatings, "creditratings.json");
            await LoadEntityAsync<NetWorthSnapshot>(context, context.NetWorthSnapshots, "networthsnapshots.json");
            await LoadEntityAsync<CurrencyAccount>(context, context.CurrencyAccounts, "currencyaccounts.json");
            await LoadEntityAsync<AuditLog>(context, context.AuditLogs, "auditlogs.json");

            _logger.LogInformation("Data loaded successfully from JSON files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from JSON files");
            throw;
        }
    }

    private async Task LoadMigrationSnapshotAsync(
        PrivatekonomyContext context,
        string snapshotPath)
    {
        await using var stream = File.OpenRead(snapshotPath);
        var snapshot = await JsonSerializer.DeserializeAsync<DataMigrationSnapshot>(stream, _jsonOptions)
                       ?? throw new InvalidOperationException("JSON-migrationssnapshot kunde inte läsas.");
        snapshot.ValidateIntegrity(_jsonOptions);

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (entityType.FindOwnership() != null)
                continue;

            var typeName = entityType.ClrType.FullName ?? entityType.ClrType.Name;
            if (!snapshot.Entities.TryGetValue(typeName, out var records))
                continue;

            var setMethod = typeof(DbContext).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(method => method.Name == "Set" &&
                                  method.IsGenericMethodDefinition &&
                                  method.GetParameters().Length == 0);
            var entitySet = (IQueryable)(setMethod.MakeGenericMethod(entityType.ClrType)
                .Invoke(context, null)!);
            var existingEntities = await entitySet.Cast<object>().ToListAsync();
            foreach (var record in records)
            {
                var entity = Activator.CreateInstance(entityType.ClrType)
                             ?? throw new InvalidOperationException(
                                 $"Det går inte att skapa entitetstypen {entityType.ClrType.Name}.");

                foreach (var property in entityType.GetProperties())
                {
                    var propertyInfo = property.PropertyInfo;
                    if (propertyInfo == null || !propertyInfo.CanWrite ||
                        !record.TryGetValue(property.Name, out var jsonElement))
                        continue;

                    propertyInfo.SetValue(
                        entity,
                        jsonElement.ValueKind == JsonValueKind.Null
                            ? null
                            : jsonElement.Deserialize(property.ClrType, _jsonOptions));
                }

                var primaryKey = entityType.FindPrimaryKey();
                if (primaryKey != null &&
                    existingEntities.Any(existing => primaryKey.Properties.All(property =>
                    {
                        var propertyInfo = property.PropertyInfo;
                        return propertyInfo != null &&
                               Equals(propertyInfo.GetValue(entity), propertyInfo.GetValue(existing));
                    })))
                    continue;

                await context.AddAsync(entity);
                existingEntities.Add(entity);
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task SaveEntityAsync<T>(List<T> entities, string fileName)
    {
        if (entities.Count == 0)
        {
            // Don't create empty files, but delete if exists
            var filePath = Path.Combine(_dataDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return;
        }

        var json = JsonSerializer.Serialize(entities, _jsonOptions);
        var path = Path.Combine(_dataDirectory, fileName);
        await File.WriteAllTextAsync(path, json);
    }

    /// <summary>
    /// Saves a list of entities to monthly JSON files named {baseFileName}-YYYY-MM.json.
    /// Existing monthly files for this entity type are replaced on each save.
    /// Any legacy single-file (baseFileName.json) is removed after successful save.
    /// </summary>
    private async Task SaveEntityMonthlyAsync<T>(List<T> entities, string baseFileName, Func<T, DateTime> dateSelector)
    {
        // Remove all existing monthly files for this entity type
        var existingMonthlyFiles = Directory.GetFiles(_dataDirectory, $"{baseFileName}-*.json");
        foreach (var file in existingMonthlyFiles)
        {
            File.Delete(file);
        }

        // Remove legacy single-file if it exists
        var legacyFile = Path.Combine(_dataDirectory, $"{baseFileName}.json");
        if (File.Exists(legacyFile))
        {
            File.Delete(legacyFile);
        }

        if (entities.Count == 0)
        {
            return;
        }

        // Group entities by year and month, then save each group to its own file
        var groups = entities.GroupBy(e =>
        {
            var date = dateSelector(e);
            return (date.Year, date.Month);
        });

        foreach (var group in groups)
        {
            var fileName = $"{baseFileName}-{group.Key.Year:D4}-{group.Key.Month:D2}.json";
            await SaveEntityAsync(group.ToList(), fileName);
        }
    }

    private async Task LoadEntityAsync<T>(
        PrivatekonomyContext context,
        DbSet<T> dbSet,
        string fileName) where T : class
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(path);
        var entities = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
        
        if (entities != null && entities.Count > 0)
        {
            // Clear change tracker before loading to avoid entity tracking conflicts
            context.ChangeTracker.Clear();
            
            // Check if there's existing data using AsNoTracking to avoid tracking conflicts
            var hasExistingData = await dbSet.AsNoTracking().AnyAsync();
            if (hasExistingData)
            {
                // For InMemory database, we need to read the entities first (without tracking)
                // then remove them
                var existingEntities = await dbSet.ToListAsync();
                dbSet.RemoveRange(existingEntities);
                await context.SaveChangesAsync();
                
                // Clear change tracker again after removal
                context.ChangeTracker.Clear();
            }
            
            await dbSet.AddRangeAsync(entities);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Loads entities from monthly JSON files named {baseFileName}-YYYY-MM.json.
    /// Also loads from a legacy single file {baseFileName}.json if present (backward compatibility).
    /// </summary>
    private async Task LoadEntityMonthlyAsync<T>(
        PrivatekonomyContext context,
        DbSet<T> dbSet,
        string baseFileName) where T : class
    {
        var monthlyFiles = Directory.GetFiles(_dataDirectory, $"{baseFileName}-*.json")
            .OrderBy(f => f)
            .ToList();

        // Also include legacy single-file for backward compatibility
        var legacyFile = Path.Combine(_dataDirectory, $"{baseFileName}.json");
        if (File.Exists(legacyFile))
        {
            monthlyFiles.Add(legacyFile);
        }

        if (monthlyFiles.Count == 0)
        {
            return;
        }

        var allEntities = new List<T>();
        foreach (var file in monthlyFiles)
        {
            var json = await File.ReadAllTextAsync(file);
            var entities = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            if (entities != null)
            {
                allEntities.AddRange(entities);
            }
        }

        if (allEntities.Count > 0)
        {
            context.ChangeTracker.Clear();

            var hasExistingData = await dbSet.AsNoTracking().AnyAsync();
            if (hasExistingData)
            {
                var existingEntities = await dbSet.ToListAsync();
                dbSet.RemoveRange(existingEntities);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }

            await dbSet.AddRangeAsync(allEntities);
            await context.SaveChangesAsync();
        }
    }
}
