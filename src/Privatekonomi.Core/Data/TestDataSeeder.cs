using Microsoft.AspNetCore.Identity;
using Privatekonomi.Core.Models;
using System.Globalization;

namespace Privatekonomi.Core.Data;

public static class TestDataSeeder
{
    /// <summary>
    /// Seeds production reference data that should always be available (challenge templates, etc.)
    /// This method is called regardless of the SeedTestData setting.
    /// </summary>
    public static void SeedProductionReferenceData(PrivatekonomyContext context)
    {
        // Seed challenge templates if they don't exist
        SeedChallengeTemplates(context);
    }

    public static async Task SeedTestDataAsync(PrivatekonomyContext context, UserManager<ApplicationUser> userManager)
    {
        // Check if there are already transactions to avoid duplicate seeding
        if (context.Transactions.Any())
        {
            return;
        }

        // Create test users first
        var testUserId = await SeedUsers(userManager);
        
        // Seed data with user association
        SeedTransactions(context, testUserId);
        SeedInvestments(context, testUserId);
        SeedAssets(context, testUserId);
        SeedLoans(context, testUserId);
        SeedBudgets(context, testUserId);
        SeedCategoryRules(context);
        SeedHouseholds(context);
        SeedGoals(context, testUserId);
        SeedSalaryHistory(context, testUserId);
        SeedNetWorthSnapshots(context, testUserId);
        SeedPockets(context, testUserId);
        // Challenge templates are now seeded via SeedProductionReferenceData
        SeedSubscriptions(context, testUserId);
        SeedBills(context, testUserId);
        SeedBillReminders(context, testUserId);
        SeedPensions(context, testUserId);
        SeedDividends(context, testUserId);
        SeedInvestmentTransactions(context, testUserId);
        SeedSavingsChallenges(context, testUserId);
        SeedCurrencyAccounts(context, testUserId);
        SeedLifeTimelineMilestones(context, testUserId);
        SeedNotifications(context, testUserId);
        SeedAuditLogs(context, testUserId);
        SeedAdditionalBudgets(context, testUserId);
        SeedMoreInvestmentHistory(context, testUserId);
        SeedMoreNetWorthSnapshots(context, testUserId);
        SeedMoreTransactions(context, testUserId);
        SeedMoreGoals(context, testUserId);
        SeedMoreSubscriptions(context, testUserId);
        SeedReceipts(context, testUserId);
        SeedPortfolioAllocations(context, testUserId);
        SeedSavingsGroups(context, testUserId);
        SeedLifeTimelineScenarios(context, testUserId);
        SeedGoalShares(context, testUserId);
        
        //// Missing seed placeholders for other data types
        // SeedBankConnections(context);
        // SeedTaxDeductions(context, testUserId);
        // SeedCapitalGains(context, testUserId);
        // SeedCommuteDeductions(context, testUserId);
        // SeedCreditRatings(context, testUserId);
        // SeedMLModels(context);
        // SeedUserFeedback(context, testUserId);
        // SeedNotificationPreferences(context, testUserId);
        // SeedDoNotDisturbSchedules(context, testUserId);
        // SeedUserPrivacySettings(context, testUserId);
    }
    
    private static async Task<string> SeedUsers(UserManager<ApplicationUser> userManager)
    {
        // Create a test user
        var testUser = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Användare",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            IsSystemAdmin = true // Mark test user as system admin for testing admin dashboard
        };

        var existingUser = await userManager.FindByEmailAsync(testUser.Email);
        if (existingUser == null)
        {
            await userManager.CreateAsync(testUser, "Test123!");
            existingUser = testUser;
        }
        else
        {
            // Update existing user to be system admin
            existingUser.IsSystemAdmin = true;
            await userManager.UpdateAsync(existingUser);
        }

        return existingUser.Id;
    }

    private static void SeedCategoryRules(PrivatekonomyContext context)
    {
        // Only seed if no rules exist
        if (context.CategoryRules.Any())
        {
            return;
        }

        var categoryRules = new List<CategoryRule>
        {
            // Mat & Dryck (Category 1) - High priority
            new CategoryRule { CategoryRuleId = 1, Pattern = "ICA", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 100, IsActive = true, Description = "ICA stores", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 2, Pattern = "Coop", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 100, IsActive = true, Description = "Coop stores", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 3, Pattern = "Willys", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 100, IsActive = true, Description = "Willys stores", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 4, Pattern = "Hemköp", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 100, IsActive = true, Description = "Hemköp stores", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 5, Pattern = "Restaurant", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 90, IsActive = true, Description = "Restaurants", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 6, Pattern = "Restaurang", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 90, IsActive = true, Description = "Swedish restaurants", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 7, Pattern = "Cafe", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 90, IsActive = true, Description = "Cafes", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 8, Pattern = "McDonalds", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 95, IsActive = true, Description = "McDonald's", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 9, Pattern = "Pizza", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 90, IsActive = true, Description = "Pizza places", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 10, Pattern = "Sushi", MatchType = PatternMatchType.Contains, CategoryId = 1, Priority = 90, IsActive = true, Description = "Sushi restaurants", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },

            // Transport (Category 2)
            new CategoryRule { CategoryRuleId = 11, Pattern = "SL-kort", MatchType = PatternMatchType.Contains, CategoryId = 2, Priority = 100, IsActive = true, Description = "Stockholm public transport", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 12, Pattern = "Bensin", MatchType = PatternMatchType.Contains, CategoryId = 2, Priority = 95, IsActive = true, Description = "Gas stations", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 13, Pattern = "Circle K", MatchType = PatternMatchType.Contains, CategoryId = 2, Priority = 100, IsActive = true, Description = "Circle K gas", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 14, Pattern = "Preem", MatchType = PatternMatchType.Contains, CategoryId = 2, Priority = 100, IsActive = true, Description = "Preem gas", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 15, Pattern = "Parkering", MatchType = PatternMatchType.Contains, CategoryId = 2, Priority = 95, IsActive = true, Description = "Parking", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 16, Pattern = "Taxi", MatchType = PatternMatchType.Contains, CategoryId = 2, Priority = 95, IsActive = true, Description = "Taxi services", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 17, Pattern = "SJ", MatchType = PatternMatchType.Contains, CategoryId = 2, Priority = 100, IsActive = true, Description = "Swedish railways", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },

            // Boende (Category 3)
            new CategoryRule { CategoryRuleId = 18, Pattern = "Hyra", MatchType = PatternMatchType.Contains, CategoryId = 3, Priority = 100, IsActive = true, Description = "Rent", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 19, Pattern = "Vattenfall", MatchType = PatternMatchType.Contains, CategoryId = 3, Priority = 100, IsActive = true, Description = "Vattenfall electricity", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 20, Pattern = "Telia", MatchType = PatternMatchType.Contains, CategoryId = 3, Priority = 100, IsActive = true, Description = "Telia broadband", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 21, Pattern = "Bredband", MatchType = PatternMatchType.Contains, CategoryId = 3, Priority = 95, IsActive = true, Description = "Broadband", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 22, Pattern = "Försäkring", MatchType = PatternMatchType.Contains, CategoryId = 3, Priority = 95, IsActive = true, Description = "Insurance", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },

            // Nöje (Category 4)
            new CategoryRule { CategoryRuleId = 23, Pattern = "Spotify", MatchType = PatternMatchType.Contains, CategoryId = 4, Priority = 100, IsActive = true, Description = "Spotify subscription", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 24, Pattern = "Netflix", MatchType = PatternMatchType.Contains, CategoryId = 4, Priority = 100, IsActive = true, Description = "Netflix subscription", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 25, Pattern = "Bio", MatchType = PatternMatchType.Contains, CategoryId = 4, Priority = 90, IsActive = true, Description = "Cinema", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 26, Pattern = "SF", MatchType = PatternMatchType.StartsWith, CategoryId = 4, Priority = 95, IsActive = true, Description = "SF Bio cinema", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 27, Pattern = "Gym", MatchType = PatternMatchType.Contains, CategoryId = 4, Priority = 90, IsActive = true, Description = "Gym memberships", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 28, Pattern = "Teater", MatchType = PatternMatchType.Contains, CategoryId = 4, Priority = 90, IsActive = true, Description = "Theatre", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 29, Pattern = "Konsert", MatchType = PatternMatchType.Contains, CategoryId = 4, Priority = 90, IsActive = true, Description = "Concerts", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },

            // Shopping (Category 5)
            new CategoryRule { CategoryRuleId = 30, Pattern = "H&M", MatchType = PatternMatchType.Contains, CategoryId = 5, Priority = 100, IsActive = true, Description = "H&M stores", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 31, Pattern = "IKEA", MatchType = PatternMatchType.Contains, CategoryId = 5, Priority = 100, IsActive = true, Description = "IKEA", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 32, Pattern = "Elgiganten", MatchType = PatternMatchType.Contains, CategoryId = 5, Priority = 100, IsActive = true, Description = "Elgiganten electronics", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 33, Pattern = "Clas Ohlson", MatchType = PatternMatchType.Contains, CategoryId = 5, Priority = 100, IsActive = true, Description = "Clas Ohlson", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 34, Pattern = "Stadium", MatchType = PatternMatchType.Contains, CategoryId = 5, Priority = 100, IsActive = true, Description = "Stadium sports", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 35, Pattern = "Apoteket", MatchType = PatternMatchType.Contains, CategoryId = 5, Priority = 95, IsActive = true, Description = "Pharmacy", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },

            // Hälsa (Category 6)
            new CategoryRule { CategoryRuleId = 36, Pattern = "Folktandvården", MatchType = PatternMatchType.Contains, CategoryId = 6, Priority = 100, IsActive = true, Description = "Public dental care", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 37, Pattern = "Apotek", MatchType = PatternMatchType.Contains, CategoryId = 6, Priority = 95, IsActive = true, Description = "Pharmacy (health)", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 38, Pattern = "Naprapat", MatchType = PatternMatchType.Contains, CategoryId = 6, Priority = 100, IsActive = true, Description = "Naprapathy", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 39, Pattern = "Vitaminer", MatchType = PatternMatchType.Contains, CategoryId = 6, Priority = 90, IsActive = true, Description = "Vitamins", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },

            // Lön (Category 7)
            new CategoryRule { CategoryRuleId = 40, Pattern = "Lön", MatchType = PatternMatchType.Contains, CategoryId = 7, Priority = 100, IsActive = true, Description = "Salary", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 41, Pattern = "Bonus", MatchType = PatternMatchType.Contains, CategoryId = 7, Priority = 95, IsActive = true, Description = "Bonus", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 42, Pattern = "Semesterersättning", MatchType = PatternMatchType.Contains, CategoryId = 7, Priority = 95, IsActive = true, Description = "Vacation pay", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },

            // Sparande (Category 8)
            new CategoryRule { CategoryRuleId = 43, Pattern = "Sparande", MatchType = PatternMatchType.Contains, CategoryId = 8, Priority = 100, IsActive = true, Description = "Savings", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
            new CategoryRule { CategoryRuleId = 44, Pattern = "ISK", MatchType = PatternMatchType.Contains, CategoryId = 8, Priority = 95, IsActive = true, Description = "Investment savings account", Field = MatchField.Both, RuleType = RuleType.System, CreatedAt = DateTime.UtcNow },
        };

        context.CategoryRules.AddRange(categoryRules);
        context.SaveChanges();
    }

    private static void SeedTransactions(PrivatekonomyContext context, string userId)
    {

        var random = new Random(42); // Fixed seed for reproducible data
        var startDate = DateTime.UtcNow.AddDays(-550); // Start from approximately 18 months ago

        var transactions = new List<Transaction>();
        var transactionCategories = new List<TransactionCategory>();

        // Transaction descriptions for different categories
        var descriptions = new Dictionary<int, string[]>
        {
            { 1, new[] { "ICA Maxi", "Coop", "Willys", "Hemköp", "Restaurant", "Restaurang Bella", "Cafe latte", "McDonalds", "Pizzeria", "Sushi bar" } }, // Mat & Dryck
            { 2, new[] { "SL-kort", "Bensin Circle K", "Parkering", "Bensin Preem", "Taxi Stockholm", "Biltvätt", "SJ tåg", "Flygbuss" } }, // Transport
            { 3, new[] { "Hyra lägenhet", "El Vattenfall", "Bredband Telia", "Försäkring Folksam", "Reparation" } }, // Boende
            { 4, new[] { "Bio SF", "Spotify", "Netflix", "Gym medlemskap", "Teater biljett", "Konsert", "Bowlinghallen", "Eventbiljett" } }, // Nöje
            { 5, new[] { "H&M", "Elgiganten", "IKEA", "Clas Ohlson", "Stadium", "Apoteket", "Bokus.com", "Webhallen" } }, // Shopping
            { 6, new[] { "Folktandvården", "Apotek", "Naprapat", "Gym kort", "Vitaminer", "Glasögon Synoptik" } }, // Hälsa
            { 7, new[] { "Lön arbetsgivare", "Bonus", "Semesterersättning" } }, // Lön
            { 8, new[] { "Sparande sparkonto", "Månadsspar aktier", "ISK insättning" } }, // Sparande
            { 9, new[] { "Swish betalning", "Gåva", "Okategoriserad", "Diverse", "Kontantuttag" } } // Övrigt
        };

        // Notes for transactions to add context
        var transactionNotes = new Dictionary<int, string?[]>
        {
            { 1, new string?[] { "Veckans matinköp", "Snabbhandling efter jobbet", "Storkok för veckan", "Lunch med kollegor", "Fredagsmys", "Söndagsmiddag", null, null } },
            { 2, new string?[] { "Månadskort", "Tankning för bilsemester", "Pendling till jobbet", "Resa till Göteborg", null, null } },
            { 3, new string?[] { "Månadshyra december", "Elräkning för november", "Hemförsäkring årspremie", null } },
            { 4, new string?[] { "Film med familjen", "Månadsprenumeration", "Träningspass", "Kväll på bio", "Konsert med vänner", null } },
            { 5, new string?[] { "Nya kläder", "Julklapp till barn", "Ny dator till hemmakontor", "Hushållsprylar", null } },
            { 6, new string?[] { "Tandläkarkontroll", "Medicin för förkylning", "Årlig hälsokontroll", null } },
            { 7, new string?[] { "Månadslön", "Årsbonus", "Semesterpeng", null } },
            { 8, new string?[] { "Automatiskt sparande", "Extra sparande denna månad", "Överföring till ISK", null } },
            { 9, new string?[] { "Betalning till vän", "Julklapp", null } }
        };

        // Payees for different categories
        var payees = new Dictionary<int, string?[]>
        {
            { 1, new string?[] { "ICA AB", "KF Coop", "Axfood AB", "Axfood AB", "Bella Italia AB", "Bella Italia AB", "Espresso House", "McDonald's Sverige", "Pizzeria Napoli", "Sushi Yama" } },
            { 2, new string?[] { "SL AB", "Circle K", "Stockholm Parkering", "Preem AB", "Taxi Stockholm", "OK Biltvätt", "SJ AB", "Flygbussarna" } },
            { 3, new string?[] { "Hyresvärden", "Vattenfall AB", "Telia Company AB", "Folksam", "Hantverkare AB" } },
            { 4, new string?[] { "SF Bio", "Spotify AB", "Netflix International", "Fitness24Seven", "Dramaten", "Live Nation", "Bowlinghallen Stockholm", "Ticketmaster" } },
            { 5, new string?[] { "H&M AB", "Elgiganten AB", "IKEA Sverige", "Clas Ohlson AB", "Stadium AB", "Apoteket AB", "Bokus AB", "Webhallen AB" } },
            { 6, new string?[] { "Folktandvården", "Apoteket AB", "Naprapatkliniken", "Fitness24Seven", "Vitaminer.se", "Synoptik AB" } },
            { 7, new string?[] { "Arbetsgivare AB", "Arbetsgivare AB", "Arbetsgivare AB" } },
            { 8, new string?[] { null, null, null } },
            { 9, new string?[] { null, null, null, null, null } }
        };

        // Payment methods for different categories
        var paymentMethods = new[] { "Kort", "Swish", "Autogiro", "Banköverföring", "Kontant", "E-faktura" };

        // Bank source IDs to simulate transactions from different banks (1-6 as per seeded data)
        var bankSourceIds = new[] { 1, 2, 3, 4, 5, 6 };

        // Amount ranges for different categories (min, max)
        var amountRanges = new Dictionary<int, (decimal min, decimal max)>
        {
            { 1, (50m, 800m) },    // Mat & Dryck
            { 2, (80m, 500m) },    // Transport
            { 3, (200m, 15000m) }, // Boende
            { 4, (100m, 600m) },   // Nöje
            { 5, (150m, 3000m) },  // Shopping
            { 6, (200m, 1500m) },  // Hälsa
            { 7, (25000m, 45000m) }, // Lön
            { 8, (500m, 5000m) },  // Sparande
            { 9, (100m, 1000m) }   // Övrigt
        };

        int transactionId = 1;
        int transactionCategoryId = 1;

        // Generate 300 transactions (approximately 6x more than before to cover 18 months vs 3 months)
        for (int i = 0; i < 300; i++)
        {
            // Randomly select a category (weighted towards common categories)
            var categoryWeights = new[] { 1, 1, 1, 2, 2, 3, 3, 4, 5, 6, 7, 8, 9, 1, 2 };
            var categoryId = categoryWeights[random.Next(categoryWeights.Length)];
            
            // Select a random description for this category
            var descArray = descriptions[categoryId];
            var description = descArray[random.Next(descArray.Length)];
            
            // Select notes, payee, and payment method
            var notesArray = transactionNotes[categoryId];
            var notes = notesArray[random.Next(notesArray.Length)];
            
            var payeeArray = payees[categoryId];
            var payee = payeeArray[random.Next(payeeArray.Length)];
            
            var paymentMethod = paymentMethods[random.Next(paymentMethods.Length)];
            
            // For recurring transactions (like subscriptions in category 3, 4)
            var isRecurring = (categoryId == 3 || categoryId == 4) && random.Next(0, 100) < 30; // 30% chance
            
            // Generate amount within the range for this category
            var range = amountRanges[categoryId];
            var amount = Math.Round(range.min + (decimal)random.NextDouble() * (range.max - range.min), 2);
            
            // Generate random date within the last 18 months (approximately 550 days)
            var daysBack = random.Next(0, 550);
            var date = startDate.AddDays(daysBack);
            
            // Determine if it's income (category 7 = Lön is always income)
            var isIncome = categoryId == 7;

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Amount = amount,
                Description = description,
                Date = date,
                IsIncome = isIncome,
                BankSourceId = bankSourceIds[random.Next(bankSourceIds.Length)],
                Currency = "SEK",
                Imported = false,
                Cleared = true,
                Notes = notes,
                Payee = payee,
                PaymentMethod = paymentMethod,
                IsRecurring = isRecurring,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            };

            transactions.Add(transaction);

            // Create transaction category relationship
            var transactionCategory = new TransactionCategory
            {
                TransactionCategoryId = transactionCategoryId,
                TransactionId = transactionId,
                CategoryId = categoryId,
                Amount = amount
            };

            transactionCategories.Add(transactionCategory);

            transactionId++;
            transactionCategoryId++;
        }

        // Add 5 unmapped transactions (without categories) to demonstrate the feature
        var unmappedDescriptions = new[] { "Okänd transaktion", "Kontant betalning", "Swish från okänd", "Okategoriserad köp", "Diverse utgift" };
        var unmappedNotes = new string?[] { "Behöver kategoriseras", "Glömt kvitto", "Privatköp", "Vet inte vad detta var", null };
        for (int i = 0; i < 5; i++)
        {
            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Amount = Math.Round(100m + (decimal)random.NextDouble() * 500m, 2),
                Description = unmappedDescriptions[i],
                Date = startDate.AddDays(random.Next(0, 550)),
                IsIncome = false,
                BankSourceId = bankSourceIds[random.Next(bankSourceIds.Length)],
                Currency = "SEK",
                Imported = false,
                Cleared = false,
                Notes = unmappedNotes[i],
                PaymentMethod = paymentMethods[random.Next(paymentMethods.Length)],
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            };
            
            transactions.Add(transaction);
            transactionId++;
        }

        // Add all transactions and categories to context
        context.Transactions.AddRange(transactions);
        context.TransactionCategories.AddRange(transactionCategories);
        context.SaveChanges();
    }

    private static void SeedInvestments(PrivatekonomyContext context, string userId)
    {
        var investments = new List<Investment>
        {
            new Investment
            {
                InvestmentId = 1,
                Name = "Volvo B",
                ShortName = "VOLV-B",
                Type = "Aktie",
                Quantity = 100,
                PurchasePrice = 245.50m,
                CurrentPrice = 268.75m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-6),
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Market = "Stockholm",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Investment
            {
                InvestmentId = 2,
                Name = "SEB A",
                ShortName = "SEB-A",
                Type = "Aktie",
                Quantity = 50,
                PurchasePrice = 152.30m,
                CurrentPrice = 165.20m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-8),
                LastUpdated = DateTime.UtcNow.AddDays(-2),
                Market = "Stockholm",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Investment
            {
                InvestmentId = 3,
                Name = "Investor B",
                ShortName = "INVE-B",
                Type = "Aktie",
                Quantity = 75,
                PurchasePrice = 289.00m,
                CurrentPrice = 312.50m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-4),
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Market = "Stockholm",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Investment
            {
                InvestmentId = 4,
                Name = "SPP Aktiefond Global",
                Type = "Fond",
                Quantity = 150,
                PurchasePrice = 125.75m,
                CurrentPrice = 138.40m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-12),
                LastUpdated = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Investment
            {
                InvestmentId = 5,
                Name = "Länsförsäkringar Sverige",
                Type = "Fond",
                Quantity = 200,
                PurchasePrice = 98.50m,
                CurrentPrice = 104.80m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-10),
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Investment
            {
                InvestmentId = 6,
                Name = "Avanza Global",
                Type = "Fond",
                Quantity = 120,
                PurchasePrice = 215.30m,
                CurrentPrice = 232.90m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-9),
                LastUpdated = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Investment
            {
                InvestmentId = 7,
                Name = "Ericsson B",
                ShortName = "ERIC-B",
                Type = "Aktie",
                Quantity = 250,
                PurchasePrice = 58.20m,
                CurrentPrice = 62.15m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-5),
                LastUpdated = DateTime.UtcNow,
                Market = "Stockholm",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Investment
            {
                InvestmentId = 8,
                Name = "Nordea",
                ShortName = "NDA-SE",
                Type = "Aktie",
                Quantity = 80,
                PurchasePrice = 125.40m,
                CurrentPrice = 118.90m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-3),
                LastUpdated = DateTime.UtcNow.AddDays(-1),
                Market = "Stockholm",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            }
        };

        context.Investments.AddRange(investments);
        context.SaveChanges();
    }

    private static void SeedBudgets(PrivatekonomyContext context, string userId)
    {
        // Create a budget for the current month
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var currentMonthBudget = new Budget
        {
            BudgetId = 1,
            Name = "Månadsbudget " + now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("sv-SE")),
            Description = "Budget för den aktuella månaden",
            StartDate = startOfMonth,
            EndDate = endOfMonth,
            Period = BudgetPeriod.Monthly,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        // Add budget categories with planned amounts
        var budgetCategories = new List<BudgetCategory>
        {
            new BudgetCategory { BudgetCategoryId = 1, BudgetId = 1, CategoryId = 1, PlannedAmount = 5000m },  // Mat & Dryck
            new BudgetCategory { BudgetCategoryId = 2, BudgetId = 1, CategoryId = 2, PlannedAmount = 1500m },  // Transport
            new BudgetCategory { BudgetCategoryId = 3, BudgetId = 1, CategoryId = 3, PlannedAmount = 12000m }, // Boende
            new BudgetCategory { BudgetCategoryId = 4, BudgetId = 1, CategoryId = 4, PlannedAmount = 2000m },  // Nöje
            new BudgetCategory { BudgetCategoryId = 5, BudgetId = 1, CategoryId = 5, PlannedAmount = 3000m },  // Shopping
            new BudgetCategory { BudgetCategoryId = 6, BudgetId = 1, CategoryId = 6, PlannedAmount = 1000m },  // Hälsa
        };

        // Create a budget for the previous month
        var prevMonthStart = startOfMonth.AddMonths(-1);
        var prevMonthEnd = startOfMonth.AddDays(-1);

        var previousMonthBudget = new Budget
        {
            BudgetId = 2,
            Name = "Månadsbudget " + prevMonthStart.ToString("MMMM yyyy", new System.Globalization.CultureInfo("sv-SE")),
            Description = "Budget för föregående månad",
            StartDate = prevMonthStart,
            EndDate = prevMonthEnd,
            Period = BudgetPeriod.Monthly,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        var prevBudgetCategories = new List<BudgetCategory>
        {
            new BudgetCategory { BudgetCategoryId = 7, BudgetId = 2, CategoryId = 1, PlannedAmount = 4800m },  // Mat & Dryck
            new BudgetCategory { BudgetCategoryId = 8, BudgetId = 2, CategoryId = 2, PlannedAmount = 1400m },  // Transport
            new BudgetCategory { BudgetCategoryId = 9, BudgetId = 2, CategoryId = 3, PlannedAmount = 12000m }, // Boende
            new BudgetCategory { BudgetCategoryId = 10, BudgetId = 2, CategoryId = 4, PlannedAmount = 1800m }, // Nöje
            new BudgetCategory { BudgetCategoryId = 11, BudgetId = 2, CategoryId = 5, PlannedAmount = 2500m }, // Shopping
            new BudgetCategory { BudgetCategoryId = 12, BudgetId = 2, CategoryId = 6, PlannedAmount = 900m },  // Hälsa
        };

        context.Budgets.AddRange(new[] { currentMonthBudget, previousMonthBudget });
        context.BudgetCategories.AddRange(budgetCategories);
        context.BudgetCategories.AddRange(prevBudgetCategories);
        context.SaveChanges();
    }

    private static void SeedHouseholds(PrivatekonomyContext context)
    {
        // Create a sample household with members and shared expenses
        var household = new Household
        {
            HouseholdId = 1,
            Name = "Familjen Andersson",
            Description = "Hushåll med delad lägenhet i Stockholm",
            CreatedDate = DateTime.UtcNow.AddMonths(-6)
        };

        var members = new List<HouseholdMember>
        {
            new HouseholdMember
            {
                HouseholdMemberId = 1,
                HouseholdId = 1,
                Name = "Anna Andersson",
                Email = "anna@example.com",
                IsActive = true,
                JoinedDate = DateTime.UtcNow.AddMonths(-6)
            },
            new HouseholdMember
            {
                HouseholdMemberId = 2,
                HouseholdId = 1,
                Name = "Erik Andersson",
                Email = "erik@example.com",
                IsActive = true,
                JoinedDate = DateTime.UtcNow.AddMonths(-6)
            },
            new HouseholdMember
            {
                HouseholdMemberId = 3,
                HouseholdId = 1,
                Name = "Sara Johansson",
                Email = "sara@example.com",
                IsActive = true,
                JoinedDate = DateTime.UtcNow.AddMonths(-3)
            }
        };

        // Create shared expenses with different distribution methods
        var sharedExpenses = new List<SharedExpense>
        {
            new SharedExpense
            {
                SharedExpenseId = 1,
                HouseholdId = 1,
                Name = "Hyra lägenhet",
                Description = "Månadshyra för 3-rummare",
                TotalAmount = 15000m,
                Type = ExpenseType.Rent,
                ExpenseDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                SplitMethod = SplitMethod.Equal
            },
            new SharedExpense
            {
                SharedExpenseId = 2,
                HouseholdId = 1,
                Name = "El",
                Description = "Elräkning för månaden",
                TotalAmount = 1200m,
                Type = ExpenseType.Electricity,
                ExpenseDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15),
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                SplitMethod = SplitMethod.Equal
            },
            new SharedExpense
            {
                SharedExpenseId = 3,
                HouseholdId = 1,
                Name = "Bredband",
                Description = "Telia bredband 500 Mbit/s",
                TotalAmount = 399m,
                Type = ExpenseType.Internet,
                ExpenseDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                SplitMethod = SplitMethod.Equal
            }
        };

        // Create expense shares (equal distribution)
        var expenseShares = new List<ExpenseShare>();
        foreach (var expense in sharedExpenses)
        {
            var shareAmount = expense.TotalAmount / members.Count;
            foreach (var member in members)
            {
                expenseShares.Add(new ExpenseShare
                {
                    SharedExpenseId = expense.SharedExpenseId,
                    HouseholdMemberId = member.HouseholdMemberId,
                    ShareAmount = shareAmount,
                    SharePercentage = 100m / members.Count
                });
            }
        }

        // Create household activities (timeline items)
        var activities = new List<HouseholdActivity>
        {
            new HouseholdActivity
            {
                HouseholdActivityId = 1,
                HouseholdId = 1,
                Title = "Städade hela lägenheten",
                Description = "Genomförde storstädning i alla rum, dammade och moppade",
                Type = HouseholdActivityType.Cleaning,
                CompletedDate = DateTime.UtcNow.AddDays(-2),
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                CompletedByMemberId = 1
            },
            new HouseholdActivity
            {
                HouseholdActivityId = 2,
                HouseholdId = 1,
                Title = "Handlade mat för veckan",
                Description = "Veckohandling på ICA Maxi - totalt 2450 kr",
                Type = HouseholdActivityType.Shopping,
                CompletedDate = DateTime.UtcNow.AddDays(-5),
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                CompletedByMemberId = 2
            },
            new HouseholdActivity
            {
                HouseholdActivityId = 3,
                HouseholdId = 1,
                Title = "Lagade diskbänkskranen",
                Description = "Bytte ut packningsring som läckte",
                Type = HouseholdActivityType.Repair,
                CompletedDate = DateTime.UtcNow.AddDays(-7),
                CreatedDate = DateTime.UtcNow.AddDays(-7),
                CompletedByMemberId = 2
            },
            new HouseholdActivity
            {
                HouseholdActivityId = 4,
                HouseholdId = 1,
                Title = "Lagade söndagsmiddag",
                Description = "Bjöd familjen på husmanskost med köttbullar och potatismos",
                Type = HouseholdActivityType.Cooking,
                CompletedDate = DateTime.UtcNow.AddDays(-3),
                CreatedDate = DateTime.UtcNow.AddDays(-3),
                CompletedByMemberId = 1
            },
            new HouseholdActivity
            {
                HouseholdActivityId = 5,
                HouseholdId = 1,
                Title = "Klippte gräsmattan",
                Description = "Gräsklippning och trädgårdsarbete",
                Type = HouseholdActivityType.Maintenance,
                CompletedDate = DateTime.UtcNow.AddDays(-10),
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                CompletedByMemberId = 3
            }
        };

        // Create household tasks (to-do items)
        var tasks = new List<HouseholdTask>
        {
            new HouseholdTask
            {
                HouseholdTaskId = 1,
                HouseholdId = 1,
                Title = "Boka lägenhetssyn",
                Description = "Ring värden och boka tid för årlig besiktning",
                Category = HouseholdActivityType.General,
                Priority = HouseholdTaskPriority.High,
                DueDate = DateTime.UtcNow.AddDays(5),
                CreatedDate = DateTime.UtcNow.AddDays(-3),
                IsCompleted = false,
                AssignedToMemberId = 2
            },
            new HouseholdTask
            {
                HouseholdTaskId = 2,
                HouseholdId = 1,
                Title = "Tvätta fönster",
                Description = "Invändigt och utvändigt på alla fönster",
                Category = HouseholdActivityType.Cleaning,
                Priority = HouseholdTaskPriority.Normal,
                DueDate = DateTime.UtcNow.AddDays(14),
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                IsCompleted = false,
                AssignedToMemberId = 1
            },
            new HouseholdTask
            {
                HouseholdTaskId = 3,
                HouseholdId = 1,
                Title = "Handla julklappar",
                Description = "Köpa presenter till släkten",
                Category = HouseholdActivityType.Shopping,
                Priority = HouseholdTaskPriority.High,
                DueDate = DateTime.UtcNow.AddDays(30),
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                IsCompleted = false,
                AssignedToMemberId = 3
            },
            new HouseholdTask
            {
                HouseholdTaskId = 4,
                HouseholdId = 1,
                Title = "Beställa ny diskmaskin",
                Description = "Nuvarande diskmaskin är trasig, måste beställa ny",
                Category = HouseholdActivityType.Maintenance,
                Priority = HouseholdTaskPriority.High,
                DueDate = DateTime.UtcNow.AddDays(7),
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                IsCompleted = false,
                AssignedToMemberId = 2
            },
            new HouseholdTask
            {
                HouseholdTaskId = 5,
                HouseholdId = 1,
                Title = "Sortera garderoben",
                Description = "Gå igenom kläder och skänka det vi inte använder",
                Category = HouseholdActivityType.General,
                Priority = HouseholdTaskPriority.Low,
                DueDate = null,
                CreatedDate = DateTime.UtcNow.AddDays(-7),
                IsCompleted = false,
                AssignedToMemberId = 1
            },
            new HouseholdTask
            {
                HouseholdTaskId = 6,
                HouseholdId = 1,
                Title = "Dammsög bilen",
                Description = "Invändig rengöring av familjebilen",
                Category = HouseholdActivityType.Cleaning,
                Priority = HouseholdTaskPriority.Low,
                DueDate = DateTime.UtcNow.AddDays(20),
                CreatedDate = DateTime.UtcNow.AddDays(-4),
                IsCompleted = false
            },
            new HouseholdTask
            {
                HouseholdTaskId = 7,
                HouseholdId = 1,
                Title = "Byt glödlampor i hallen",
                Description = "Tre lampor är trasiga i hallen",
                Category = HouseholdActivityType.Maintenance,
                Priority = HouseholdTaskPriority.Normal,
                DueDate = DateTime.UtcNow.AddDays(3),
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                IsCompleted = false,
                AssignedToMemberId = 2
            },
            // Some completed tasks
            new HouseholdTask
            {
                HouseholdTaskId = 8,
                HouseholdId = 1,
                Title = "Tömma soporna",
                Description = "Ta ut sopor och återvinning",
                Category = HouseholdActivityType.General,
                Priority = HouseholdTaskPriority.Normal,
                DueDate = DateTime.UtcNow.AddDays(-1),
                CreatedDate = DateTime.UtcNow.AddDays(-8),
                IsCompleted = true,
                CompletedDate = DateTime.UtcNow.AddDays(-1),
                CompletedByMemberId = 3
            },
            new HouseholdTask
            {
                HouseholdTaskId = 9,
                HouseholdId = 1,
                Title = "Köpa mjölk och bröd",
                Description = "Snabbhandling på vägen hem",
                Category = HouseholdActivityType.Shopping,
                Priority = HouseholdTaskPriority.High,
                DueDate = DateTime.UtcNow.AddDays(-2),
                CreatedDate = DateTime.UtcNow.AddDays(-3),
                IsCompleted = true,
                CompletedDate = DateTime.UtcNow.AddDays(-2),
                CompletedByMemberId = 1
            }
        };

        context.Households.Add(household);
        context.HouseholdMembers.AddRange(members);
        context.SharedExpenses.AddRange(sharedExpenses);
        context.ExpenseShares.AddRange(expenseShares);
        context.HouseholdActivities.AddRange(activities);
        context.HouseholdTasks.AddRange(tasks);
        context.SaveChanges();
    }

    private static void SeedGoals(PrivatekonomyContext context, string userId)
    {
        // Create sample savings goals
        var goals = new List<Goal>
        {
            new Goal
            {
                GoalId = 1,
                Name = "Semesterresa till Japan",
                Description = "Spara till en två veckors resa till Japan",
                TargetAmount = 50000m,
                CurrentAmount = 15000m,
                TargetDate = DateTime.UtcNow.AddMonths(12),
                Priority = 1,
                FundedFromBankSourceId = 1,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Goal
            {
                GoalId = 2,
                Name = "Ny laptop",
                Description = "MacBook Pro för arbete och studier",
                TargetAmount = 25000m,
                CurrentAmount = 8500m,
                TargetDate = DateTime.UtcNow.AddMonths(6),
                Priority = 2,
                FundedFromBankSourceId = 1,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Goal
            {
                GoalId = 3,
                Name = "Nödfond",
                Description = "Buffert för oförutsedda utgifter - 3 månadslöner",
                TargetAmount = 90000m,
                CurrentAmount = 45000m,
                TargetDate = DateTime.UtcNow.AddMonths(18),
                Priority = 1,
                FundedFromBankSourceId = 2,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Goal
            {
                GoalId = 4,
                Name = "Kontantinsats lägenhet",
                Description = "Spara till 15% kontantinsats för lägenhet",
                TargetAmount = 300000m,
                CurrentAmount = 120000m,
                TargetDate = DateTime.UtcNow.AddMonths(24),
                Priority = 1,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Goal
            {
                GoalId = 5,
                Name = "Ny cykel",
                Description = "Elcykel för pendling",
                TargetAmount = 15000m,
                CurrentAmount = 12000m,
                TargetDate = DateTime.UtcNow.AddMonths(3),
                Priority = 3,
                FundedFromBankSourceId = 1,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            }
        };

        context.Goals.AddRange(goals);
        context.SaveChanges();

        // Add milestones for each goal
        var milestones = new List<GoalMilestone>();
        int milestoneId = 1;

        // Milestones for Goal 1: Semesterresa till Japan (30% progress - 15000/50000)
        // Reached: 25%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 1,
            TargetAmount = 12500m,
            Percentage = 25,
            Description = "25% av målet uppnått",
            IsReached = true,
            ReachedAt = DateTime.UtcNow.AddMonths(-2),
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        });
        // Not reached: 50%, 75%, 100%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 1,
            TargetAmount = 25000m,
            Percentage = 50,
            Description = "50% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 1,
            TargetAmount = 37500m,
            Percentage = 75,
            Description = "75% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 1,
            TargetAmount = 50000m,
            Percentage = 100,
            Description = "100% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        });

        // Milestones for Goal 2: Ny laptop (34% progress - 8500/25000)
        // Reached: 25%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 2,
            TargetAmount = 6250m,
            Percentage = 25,
            Description = "25% av målet uppnått",
            IsReached = true,
            ReachedAt = DateTime.UtcNow.AddMonths(-1),
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        });
        // Not reached: 50%, 75%, 100%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 2,
            TargetAmount = 12500m,
            Percentage = 50,
            Description = "50% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 2,
            TargetAmount = 18750m,
            Percentage = 75,
            Description = "75% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 2,
            TargetAmount = 25000m,
            Percentage = 100,
            Description = "100% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        });

        // Milestones for Goal 3: Nödfond (50% progress - 45000/90000)
        // Reached: 25%, 50%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 3,
            TargetAmount = 22500m,
            Percentage = 25,
            Description = "25% av målet uppnått",
            IsReached = true,
            ReachedAt = DateTime.UtcNow.AddMonths(-4),
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-8)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 3,
            TargetAmount = 45000m,
            Percentage = 50,
            Description = "50% av målet uppnått",
            IsReached = true,
            ReachedAt = DateTime.UtcNow.AddDays(-10),
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-8)
        });
        // Not reached: 75%, 100%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 3,
            TargetAmount = 67500m,
            Percentage = 75,
            Description = "75% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-8)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 3,
            TargetAmount = 90000m,
            Percentage = 100,
            Description = "100% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-8)
        });

        // Milestones for Goal 4: Kontantinsats lägenhet (40% progress - 120000/300000)
        // Reached: 25%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 4,
            TargetAmount = 75000m,
            Percentage = 25,
            Description = "25% av målet uppnått",
            IsReached = true,
            ReachedAt = DateTime.UtcNow.AddMonths(-3),
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-10)
        });
        // Not reached: 50%, 75%, 100%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 4,
            TargetAmount = 150000m,
            Percentage = 50,
            Description = "50% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-10)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 4,
            TargetAmount = 225000m,
            Percentage = 75,
            Description = "75% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-10)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 4,
            TargetAmount = 300000m,
            Percentage = 100,
            Description = "100% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-10)
        });

        // Milestones for Goal 5: Ny cykel (80% progress - 12000/15000)
        // Reached: 25%, 50%, 75%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 5,
            TargetAmount = 3750m,
            Percentage = 25,
            Description = "25% av målet uppnått",
            IsReached = true,
            ReachedAt = DateTime.UtcNow.AddMonths(-2),
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 5,
            TargetAmount = 7500m,
            Percentage = 50,
            Description = "50% av målet uppnått",
            IsReached = true,
            ReachedAt = DateTime.UtcNow.AddMonths(-1),
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        });
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 5,
            TargetAmount = 11250m,
            Percentage = 75,
            Description = "75% av målet uppnått",
            IsReached = true,
            ReachedAt = DateTime.UtcNow.AddDays(-15),
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        });
        // Not reached: 100%
        milestones.Add(new GoalMilestone
        {
            GoalMilestoneId = milestoneId++,
            GoalId = 5,
            TargetAmount = 15000m,
            Percentage = 100,
            Description = "100% av målet uppnått",
            IsReached = false,
            IsAutomatic = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        });

        context.GoalMilestones.AddRange(milestones);
        context.SaveChanges();
    }

    private static void SeedAssets(PrivatekonomyContext context, string userId)
    {
        var assets = new List<Asset>
        {
            new Asset
            {
                AssetId = 1,
                Name = "Bostadsrätt Södermalm",
                Type = "Fastighet",
                Description = "2:a på Södermalm, Stockholm",
                PurchaseValue = 2800000m,
                CurrentValue = 3200000m,
                PurchaseDate = DateTime.UtcNow.AddYears(-5),
                Location = "Stockholm, Södermalm",
                Currency = "SEK",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Asset
            {
                AssetId = 2,
                Name = "Volvo V60",
                Type = "Fordon",
                Description = "2020 års modell, diesel",
                PurchaseValue = 285000m,
                CurrentValue = 240000m,
                PurchaseDate = DateTime.UtcNow.AddYears(-3),
                Location = "Stockholm",
                Currency = "SEK",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Asset
            {
                AssetId = 3,
                Name = "MacBook Pro",
                Type = "Elektronik",
                Description = "MacBook Pro 16\" M3 Pro",
                PurchaseValue = 32000m,
                CurrentValue = 28000m,
                PurchaseDate = DateTime.UtcNow.AddMonths(-6),
                Location = "Hemkontor",
                Currency = "SEK",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Asset
            {
                AssetId = 4,
                Name = "IKEA Möbler",
                Type = "Möbler",
                Description = "Soffa, matbord, och bokhyllor",
                PurchaseValue = 45000m,
                CurrentValue = 25000m,
                PurchaseDate = DateTime.UtcNow.AddYears(-2),
                Location = "Lägenheten",
                Currency = "SEK",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Asset
            {
                AssetId = 5,
                Name = "Samsung TV",
                Type = "Elektronik",
                Description = "55\" 4K QLED TV",
                PurchaseValue = 15000m,
                CurrentValue = 10000m,
                PurchaseDate = DateTime.UtcNow.AddYears(-1),
                Location = "Vardagsrum",
                Currency = "SEK",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Asset
            {
                AssetId = 6,
                Name = "Rolex klocka",
                Type = "Övrigt",
                Description = "Vintage Submariner",
                PurchaseValue = 85000m,
                CurrentValue = 120000m,
                PurchaseDate = DateTime.UtcNow.AddYears(-10),
                Location = "Bankfack",
                Currency = "SEK",
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            }
        };

        context.Assets.AddRange(assets);
        context.SaveChanges();
    }

    private static void SeedLoans(PrivatekonomyContext context, string userId)
    {
        var loans = new List<Loan>
        {
            new Loan
            {
                LoanId = 1,
                Name = "Bolån bostadsrätt",
                Type = "Bolån",
                Amount = 2100000m,
                InterestRate = 4.5m,
                Amortization = 5000m,
                Currency = "SEK",
                StartDate = DateTime.UtcNow.AddYears(-5),
                MaturityDate = DateTime.UtcNow.AddYears(25),
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Loan
            {
                LoanId = 2,
                Name = "Billån Volvo",
                Type = "Privatlån",
                Amount = 150000m,
                InterestRate = 5.9m,
                Amortization = 3500m,
                Currency = "SEK",
                StartDate = DateTime.UtcNow.AddYears(-3),
                MaturityDate = DateTime.UtcNow.AddYears(2),
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Loan
            {
                LoanId = 3,
                Name = "CSN Studielån",
                Type = "CSN-lån",
                Amount = 180000m,
                InterestRate = 1.5m,
                Amortization = 1200m,
                Currency = "SEK",
                StartDate = DateTime.UtcNow.AddYears(-8),
                MaturityDate = DateTime.UtcNow.AddYears(17),
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            }
        };

        context.Loans.AddRange(loans);
        context.SaveChanges();
    }

    private static void SeedSalaryHistory(PrivatekonomyContext context, string userId)
    {
        // Create realistic salary history showing career progression over 5 years
        var salaryHistories = new List<SalaryHistory>
        {
            // Year 1 - Junior Developer
            new SalaryHistory
            {
                SalaryHistoryId = 1,
                MonthlySalary = 28000m,
                Period = new DateTime(2020, 1, 1),
                JobTitle = "Junior Utvecklare",
                Employer = "Tech Startup AB",
                EmploymentType = "Heltid",
                WorkPercentage = 100,
                Notes = "Första jobbet efter examen",
                Currency = "SEK",
                IsCurrent = false,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            },
            // Year 2 - Salary revision
            new SalaryHistory
            {
                SalaryHistoryId = 2,
                MonthlySalary = 30000m,
                Period = new DateTime(2021, 1, 1),
                JobTitle = "Junior Utvecklare",
                Employer = "Tech Startup AB",
                EmploymentType = "Heltid",
                WorkPercentage = 100,
                Notes = "Årlig lönerevision",
                Currency = "SEK",
                IsCurrent = false,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            },
            // Year 3 - Promotion
            new SalaryHistory
            {
                SalaryHistoryId = 3,
                MonthlySalary = 35000m,
                Period = new DateTime(2022, 7, 1),
                JobTitle = "Utvecklare",
                Employer = "Tech Startup AB",
                EmploymentType = "Heltid",
                WorkPercentage = 100,
                Notes = "Befordran till Utvecklare",
                Currency = "SEK",
                IsCurrent = false,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            },
            // Year 4 - New job
            new SalaryHistory
            {
                SalaryHistoryId = 4,
                MonthlySalary = 42000m,
                Period = new DateTime(2023, 3, 1),
                JobTitle = "Senior Utvecklare",
                Employer = "Innovation Tech Group",
                EmploymentType = "Heltid",
                WorkPercentage = 100,
                Notes = "Nytt jobb med högre lön",
                Currency = "SEK",
                IsCurrent = false,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            },
            // Year 4.5 - Salary revision
            new SalaryHistory
            {
                SalaryHistoryId = 5,
                MonthlySalary = 44000m,
                Period = new DateTime(2024, 1, 1),
                JobTitle = "Senior Utvecklare",
                Employer = "Innovation Tech Group",
                EmploymentType = "Heltid",
                WorkPercentage = 100,
                Notes = "Årlig lönerevision",
                Currency = "SEK",
                IsCurrent = false,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            },
            // Year 5 - Current position
            new SalaryHistory
            {
                SalaryHistoryId = 6,
                MonthlySalary = 48000m,
                Period = new DateTime(2025, 1, 1),
                JobTitle = "Senior Utvecklare",
                Employer = "Innovation Tech Group",
                EmploymentType = "Heltid",
                WorkPercentage = 100,
                Notes = "Lönerevision 2025",
                Currency = "SEK",
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            }
        };
        
        context.SalaryHistories.AddRange(salaryHistories);
        context.SaveChanges();
    }
  
    private static void SeedPockets(PrivatekonomyContext context, string userId)
    {
        // Create sample pockets on different savings accounts
        var pockets = new List<Pocket>
        {
            // Pockets on ICA-banken (BankSourceId = 1)
            new Pocket
            {
                PocketId = 1,
                Name = "Bilköp",
                Description = "Spara till ny bil - siktar på en elbil",
                TargetAmount = 450000m,
                CurrentAmount = 125000m,
                MonthlyAllocation = 25000m,
                BankSourceId = 1,
                Priority = 1,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Pocket
            {
                PocketId = 2,
                Name = "Teknikinköp",
                Description = "Ny laptop, telefon och surfplatta",
                TargetAmount = 35000m,
                CurrentAmount = 28500m,
                MonthlyAllocation = 5000m,
                BankSourceId = 1,
                Priority = 2,
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Pocket
            {
                PocketId = 3,
                Name = "Resor",
                Description = "Sommarlov i Italien och vinterresa till Japan (kan spara extra för lyxresa)",
                TargetAmount = 75000m,
                CurrentAmount = 42000m,
                MonthlyAllocation = 8000m,
                BankSourceId = 1,
                Priority = 2,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },

            // Pockets on Swedbank (BankSourceId = 2)
            new Pocket
            {
                PocketId = 4,
                Name = "Renovering",
                Description = "Renovera kök och badrum",
                TargetAmount = 250000m,
                CurrentAmount = 85000m,
                MonthlyAllocation = 15000m,
                BankSourceId = 2,
                Priority = 1,
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Pocket
            {
                PocketId = 5,
                Name = "Trädgård",
                Description = "Altandäck, utemöbler och plantering",
                TargetAmount = 60000m,
                CurrentAmount = 15000m,
                MonthlyAllocation = 3000m,
                BankSourceId = 2,
                Priority = 3,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Pocket
            {
                PocketId = 6,
                Name = "Kläder",
                Description = "Vår- och höstgarderob",
                TargetAmount = 15000m,
                CurrentAmount = 12500m,
                BankSourceId = 2,
                Priority = 4,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            },
            new Pocket
            {
                PocketId = 7,
                Name = "Mat & Uppläggning",
                Description = "Extra buffert för matinköp och storkok",
                TargetAmount = 8000m,
                CurrentAmount = 8000m,
                BankSourceId = 2,
                Priority = 2,
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UserId = userId,
                ValidFrom = DateTime.UtcNow,
                ValidTo = null
            }
        };

        // Create some pocket transactions to show history
        var pocketTransactions = new List<PocketTransaction>
        {
            // Transactions for "Bilköp" pocket
            new PocketTransaction
            {
                PocketTransactionId = 1,
                PocketId = 1,
                Amount = 50000m,
                Type = "Deposit",
                Description = "Initial insättning från bonus",
                TransactionDate = DateTime.UtcNow.AddMonths(-6),
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UserId = userId
            },
            new PocketTransaction
            {
                PocketTransactionId = 2,
                PocketId = 1,
                Amount = 25000m,
                Type = "Deposit",
                Description = "Månadssparande",
                TransactionDate = DateTime.UtcNow.AddMonths(-5),
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UserId = userId
            },
            new PocketTransaction
            {
                PocketTransactionId = 3,
                PocketId = 1,
                Amount = 25000m,
                Type = "Deposit",
                Description = "Månadssparande",
                TransactionDate = DateTime.UtcNow.AddMonths(-4),
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UserId = userId
            },
            new PocketTransaction
            {
                PocketTransactionId = 4,
                PocketId = 1,
                Amount = 25000m,
                Type = "Deposit",
                Description = "Månadssparande",
                TransactionDate = DateTime.UtcNow.AddMonths(-3),
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UserId = userId
            },

            // Transactions for "Teknikinköp" pocket
            new PocketTransaction
            {
                PocketTransactionId = 5,
                PocketId = 2,
                Amount = 15000m,
                Type = "Deposit",
                Description = "Startkapital",
                TransactionDate = DateTime.UtcNow.AddMonths(-4),
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UserId = userId
            },
            new PocketTransaction
            {
                PocketTransactionId = 6,
                PocketId = 2,
                Amount = 6500m,
                Type = "Deposit",
                Description = "Extra sparande",
                TransactionDate = DateTime.UtcNow.AddMonths(-3),
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UserId = userId
            },
            new PocketTransaction
            {
                PocketTransactionId = 7,
                PocketId = 2,
                Amount = 7000m,
                Type = "Deposit",
                Description = "Sparande från extrainkomst",
                TransactionDate = DateTime.UtcNow.AddMonths(-1),
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UserId = userId
            },

            // Transactions for "Resor" pocket
            new PocketTransaction
            {
                PocketTransactionId = 8,
                PocketId = 3,
                Amount = 30000m,
                Type = "Deposit",
                Description = "Första insättningen för resefond",
                TransactionDate = DateTime.UtcNow.AddMonths(-3),
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UserId = userId
            },
            new PocketTransaction
            {
                PocketTransactionId = 9,
                PocketId = 3,
                Amount = 12000m,
                Type = "Deposit",
                Description = "Månadssparande",
                TransactionDate = DateTime.UtcNow.AddMonths(-2),
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UserId = userId
            },

            // Transactions for "Renovering" pocket
            new PocketTransaction
            {
                PocketTransactionId = 10,
                PocketId = 4,
                Amount = 40000m,
                Type = "Deposit",
                Description = "Initial insättning för renovering",
                TransactionDate = DateTime.UtcNow.AddMonths(-8),
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                UserId = userId
            },
            new PocketTransaction
            {
                PocketTransactionId = 11,
                PocketId = 4,
                Amount = 15000m,
                Type = "Deposit",
                Description = "Månadssparande",
                TransactionDate = DateTime.UtcNow.AddMonths(-6),
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UserId = userId
            },
            new PocketTransaction
            {
                PocketTransactionId = 12,
                PocketId = 4,
                Amount = 30000m,
                Type = "Deposit",
                Description = "Extra sparande från försäljning",
                TransactionDate = DateTime.UtcNow.AddMonths(-4),
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UserId = userId
            },

            // Transaction for "Kläder" pocket (fully funded)
            new PocketTransaction
            {
                PocketTransactionId = 13,
                PocketId = 6,
                Amount = 12500m,
                Type = "Deposit",
                Description = "Klädbudget för året",
                TransactionDate = DateTime.UtcNow.AddMonths(-1),
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UserId = userId
            },

            // Transaction for "Mat & Uppläggning" pocket (fully funded)
            new PocketTransaction
            {
                PocketTransactionId = 14,
                PocketId = 7,
                Amount = 8000m,
                Type = "Deposit",
                Description = "Matbuffert för månad",
                TransactionDate = DateTime.UtcNow.AddMonths(-5),
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UserId = userId
            }
        };

        context.Pockets.AddRange(pockets);
        context.PocketTransactions.AddRange(pocketTransactions);
        context.SaveChanges();
    }

    private static void SeedNetWorthSnapshots(PrivatekonomyContext context, string userId)
    {
        // Create historical net worth snapshots for the last 24 months
        var snapshots = new List<NetWorthSnapshot>();
        var random = new Random(42); // Fixed seed for consistent test data
        
        // Starting values
        decimal bankBalance = 50000m;
        decimal investmentValue = 450000m;
        decimal physicalAssetValue = 150000m;
        decimal loanBalance = 500000m;
        
        // Generate monthly snapshots for the last 24 months
        for (int i = 24; i >= 0; i--)
        {
            var date = DateTime.Today.AddMonths(-i);
            
            // Simulate realistic growth/changes
            // Net worth typically grows over time with some volatility
            var monthlyChange = (decimal)(random.NextDouble() * 0.04 - 0.01); // -1% to +3% monthly change
            
            // Update values
            bankBalance += (decimal)(random.Next(-5000, 8000)); // Random cash flow
            bankBalance = Math.Max(10000m, bankBalance); // Keep minimum balance
            
            investmentValue *= (1 + monthlyChange); // Market volatility
            
            physicalAssetValue -= 200m; // Slow depreciation
            
            loanBalance -= 2000m; // Amortization
            loanBalance = Math.Max(300000m, loanBalance); // Don't go below a certain amount
            
            var totalAssets = bankBalance + investmentValue + physicalAssetValue;
            var netWorth = totalAssets - loanBalance;
            
            snapshots.Add(new NetWorthSnapshot
            {
                Date = date,
                TotalAssets = totalAssets,
                TotalLiabilities = loanBalance,
                NetWorth = netWorth,
                BankBalance = bankBalance,
                InvestmentValue = investmentValue,
                PhysicalAssetValue = physicalAssetValue,
                LoanBalance = loanBalance,
                IsManual = false,
                Notes = $"Automatisk snapshot för {date:yyyy-MM-dd}",
                CreatedAt = date,
                UserId = userId
            });
        }
        
        context.NetWorthSnapshots.AddRange(snapshots);
        context.SaveChanges();
    }

    private static void SeedChallengeTemplates(PrivatekonomyContext context)
    {
        // Only seed if no templates exist
        if (context.ChallengeTemplates.Any())
        {
            return;
        }

        var templates = new List<ChallengeTemplate>
        {
            // Short-term challenges (1-4 weeks)
            new ChallengeTemplate
            {
                Name = "No Spend Weekend",
                Description = "Ingen shopping eller icke-nödvändiga utgifter under en helg. Mat som redan finns hemma, gratis aktiviteter, och kvalitetstid utan konsumtion.",
                Icon = "🛍️",
                Type = ChallengeType.NoSpendWeekend,
                DurationDays = 2,
                Difficulty = DifficultyLevel.Easy,
                Category = ChallengeCategory.Individual,
                EstimatedSavingsMin = 500,
                EstimatedSavingsMax = 2000,
                SuggestedTargetAmount = 1000,
                Tags = new List<string> { "kortsiktig", "helg", "minimalism" },
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Matlåda varje dag",
                Description = "Ta med egen lunch till jobbet/skolan varje dag i 2 veckor. Ingen lunch ute, ingen lunchmeny, ingen foodcourt.",
                Icon = "🍱",
                Type = ChallengeType.LunchBox,
                DurationDays = 14,
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Health,
                EstimatedSavingsMin = 1000,
                EstimatedSavingsMax = 1500,
                SuggestedTargetAmount = 1250,
                Tags = new List<string> { "kortsiktig", "hälsa", "mat" },
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Endast cykel/kollektivtrafik",
                Description = "Ingen bilkörning eller taxiresor i 2 veckor. Använd endast cykel, promenader eller kollektivtrafik.",
                Icon = "🚴",
                Type = ChallengeType.BikeOrPublic,
                DurationDays = 14,
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Environment,
                EstimatedSavingsMin = 500,
                EstimatedSavingsMax = 2000,
                SuggestedTargetAmount = 1000,
                Tags = new List<string> { "kortsiktig", "miljö", "hälsa", "transport" },
                SortOrder = 3,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Sälja 5 saker på 30 dagar",
                Description = "Rensa ut hemma och sälja minst 5 saker på Blocket, Facebook Marketplace, Tradera eller liknande under en månad.",
                Icon = "📦",
                Type = ChallengeType.SellItems,
                DurationDays = 30,
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Minimalism,
                EstimatedSavingsMin = 500,
                EstimatedSavingsMax = 5000,
                SuggestedTargetAmount = 2000,
                Tags = new List<string> { "kortsiktig", "minimalism", "försäljning" },
                SortOrder = 4,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Växelpengsburken",
                Description = "Spara alla växelpengar (mynt) i en burk varje dag i en månad. Vid månadens slut, räkna och överför till sparkonto.",
                Icon = "🪙",
                Type = ChallengeType.ChangeJar,
                DurationDays = 30,
                Difficulty = DifficultyLevel.VeryEasy,
                Category = ChallengeCategory.Individual,
                EstimatedSavingsMin = 200,
                EstimatedSavingsMax = 800,
                SuggestedTargetAmount = 500,
                Tags = new List<string> { "kortsiktig", "enkelt", "dagligt" },
                SortOrder = 5,
                CreatedAt = DateTime.UtcNow
            },

            // Medium-term challenges (1-3 months)
            new ChallengeTemplate
            {
                Name = "Noll spontanhandel",
                Description = "Endast planerade inköp under en månad. Allt som inte står på inköpslistan är förbjudet. Handlingsplan krävs före varje shoppingrunda.",
                Icon = "🛒",
                Type = ChallengeType.NoImpulseBuying,
                DurationDays = 30,
                Difficulty = DifficultyLevel.Hard,
                Category = ChallengeCategory.Minimalism,
                EstimatedSavingsMin = 1000,
                EstimatedSavingsMax = 3000,
                SuggestedTargetAmount = 2000,
                Tags = new List<string> { "medellång", "minimalism", "planering" },
                SortOrder = 6,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Strömnings-detox",
                Description = "Pausa alla betalda strömningsabonnemang (Netflix, HBO, Spotify Premium, etc.) i en månad. Använd gratisalternativ eller bibliotek.",
                Icon = "📺",
                Type = ChallengeType.StreamingDetox,
                DurationDays = 30,
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Minimalism,
                EstimatedSavingsMin = 200,
                EstimatedSavingsMax = 800,
                SuggestedTargetAmount = 400,
                Tags = new List<string> { "medellång", "abonnemang", "digital" },
                SortOrder = 7,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Alkoholfri månad",
                Description = "Ingen alkohol på en månad - varken hemma eller ute. Inspirerad av 'Dry January' eller 'Sober October'.",
                Icon = "🍷",
                Type = ChallengeType.AlcoholFree,
                DurationDays = 30,
                Difficulty = DifficultyLevel.Hard,
                Category = ChallengeCategory.Health,
                EstimatedSavingsMin = 1000,
                EstimatedSavingsMax = 5000,
                SuggestedTargetAmount = 2500,
                Tags = new List<string> { "medellång", "hälsa", "livsstil" },
                SortOrder = 8,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Gåvofri period (utom födelsedag)",
                Description = "Inga presenter till vuxna (exkl. faktiska födelsedagar och högtider). Fokus på upplevelser och tid istället för materiella ting.",
                Icon = "🎁",
                Type = ChallengeType.GiftFree,
                DurationDays = 60,
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Minimalism,
                EstimatedSavingsMin = 500,
                EstimatedSavingsMax = 2000,
                SuggestedTargetAmount = 1000,
                Tags = new List<string> { "medellång", "minimalism", "relationer" },
                SortOrder = 9,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Hemma-gymmet",
                Description = "Pausa gym-medlemskap och träna hemma eller utomhus istället i 3 månader.",
                Icon = "🏋️",
                Type = ChallengeType.HomeGym,
                DurationDays = 90,
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Health,
                EstimatedSavingsMin = 1500,
                EstimatedSavingsMax = 3000,
                SuggestedTargetAmount = 2000,
                Tags = new List<string> { "medellång", "hälsa", "träning" },
                SortOrder = 10,
                CreatedAt = DateTime.UtcNow
            },

            // Long-term challenges (3-6 months)
            new ChallengeTemplate
            {
                Name = "Spara för ett specifikt mål",
                Description = "Sätt upp ett konkret sparmål (t.ex. resa, ny dator, möbel) och spara systematiskt med delmål och progress-tracking.",
                Icon = "💰",
                Type = ChallengeType.SpecificGoal,
                DurationDays = 90,
                Difficulty = DifficultyLevel.Hard,
                Category = ChallengeCategory.GoalBased,
                EstimatedSavingsMin = 5000,
                EstimatedSavingsMax = 50000,
                SuggestedTargetAmount = 10000,
                Tags = new List<string> { "långsiktig", "målbaserad", "sparande" },
                SortOrder = 11,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Hushålls-challenge: Gemensamt sparmål",
                Description = "Hela hushållet sparar tillsammans mot ett gemensamt mål (semester, renovering, ny bil). Alla bidrar enligt sina förutsättningar.",
                Icon = "🏠",
                Type = ChallengeType.HouseholdGoal,
                DurationDays = 90,
                Difficulty = DifficultyLevel.Hard,
                Category = ChallengeCategory.Household,
                EstimatedSavingsMin = 10000,
                EstimatedSavingsMax = 100000,
                SuggestedTargetAmount = 30000,
                Tags = new List<string> { "långsiktig", "hushåll", "social", "målbaserad" },
                SortOrder = 12,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Klimatsmart-utmaning",
                Description = "Kombinerad utmaning för att minska konsumtion och klimatpåverkan samtidigt som man sparar pengar. Fokus på: ingen onlineshopping, lokala/begagnade köp, mindre kött, cykel istället för bil.",
                Icon = "🌍",
                Type = ChallengeType.ClimateChallenge,
                DurationDays = 90,
                Difficulty = DifficultyLevel.Hard,
                Category = ChallengeCategory.Environment,
                EstimatedSavingsMin = 2000,
                EstimatedSavingsMax = 6000,
                SuggestedTargetAmount = 4000,
                Tags = new List<string> { "långsiktig", "miljö", "livsstil" },
                SortOrder = 13,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Spara X% mer varje månad",
                Description = "Börja med att spara 5% av inkomsten månad 1, sedan öka med 1-2% varje månad tills du når 10-15%. Progressiv sparutmaning.",
                Icon = "📈",
                Type = ChallengeType.ProgressiveSaving,
                DurationDays = 180,
                Difficulty = DifficultyLevel.VeryHard,
                Category = ChallengeCategory.GoalBased,
                EstimatedSavingsMin = 15000,
                EstimatedSavingsMax = 50000,
                SuggestedTargetAmount = 25000,
                Tags = new List<string> { "långsiktig", "progressiv", "procent", "inkomst" },
                SortOrder = 14,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Slump-spararen",
                Description = "Varje vecka får du en slumpmässig sparutmaning (spara 50 kr extra, ingen fika i 3 dagar, sälja något, etc.). Gamified och oförutsägbar!",
                Icon = "🎲",
                Type = ChallengeType.RandomChallenge,
                DurationDays = 90,
                Difficulty = DifficultyLevel.Easy,
                Category = ChallengeCategory.Fun,
                EstimatedSavingsMin = 1000,
                EstimatedSavingsMax = 3000,
                SuggestedTargetAmount = 2000,
                Tags = new List<string> { "långsiktig", "rolig", "överraskning", "varierad" },
                SortOrder = 15,
                CreatedAt = DateTime.UtcNow
            },

            // Social challenges
            new ChallengeTemplate
            {
                Name = "Spargruppen",
                Description = "Skapa eller gå med i en spargrupp med vänner. Gemensamt mål att alla sparar minst X kr eller X% av lön. Support och accountability.",
                Icon = "👥",
                Type = ChallengeType.SavingsGroup,
                DurationDays = 60,
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Social,
                EstimatedSavingsMin = 5000,
                EstimatedSavingsMax = 20000,
                SuggestedTargetAmount = 10000,
                Tags = new List<string> { "social", "grupp", "support" },
                SortOrder = 16,
                CreatedAt = DateTime.UtcNow
            },
            new ChallengeTemplate
            {
                Name = "Månadsutmaning med Leaderboard",
                Description = "Varje månad en ny tävling: vem kan spara mest? Vem kan minska utgifterna mest? Vem har bäst streak? Poäng och ranking.",
                Icon = "🥇",
                Type = ChallengeType.Leaderboard,
                DurationDays = 30,
                Difficulty = DifficultyLevel.Hard,
                Category = ChallengeCategory.Social,
                EstimatedSavingsMin = 2000,
                EstimatedSavingsMax = 10000,
                SuggestedTargetAmount = 5000,
                Tags = new List<string> { "social", "tävling", "leaderboard", "månatlig" },
                SortOrder = 17,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.ChallengeTemplates.AddRange(templates);
        context.SaveChanges();
    }

    private static void SeedSubscriptions(PrivatekonomyContext context, string userId)
    {
        var subscriptions = new List<Subscription>
        {
            new Subscription
            {
                SubscriptionId = 1,
                Name = "Spotify Premium Family",
                Description = "Musikstreaming för hela familjen",
                Price = 179m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                NextBillingDate = DateTime.UtcNow.AddDays(15),
                StartDate = DateTime.UtcNow.AddYears(-2),
                IsActive = true,
                CategoryId = 20, // Streaming
                ManagementUrl = "https://www.spotify.com/account",
                AccountEmail = "test@example.com",
                LastUsedDate = DateTime.UtcNow.AddDays(-1),
                SharedWith = "Partner, Barn",
                Notes = "Familjeplan med 6 konton",
                CancellationNoticeDays = 30,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new Subscription
            {
                SubscriptionId = 2,
                Name = "Netflix Standard",
                Description = "Filmer och serier i HD",
                Price = 139m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                NextBillingDate = DateTime.UtcNow.AddDays(5),
                StartDate = DateTime.UtcNow.AddYears(-3),
                IsActive = true,
                CategoryId = 20, // Streaming
                ManagementUrl = "https://www.netflix.com/YourAccount",
                CancellationUrl = "https://www.netflix.com/cancelplan",
                AccountEmail = "test@example.com",
                LastUsedDate = DateTime.UtcNow.AddDays(-2),
                SharedWith = "Partner",
                Notes = "Standard HD-plan",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Subscription
            {
                SubscriptionId = 3,
                Name = "Fitness24Seven",
                Description = "Gymmedlemskap",
                Price = 299m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                NextBillingDate = DateTime.UtcNow.AddDays(20),
                StartDate = DateTime.UtcNow.AddMonths(-6),
                IsActive = true,
                CategoryId = 21, // Gym
                ManagementUrl = "https://www.24-7.se/mina-sidor",
                LastUsedDate = DateTime.UtcNow.AddDays(-3),
                CancellationNoticeDays = 30,
                Notes = "Medlemskap på hemmastudion",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Subscription
            {
                SubscriptionId = 4,
                Name = "HBO Max",
                Description = "Streaming av HBO-serier och filmer",
                Price = 109m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                NextBillingDate = DateTime.UtcNow.AddDays(10),
                StartDate = DateTime.UtcNow.AddMonths(-8),
                IsActive = true,
                CategoryId = 20, // Streaming
                AccountEmail = "test@example.com",
                LastUsedDate = DateTime.UtcNow.AddDays(-7),
                Notes = "För HBO-serier och dokumentärer",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Subscription
            {
                SubscriptionId = 5,
                Name = "Adobe Creative Cloud",
                Description = "Photoshop, Illustrator och andra kreativa verktyg",
                Price = 659m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                NextBillingDate = DateTime.UtcNow.AddDays(8),
                StartDate = DateTime.UtcNow.AddYears(-1),
                IsActive = true,
                CategoryId = 25, // Elektronik
                AccountEmail = "test@example.com",
                LastUsedDate = DateTime.UtcNow.AddDays(-1),
                Notes = "Behövs för jobbprojekt och hobby",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Subscription
            {
                SubscriptionId = 6,
                Name = "Aftonbladet Plus",
                Description = "Nyheter utan annonser",
                Price = 49m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                NextBillingDate = DateTime.UtcNow.AddDays(12),
                StartDate = DateTime.UtcNow.AddMonths(-4),
                IsActive = true,
                CategoryId = 4, // Nöje
                LastUsedDate = DateTime.UtcNow,
                Notes = "Läser varje morgon",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Subscription
            {
                SubscriptionId = 7,
                Name = "iCloud+ 200GB",
                Description = "Molnlagring för Apple-enheter",
                Price = 29m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                NextBillingDate = DateTime.UtcNow.AddDays(7),
                StartDate = DateTime.UtcNow.AddYears(-2),
                IsActive = true,
                CategoryId = 5, // Shopping
                LastUsedDate = DateTime.UtcNow,
                Notes = "Backup för iPhone och iPad",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Subscriptions.AddRange(subscriptions);
        context.SaveChanges();
    }

    private static void SeedBills(PrivatekonomyContext context, string userId)
    {
        var bills = new List<Bill>
        {
            new Bill
            {
                BillId = 1,
                Name = "Elräkning",
                Description = "Månatlig elförbrukning",
                Amount = 1450m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-20),
                DueDate = DateTime.UtcNow.AddDays(10),
                Status = "Pending",
                IsRecurring = true,
                RecurringFrequency = "Monthly",
                PaymentMethod = "Autogiro",
                InvoiceNumber = "EL-2025-001234",
                Payee = "Vattenfall AB",
                CategoryId = 17, // El
                Notes = "Högre förbrukning än vanligt, vinter",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Bill
            {
                BillId = 2,
                Name = "Hemförsäkring",
                Description = "Försäkring för lägenhet och lösöre",
                Amount = 349m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-25),
                DueDate = DateTime.UtcNow.AddDays(5),
                Status = "Pending",
                IsRecurring = true,
                RecurringFrequency = "Monthly",
                PaymentMethod = "Autogiro",
                InvoiceNumber = "IF-2025-004567",
                Payee = "IF Skadeförsäkring",
                CategoryId = 19, // Hemförsäkring
                Notes = "Automatisk betalning",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Bill
            {
                BillId = 3,
                Name = "Tandvårdsräkning",
                Description = "Tandläkarkontroll och lagning",
                Amount = 2800m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(25),
                Status = "Pending",
                IsRecurring = false,
                InvoiceNumber = "TV-2025-00789",
                OCR = "123456789012345",
                Bankgiro = "123-4567",
                Payee = "Folktandvården Stockholm",
                CategoryId = 26, // Tandvård
                Notes = "Årlig kontroll plus lagning av en tand",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Bill
            {
                BillId = 4,
                Name = "Mobilabonnemang",
                Description = "Telia Mobil 50 GB",
                Amount = 349m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-15),
                DueDate = DateTime.UtcNow.AddDays(-5),
                PaidDate = DateTime.UtcNow.AddDays(-3),
                Status = "Paid",
                IsRecurring = true,
                RecurringFrequency = "Monthly",
                PaymentMethod = "E-invoice",
                InvoiceNumber = "TEL-2024-112233",
                Payee = "Telia Sverige AB",
                CategoryId = 18, // Bredband
                Notes = "Betald i tid via e-faktura",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Bill
            {
                BillId = 5,
                Name = "Bilförsäkring",
                Description = "Helförsäkring Volvo V60",
                Amount = 580m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-30),
                DueDate = DateTime.UtcNow.AddDays(-10),
                PaidDate = DateTime.UtcNow.AddDays(-8),
                Status = "Paid",
                IsRecurring = true,
                RecurringFrequency = "Monthly",
                PaymentMethod = "Autogiro",
                InvoiceNumber = "BF-2024-445566",
                Payee = "Länsförsäkringar",
                CategoryId = 2, // Transport
                Notes = "Autogiro dras den 20:e varje månad",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow.AddDays(-8)
            },
            // New bills for reminder testing
            new Bill
            {
                BillId = 6,
                Name = "Hyra",
                Description = "Månadshyra lägenhet",
                Amount = 8500m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-25),
                DueDate = DateTime.UtcNow.AddDays(2),
                Status = "Pending",
                IsRecurring = true,
                RecurringFrequency = "Monthly",
                PaymentMethod = "Bankgiro",
                InvoiceNumber = "HYRA-2025-11",
                Bankgiro = "555-8888",
                Payee = "AB Bostäder Stockholm",
                CategoryId = 3, // Boende
                Notes = "Hyra förfaller den 1:a varje månad",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Bill
            {
                BillId = 7,
                Name = "Netflix Premium",
                Description = "Streaming-tjänst",
                Amount = 139m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(10),
                Status = "Pending",
                IsRecurring = true,
                RecurringFrequency = "Monthly",
                PaymentMethod = "Credit Card",
                InvoiceNumber = "NFLX-2025-11-001",
                Payee = "Netflix AB",
                CategoryId = 15, // Nöje
                Notes = "Automatisk kortbetalning",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Bill
            {
                BillId = 8,
                Name = "Interneträkning",
                Description = "Bredband 1000/1000 Mbit",
                Amount = 399m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-12),
                DueDate = DateTime.UtcNow.AddDays(-8),
                Status = "Overdue",
                IsRecurring = true,
                RecurringFrequency = "Monthly",
                PaymentMethod = "E-invoice",
                InvoiceNumber = "INET-2025-10-789",
                OCR = "987654321098765",
                Payee = "Bahnhof AB",
                CategoryId = 18, // Bredband
                Notes = "FÖRSENAD! Betala omedelbart för att undvika avbrott",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Bill
            {
                BillId = 9,
                Name = "Spotify Family",
                Description = "Musikstreaming familjeplan",
                Amount = 179m,
                Currency = "SEK",
                IssueDate = DateTime.UtcNow.AddDays(-3),
                DueDate = DateTime.UtcNow.AddDays(12),
                Status = "Pending",
                IsRecurring = true,
                RecurringFrequency = "Monthly",
                PaymentMethod = "Credit Card",
                InvoiceNumber = "SPOT-2025-11-456",
                Payee = "Spotify AB",
                CategoryId = 15, // Nöje
                Notes = "Familjeplan för 6 användare",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Bills.AddRange(bills);
        context.SaveChanges();
    }

    private static void SeedBillReminders(PrivatekonomyContext context, string userId)
    {
        var billReminders = new List<BillReminder>
        {
            // Reminder 1: Normal reminder for electricity bill (Bill 1)
            new BillReminder
            {
                BillReminderId = 1,
                BillId = 1,
                ReminderDate = DateTime.UtcNow.AddDays(-3),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddDays(-3),
                ReminderMethod = "Notification",
                Message = "Elräkning på 1,450 kr förfaller om 13 dagar",
                SnoozeUntil = null,
                SnoozeCount = 0,
                IsCompleted = false,
                EscalationLevel = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            
            // Reminder 2: Snoozed once - Rent bill (Bill 6)
            new BillReminder
            {
                BillReminderId = 2,
                BillId = 6,
                ReminderDate = DateTime.UtcNow.AddDays(-5),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddDays(-5),
                ReminderMethod = "Notification",
                Message = "Hyra på 8,500 kr förfaller om 7 dagar",
                SnoozeUntil = DateTime.UtcNow.AddDays(1),
                SnoozeCount = 1,
                IsCompleted = false,
                EscalationLevel = 0,
                LastFollowUpDate = null,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            
            // Reminder 3: Snoozed multiple times (2 times) - Netflix (Bill 7)
            new BillReminder
            {
                BillReminderId = 3,
                BillId = 7,
                ReminderDate = DateTime.UtcNow.AddDays(-10),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddDays(-10),
                ReminderMethod = "Notification",
                Message = "Netflix Premium på 139 kr förfaller snart",
                SnoozeUntil = DateTime.UtcNow.AddHours(6),
                SnoozeCount = 2,
                IsCompleted = false,
                EscalationLevel = 0,
                LastFollowUpDate = null,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            
            // Reminder 4: Critical - Overdue internet bill with escalation level 3 (Bill 8)
            new BillReminder
            {
                BillReminderId = 4,
                BillId = 8,
                ReminderDate = DateTime.UtcNow.AddDays(-15),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddDays(-15),
                ReminderMethod = "Notification",
                Message = "⚠️ BRÅDSKANDE: Interneträkning på 399 kr förföll för 8 dagar sedan. Åtgärd krävs omedelbart!",
                SnoozeUntil = null,
                SnoozeCount = 2,
                IsCompleted = false,
                EscalationLevel = 3,
                LastFollowUpDate = DateTime.UtcNow.AddHours(-12),
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            
            // Reminder 5: Escalation level 1 - 1 day overdue (Bill 1 - second reminder)
            new BillReminder
            {
                BillReminderId = 5,
                BillId = 1,
                ReminderDate = DateTime.UtcNow.AddDays(-1),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddDays(-1),
                ReminderMethod = "Notification",
                Message = "Påminnelse: Elräkning på 1,450 kr förfaller om 11 dagar",
                SnoozeUntil = null,
                SnoozeCount = 0,
                IsCompleted = false,
                EscalationLevel = 1,
                LastFollowUpDate = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            
            // Reminder 6: Escalation level 2 - 3 days, high priority (Bill 6 - second reminder)
            new BillReminder
            {
                BillReminderId = 6,
                BillId = 6,
                ReminderDate = DateTime.UtcNow.AddDays(-3),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddDays(-3),
                ReminderMethod = "Notification",
                Message = "⚠️ BRÅDSKANDE: Hyra på 8,500 kr förfaller om 5 dagar",
                SnoozeUntil = null,
                SnoozeCount = 0,
                IsCompleted = false,
                EscalationLevel = 2,
                LastFollowUpDate = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            
            // Reminder 7: Completed reminder (Bill 4 - paid mobile bill)
            new BillReminder
            {
                BillReminderId = 7,
                BillId = 4,
                ReminderDate = DateTime.UtcNow.AddDays(-8),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddDays(-8),
                ReminderMethod = "Notification",
                Message = "Mobilabonnemang på 349 kr förfaller snart",
                SnoozeUntil = null,
                SnoozeCount = 0,
                IsCompleted = true,
                CompletedDate = DateTime.UtcNow.AddDays(-3),
                EscalationLevel = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },
            
            // Reminder 8: Snoozed 3+ times - should trigger warning (Bill 9 - Spotify)
            new BillReminder
            {
                BillReminderId = 8,
                BillId = 9,
                ReminderDate = DateTime.UtcNow.AddDays(-12),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddDays(-12),
                ReminderMethod = "Notification",
                Message = "Spotify Family på 179 kr förfaller snart (Snoozad 3 gånger)",
                SnoozeUntil = DateTime.UtcNow.AddDays(2),
                SnoozeCount = 3,
                IsCompleted = false,
                EscalationLevel = 1,
                LastFollowUpDate = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            },
            
            // Reminder 9: Recently sent, not yet actioned (Bill 2 - Home insurance)
            new BillReminder
            {
                BillReminderId = 9,
                BillId = 2,
                ReminderDate = DateTime.UtcNow.AddHours(-6),
                IsSent = true,
                SentDate = DateTime.UtcNow.AddHours(-6),
                ReminderMethod = "Notification",
                Message = "Hemförsäkring på 349 kr förfaller om 5 dagar",
                SnoozeUntil = null,
                SnoozeCount = 0,
                IsCompleted = false,
                EscalationLevel = 0,
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            }
        };

        context.BillReminders.AddRange(billReminders);
        context.SaveChanges();
    }

    private static void SeedPensions(PrivatekonomyContext context, string userId)
    {
        var pensions = new List<Pension>
        {
            new Pension
            {
                PensionId = 1,
                Name = "Tjänstepension ITP",
                PensionType = "Tjänstepension",
                Provider = "Alecta",
                CurrentValue = 485000m,
                TotalContributions = 380000m,
                MonthlyContribution = 3500m,
                ExpectedMonthlyPension = 12500m,
                RetirementAge = 65,
                StartDate = DateTime.UtcNow.AddYears(-10),
                LastUpdated = DateTime.UtcNow.AddDays(-7),
                AccountNumber = "ITP-123456",
                Notes = "Premiebestämd tjänstepension via arbetsgivare",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Pension
            {
                PensionId = 2,
                Name = "Privat pensionssparande",
                PensionType = "Privat pension",
                Provider = "Avanza",
                CurrentValue = 245000m,
                TotalContributions = 200000m,
                MonthlyContribution = 2000m,
                ExpectedMonthlyPension = 5800m,
                RetirementAge = 65,
                StartDate = DateTime.UtcNow.AddYears(-7),
                LastUpdated = DateTime.UtcNow.AddDays(-2),
                AccountNumber = "PP-789012",
                Notes = "Eget pensionssparande med fondförsäkring",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new Pension
            {
                PensionId = 3,
                Name = "AP7 Såfa",
                PensionType = "Allmän pension (AP7)",
                Provider = "AP7",
                CurrentValue = 125000m,
                TotalContributions = 110000m,
                ExpectedMonthlyPension = 8500m,
                RetirementAge = 65,
                StartDate = DateTime.UtcNow.AddYears(-12),
                LastUpdated = DateTime.UtcNow.AddMonths(-1),
                Notes = "Premiepension via AP7 - statlig allmän pension",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Pensions.AddRange(pensions);
        context.SaveChanges();
    }

    private static void SeedDividends(PrivatekonomyContext context, string userId)
    {
        var dividends = new List<Dividend>
        {
            // Dividends from Volvo B (InvestmentId = 1)
            new Dividend
            {
                DividendId = 1,
                InvestmentId = 1,
                PaymentDate = DateTime.UtcNow.AddMonths(-2),
                ExDividendDate = DateTime.UtcNow.AddMonths(-2).AddDays(-5),
                AmountPerShare = 5.50m,
                TotalAmount = 550m, // 100 shares * 5.50
                SharesHeld = 100,
                Currency = "SEK",
                TaxWithheld = 165m, // 30% tax
                IsReinvested = false,
                Notes = "Ordinarie utdelning Q4 2024",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            // Dividend from SEB A (InvestmentId = 2)
            new Dividend
            {
                DividendId = 2,
                InvestmentId = 2,
                PaymentDate = DateTime.UtcNow.AddMonths(-3),
                ExDividendDate = DateTime.UtcNow.AddMonths(-3).AddDays(-7),
                AmountPerShare = 7.25m,
                TotalAmount = 362.50m, // 50 shares * 7.25
                SharesHeld = 50,
                Currency = "SEK",
                TaxWithheld = 108.75m, // 30% tax
                IsReinvested = false,
                Notes = "Ordinarie utdelning 2024",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            // Dividend from Investor B (InvestmentId = 3)
            new Dividend
            {
                DividendId = 3,
                InvestmentId = 3,
                PaymentDate = DateTime.UtcNow.AddMonths(-1),
                ExDividendDate = DateTime.UtcNow.AddMonths(-1).AddDays(-10),
                AmountPerShare = 12.50m,
                TotalAmount = 937.50m, // 75 shares * 12.50
                SharesHeld = 75,
                Currency = "SEK",
                TaxWithheld = 281.25m, // 30% tax
                IsReinvested = false,
                Notes = "Ordinarie utdelning 2025",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            // Dividend from Ericsson B (InvestmentId = 7)
            new Dividend
            {
                DividendId = 4,
                InvestmentId = 7,
                PaymentDate = DateTime.UtcNow.AddDays(-15),
                ExDividendDate = DateTime.UtcNow.AddDays(-20),
                AmountPerShare = 2.80m,
                TotalAmount = 700m, // 250 shares * 2.80
                SharesHeld = 250,
                Currency = "SEK",
                TaxWithheld = 210m, // 30% tax
                IsReinvested = false,
                Notes = "Delårsutdelning Q1 2025",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Dividends.AddRange(dividends);
        context.SaveChanges();
    }

    private static void SeedInvestmentTransactions(PrivatekonomyContext context, string userId)
    {
        var transactions = new List<InvestmentTransaction>
        {
            // Purchase of Volvo B
            new InvestmentTransaction
            {
                InvestmentTransactionId = 1,
                InvestmentId = 1,
                TransactionType = "Buy",
                Quantity = 100m,
                PricePerShare = 245.50m,
                TotalAmount = 24550m,
                Fees = 99m,
                Currency = "SEK",
                TransactionDate = DateTime.UtcNow.AddMonths(-6),
                Notes = "Initial köp av Volvo B aktier",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            // Purchase of SEB A
            new InvestmentTransaction
            {
                InvestmentTransactionId = 2,
                InvestmentId = 2,
                TransactionType = "Buy",
                Quantity = 50m,
                PricePerShare = 152.30m,
                TotalAmount = 7615m,
                Fees = 99m,
                Currency = "SEK",
                TransactionDate = DateTime.UtcNow.AddMonths(-8),
                Notes = "Köp av SEB A aktier",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            // Purchase of Investor B
            new InvestmentTransaction
            {
                InvestmentTransactionId = 3,
                InvestmentId = 3,
                TransactionType = "Buy",
                Quantity = 75m,
                PricePerShare = 289.00m,
                TotalAmount = 21675m,
                Fees = 99m,
                Currency = "SEK",
                TransactionDate = DateTime.UtcNow.AddMonths(-4),
                Notes = "Köp av Investor B",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            // Additional purchase of Ericsson B
            new InvestmentTransaction
            {
                InvestmentTransactionId = 4,
                InvestmentId = 7,
                TransactionType = "Buy",
                Quantity = 150m,
                PricePerShare = 58.20m,
                TotalAmount = 8730m,
                Fees = 99m,
                Currency = "SEK",
                TransactionDate = DateTime.UtcNow.AddMonths(-5),
                Notes = "Första köpet av Ericsson B",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new InvestmentTransaction
            {
                InvestmentTransactionId = 5,
                InvestmentId = 7,
                TransactionType = "Buy",
                Quantity = 100m,
                PricePerShare = 58.20m,
                TotalAmount = 5820m,
                Fees = 99m,
                Currency = "SEK",
                TransactionDate = DateTime.UtcNow.AddMonths(-3),
                Notes = "Påköp av Ericsson B",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            // Sale of some Nordea shares (showing a loss)
            new InvestmentTransaction
            {
                InvestmentTransactionId = 6,
                InvestmentId = 8,
                TransactionType = "Sell",
                Quantity = 20m,
                PricePerShare = 118.90m,
                TotalAmount = 2378m,
                Fees = 99m,
                Currency = "SEK",
                TransactionDate = DateTime.UtcNow.AddDays(-10),
                Notes = "Sålde 20 aktier med förlust",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.InvestmentTransactions.AddRange(transactions);
        context.SaveChanges();
    }

    private static void SeedSavingsChallenges(PrivatekonomyContext context, string userId)
    {
        var challenges = new List<SavingsChallenge>
        {
            new SavingsChallenge
            {
                SavingsChallengeId = 1,
                Name = "Ingen restaurang i januari",
                Description = "Spara pengar genom att äta hemlagad mat hela januari",
                Type = ChallengeType.NoRestaurant,
                TargetAmount = 3000m,
                CurrentAmount = 2450m,
                DurationDays = 31,
                StartDate = DateTime.UtcNow.AddDays(-25),
                EndDate = DateTime.UtcNow.AddDays(6),
                Status = ChallengeStatus.Active,
                CurrentStreak = 25,
                BestStreak = 25,
                Icon = "🍽️",
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Health,
                EstimatedSavingsMin = 2000m,
                EstimatedSavingsMax = 4000m,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new SavingsChallenge
            {
                SavingsChallengeId = 2,
                Name = "Spara 100 kr per dag",
                Description = "Sätt undan 100 kronor varje dag i 30 dagar",
                Type = ChallengeType.SaveDaily,
                TargetAmount = 3000m,
                CurrentAmount = 1800m,
                DurationDays = 30,
                StartDate = DateTime.UtcNow.AddDays(-18),
                EndDate = DateTime.UtcNow.AddDays(12),
                Status = ChallengeStatus.Active,
                CurrentStreak = 18,
                BestStreak = 18,
                Icon = "💰",
                Difficulty = DifficultyLevel.Easy,
                Category = ChallengeCategory.Individual,
                EstimatedSavingsMin = 3000m,
                EstimatedSavingsMax = 3000m,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new SavingsChallenge
            {
                SavingsChallengeId = 3,
                Name = "Ta med egen lunch",
                Description = "Ingen lunch ute - matlåda varje dag i 2 veckor",
                Type = ChallengeType.LunchBox,
                TargetAmount = 1400m,
                CurrentAmount = 1400m,
                DurationDays = 14,
                StartDate = DateTime.UtcNow.AddDays(-14),
                EndDate = DateTime.UtcNow,
                Status = ChallengeStatus.Completed,
                CurrentStreak = 14,
                BestStreak = 14,
                Icon = "🍱",
                Difficulty = DifficultyLevel.Medium,
                Category = ChallengeCategory.Health,
                EstimatedSavingsMin = 1000m,
                EstimatedSavingsMax = 1500m,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new SavingsChallenge
            {
                SavingsChallengeId = 4,
                Name = "Alkoholfri månad",
                Description = "Ingen alkohol under hela februari - spara pengar och må bättre",
                Type = ChallengeType.AlcoholFree,
                TargetAmount = 2500m,
                CurrentAmount = 0m,
                DurationDays = 28,
                StartDate = DateTime.UtcNow.AddDays(-5), // Started 5 days ago
                EndDate = DateTime.UtcNow.AddDays(23), // 23 days remaining
                Status = ChallengeStatus.Active,
                CurrentStreak = 5,
                BestStreak = 5,
                Icon = "🍷",
                Difficulty = DifficultyLevel.Hard,
                Category = ChallengeCategory.Health,
                EstimatedSavingsMin = 1000m,
                EstimatedSavingsMax = 5000m,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        var progressEntries = new List<SavingsChallengeProgress>
        {
            // Progress for "Ingen restaurang i januari" (25 days completed)
            new SavingsChallengeProgress
            {
                SavingsChallengeProgressId = 1,
                SavingsChallengeId = 1,
                Date = DateTime.UtcNow.AddDays(-25),
                Completed = true,
                AmountSaved = 100m,
                Notes = "Första dagen lyckad!",
                CreatedAt = DateTime.UtcNow
            },
            new SavingsChallengeProgress
            {
                SavingsChallengeProgressId = 2,
                SavingsChallengeId = 1,
                Date = DateTime.UtcNow.AddDays(-16),
                Completed = true,
                AmountSaved = 95m,
                Notes = "10 dagar klarat",
                CreatedAt = DateTime.UtcNow
            },
            new SavingsChallengeProgress
            {
                SavingsChallengeProgressId = 3,
                SavingsChallengeId = 1,
                Date = DateTime.UtcNow.AddDays(-6),
                Completed = true,
                AmountSaved = 110m,
                Notes = "20 dagar - går bra!",
                CreatedAt = DateTime.UtcNow
            },
            // Progress for "Spara 100 kr per dag" (18 days completed)
            new SavingsChallengeProgress
            {
                SavingsChallengeProgressId = 4,
                SavingsChallengeId = 2,
                Date = DateTime.UtcNow.AddDays(-18),
                Completed = true,
                AmountSaved = 100m,
                CreatedAt = DateTime.UtcNow
            },
            new SavingsChallengeProgress
            {
                SavingsChallengeProgressId = 5,
                SavingsChallengeId = 2,
                Date = DateTime.UtcNow.AddDays(-12),
                Completed = true,
                AmountSaved = 100m,
                Notes = "En vecka klar",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.SavingsChallenges.AddRange(challenges);
        context.SavingsChallengeProgress.AddRange(progressEntries);
        context.SaveChanges();
    }

    private static void SeedCurrencyAccounts(PrivatekonomyContext context, string userId)
    {
        var currencyAccounts = new List<CurrencyAccount>
        {
            new CurrencyAccount
            {
                CurrencyAccountId = 1,
                Currency = "USD",
                Balance = 5000m,
                ExchangeRate = 10.45m, // SEK per USD
                AccountNumber = "USD-001234",
                Description = "Sparkonto i USD för USA-resor",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new CurrencyAccount
            {
                CurrencyAccountId = 2,
                Currency = "EUR",
                Balance = 2500m,
                ExchangeRate = 11.32m, // SEK per EUR
                AccountNumber = "EUR-005678",
                Description = "Euro-konto för semesterresor i Europa",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new CurrencyAccount
            {
                CurrencyAccountId = 3,
                Currency = "GBP",
                Balance = 1000m,
                ExchangeRate = 13.15m, // SEK per GBP
                AccountNumber = "GBP-009012",
                Description = "Brittiska pund från tidigare resa",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.CurrencyAccounts.AddRange(currencyAccounts);
        context.SaveChanges();
    }

    private static void SeedLifeTimelineMilestones(PrivatekonomyContext context, string userId)
    {
        var milestones = new List<LifeTimelineMilestone>
        {
            new LifeTimelineMilestone
            {
                LifeTimelineMilestoneId = 1,
                Name = "Köpa första bostaden",
                Description = "Spara ihop till kontantinsats för en bostadsrätt i Stockholm",
                MilestoneType = "Housing",
                PlannedDate = DateTime.UtcNow.AddYears(2),
                EstimatedCost = 300000m, // 15% av 2 miljoner
                RequiredMonthlySavings = 12500m,
                ProgressPercentage = 40m,
                CurrentSavings = 120000m,
                Priority = 1,
                IsCompleted = false,
                Notes = "Siktar på en 2:a på Södermalm eller liknande område",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new LifeTimelineMilestone
            {
                LifeTimelineMilestoneId = 2,
                Name = "Studera vidare - Masterutbildning",
                Description = "Spara för masterutbildning utomlands",
                MilestoneType = "Education",
                PlannedDate = DateTime.UtcNow.AddYears(1),
                EstimatedCost = 150000m,
                RequiredMonthlySavings = 12500m,
                ProgressPercentage = 20m,
                CurrentSavings = 30000m,
                Priority = 2,
                IsCompleted = false,
                Notes = "Överväger universitet i Storbritannien eller Nederländerna",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new LifeTimelineMilestone
            {
                LifeTimelineMilestoneId = 3,
                Name = "Pension vid 65",
                Description = "Spara för en bekväm pension",
                MilestoneType = "Retirement",
                PlannedDate = DateTime.UtcNow.AddYears(30),
                EstimatedCost = 5000000m,
                RequiredMonthlySavings = 5500m,
                ProgressPercentage = 17m,
                CurrentSavings = 855000m, // Sum of pensions
                Priority = 1,
                IsCompleted = false,
                Notes = "Kombinerar tjänstepension, privat pensionssparande och AP7",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new LifeTimelineMilestone
            {
                LifeTimelineMilestoneId = 4,
                Name = "Stor utlandsresa - Japan & Thailand",
                Description = "Sparar till en drömresa i Asien i 4 veckor",
                MilestoneType = "Travel",
                PlannedDate = DateTime.UtcNow.AddMonths(18),
                EstimatedCost = 75000m,
                RequiredMonthlySavings = 4200m,
                ProgressPercentage = 56m,
                CurrentSavings = 42000m,
                Priority = 2,
                IsCompleted = false,
                Notes = "Tokyo, Kyoto, Bangkok och några thailändska öar",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            },
            new LifeTimelineMilestone
            {
                LifeTimelineMilestoneId = 5,
                Name = "Ny bil - Elbil",
                Description = "Spara till en elbil när nuvarande bil blir för gammal",
                MilestoneType = "Vehicle",
                PlannedDate = DateTime.UtcNow.AddYears(3),
                EstimatedCost = 450000m,
                RequiredMonthlySavings = 12500m,
                ProgressPercentage = 28m,
                CurrentSavings = 125000m,
                Priority = 3,
                IsCompleted = false,
                Notes = "Funderar på Tesla Model 3 eller Polestar 2",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.LifeTimelineMilestones.AddRange(milestones);
        context.SaveChanges();
    }

    private static void SeedNotifications(PrivatekonomyContext context, string userId)
    {
        var notifications = new List<Notification>
        {
            // Notification 1: Normal reminder - Electricity bill (BillReminder 1)
            new Notification
            {
                NotificationId = 1,
                UserId = userId,
                Type = SystemNotificationType.BillDue,
                Title = "Påminnelse: Betala Elräkning",
                Message = "Räkning på 1,450 kr förfaller om 13 dagar",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.Normal,
                ActionUrl = "/economy/bills/1",
                BillReminderId = 1,
                SentAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            
            // Notification 2: Snoozed - Rent bill (BillReminder 2)
            new Notification
            {
                NotificationId = 2,
                UserId = userId,
                Type = SystemNotificationType.BillDue,
                Title = "Påminnelse: Hyra",
                Message = "Räkning på 8,500 kr förfaller om 2 dagar",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.High,
                ActionUrl = "/economy/bills/6",
                BillReminderId = 2,
                SnoozeUntil = DateTime.UtcNow.AddDays(1),
                SnoozeCount = 1,
                SentAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            
            // Notification 3: Snoozed 2 times - Netflix (BillReminder 3)
            new Notification
            {
                NotificationId = 3,
                UserId = userId,
                Type = SystemNotificationType.BillDue,
                Title = "Påminnelse: Netflix Premium",
                Message = "Räkning på 139 kr förfaller 2025-11-10",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.Normal,
                ActionUrl = "/economy/bills/7",
                BillReminderId = 3,
                SnoozeUntil = DateTime.UtcNow.AddHours(6),
                SnoozeCount = 2,
                SentAt = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            
            // Notification 4: Critical overdue - Internet bill (BillReminder 4)
            new Notification
            {
                NotificationId = 4,
                UserId = userId,
                Type = SystemNotificationType.BillOverdue,
                Title = "⚠️ BRÅDSKANDE: Betala Interneträkning",
                Message = "Räkningen på 399 kr förföll för 8 dagar sedan. Åtgärd krävs omedelbart! (Snoozad 2 gånger)",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.Critical,
                ActionUrl = "/economy/bills/8",
                BillReminderId = 4,
                SnoozeCount = 2,
                SentAt = DateTime.UtcNow.AddHours(-12),
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            
            // Notification 5: Escalation level 2 - Rent (BillReminder 6)
            new Notification
            {
                NotificationId = 5,
                UserId = userId,
                Type = SystemNotificationType.BillDue,
                Title = "⚠️ BRÅDSKANDE: Hyra",
                Message = "Räkning på 8,500 kr förfaller om 5 dagar",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.High,
                ActionUrl = "/economy/bills/6",
                BillReminderId = 6,
                SentAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            
            // Notification 6: Completed/Read - Mobile bill (BillReminder 7)
            new Notification
            {
                NotificationId = 6,
                UserId = userId,
                Type = SystemNotificationType.BillDue,
                Title = "Påminnelse: Mobilabonnemang",
                Message = "Räkning på 349 kr förfaller snart",
                IsRead = true,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.Normal,
                ActionUrl = "/economy/bills/4",
                BillReminderId = 7,
                ReadAt = DateTime.UtcNow.AddDays(-3),
                SentAt = DateTime.UtcNow.AddDays(-8),
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },
            
            // Notification 7: Warning - Snoozed 3 times - Spotify (BillReminder 8)
            new Notification
            {
                NotificationId = 7,
                UserId = userId,
                Type = SystemNotificationType.BillDue,
                Title = "Påminnelse: Spotify Family",
                Message = "Räkning på 179 kr förfaller 2025-11-17 (Snoozad 3 gånger)",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.High,
                ActionUrl = "/economy/bills/9",
                BillReminderId = 8,
                SnoozeUntil = DateTime.UtcNow.AddDays(2),
                SnoozeCount = 3,
                SentAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            },
            
            // Notification 8: Recent - Home insurance (BillReminder 9)
            new Notification
            {
                NotificationId = 8,
                UserId = userId,
                Type = SystemNotificationType.BillDue,
                Title = "Påminnelse: Hemförsäkring",
                Message = "Räkning på 349 kr förfaller om 5 dagar",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.Normal,
                ActionUrl = "/economy/bills/2",
                BillReminderId = 9,
                SentAt = DateTime.UtcNow.AddHours(-6),
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            },
            
            // Notification 9: Goal achieved (not bill related)
            new Notification
            {
                NotificationId = 9,
                UserId = userId,
                Type = SystemNotificationType.GoalAchieved,
                Title = "Grattis! Sparmål uppnått",
                Message = "Du har nått ditt sparmål 'Ny cykel' på 15 000 kr!",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.Normal,
                ActionUrl = "/savings/goals",
                SentAt = DateTime.UtcNow.AddHours(-5),
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            
            // Notification 10: Subscription renewal (not bill related)
            new Notification
            {
                NotificationId = 10,
                UserId = userId,
                Type = SystemNotificationType.SubscriptionRenewal,
                Title = "Prenumeration förnyas snart",
                Message = "Din Spotify Premium Family-prenumeration förnyas om 7 dagar (179 kr)",
                IsRead = true,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.Low,
                ActionUrl = "/economy/subscriptions",
                ReadAt = DateTime.UtcNow.AddDays(-2),
                SentAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            
            // Notification 11: Budget warning (not bill related)
            new Notification
            {
                NotificationId = 11,
                UserId = userId,
                Type = SystemNotificationType.BudgetWarning,
                Title = "Budgetvarning",
                Message = "Du har använt 85% av din budget för 'Mat & Dryck' denna månad",
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Priority = NotificationPriority.High,
                ActionUrl = "/economy/budgets",
                SentAt = DateTime.UtcNow.AddHours(-12),
                CreatedAt = DateTime.UtcNow.AddHours(-12)
            }
        };

        context.Notifications.AddRange(notifications);
        context.SaveChanges();
    }



    private static void SeedAuditLogs(PrivatekonomyContext context, string userId)
    {
        // Only seed if no audit logs exist
        if (context.AuditLogs.Any())
        {
            return;
        }

        var auditLogs = new List<AuditLog>();
        var random = new Random(42);
        var startDate = DateTime.UtcNow.AddDays(-550);

        var actions = new[] { "Create", "Update", "Delete", "Login", "Logout", "Import", "Export" };
        var entityTypes = new[] { "Transaction", "Budget", "Goal", "Investment", "User", "Category" };

        for (int i = 1; i <= 200; i++)
        {
            var auditDate = startDate.AddDays(random.Next(0, 550));
            var action = actions[random.Next(actions.Length)];
            var entityType = entityTypes[random.Next(entityTypes.Length)];

            var auditLog = new AuditLog
            {
                AuditLogId = i,
                Action = action,
                EntityType = entityType,
                EntityId = random.Next(1, 100),
                UserId = userId,
                IpAddress = $"192.168.1.{random.Next(1, 255)}",
                Details = $"{action} {entityType.ToLower(CultureInfo.CurrentCulture)} with ID {random.Next(1, 100)}",
                CreatedAt = auditDate
            };

            auditLogs.Add(auditLog);
        }

        context.AuditLogs.AddRange(auditLogs);
        context.SaveChanges();
    }

    private static void SeedAdditionalBudgets(PrivatekonomyContext context, string userId)
    {
        // Only seed additional budgets if less than 10 budgets exist
        if (context.Budgets.Count() >= 10)
        {
            return;
        }

        var budgets = new List<Budget>();
        var budgetCategories = new List<BudgetCategory>();
        var random = new Random(42);
        var startDate = DateTime.UtcNow.AddDays(-550);

        // Create budgets for each month over the last 18 months
        for (int i = 0; i < 16; i++) // 16 more months (already have 2)
        {
            var monthDate = startDate.AddMonths(i);
            var startOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var budget = new Budget
            {
                BudgetId = i + 3, // Start from ID 3 (1-2 already exist)
                Name = "Månadsbudget " + monthDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("sv-SE")),
                Description = $"Budget för {monthDate:MMMM yyyy}",
                StartDate = startOfMonth,
                EndDate = endOfMonth,
                Period = BudgetPeriod.Monthly,
                CreatedAt = startOfMonth.AddDays(-5),
                UserId = userId
            };

            budgets.Add(budget);

            // Add budget categories for this budget
            var categoryBudgets = new[]
            {
                new { CategoryId = 1, BaseAmount = 4500m }, // Mat & Dryck
                new { CategoryId = 2, BaseAmount = 1400m }, // Transport
                new { CategoryId = 3, BaseAmount = 11500m }, // Boende
                new { CategoryId = 4, BaseAmount = 1800m }, // Nöje
                new { CategoryId = 5, BaseAmount = 2800m }, // Shopping
                new { CategoryId = 6, BaseAmount = 900m },  // Hälsa
            };

            int categoryIndex = 0;
            foreach (var cat in categoryBudgets)
            {
                var variation = random.Next(-200, 300);
                budgetCategories.Add(new BudgetCategory
                {
                    // Start from ID 13 (after existing 12 categories), increment by 1 for each category
                    BudgetCategoryId = 13 + (i * 6) + categoryIndex,
                    BudgetId = budget.BudgetId,
                    CategoryId = cat.CategoryId,
                    PlannedAmount = cat.BaseAmount + variation
                });
                categoryIndex++;
            }
        }

        context.Budgets.AddRange(budgets);
        context.BudgetCategories.AddRange(budgetCategories);
        context.SaveChanges();
    }

    private static void SeedMoreInvestmentHistory(PrivatekonomyContext context, string userId)
    {
        // Only seed if we don't have many investment transactions yet
        if (context.InvestmentTransactions.Count() >= 50)
        {
            return;
        }

        var investmentTransactions = new List<InvestmentTransaction>();
        var random = new Random(42);
        var startDate = DateTime.UtcNow.AddDays(-550);

        // Get existing investments
        var investmentIds = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var transactionTypes = new[] { "Köp", "Sälj" }; // Only Buy and Sell are supported

        // Generate investment transactions over 18 months
        for (int i = 1; i <= 100; i++)
        {
            var transactionDate = startDate.AddDays(random.Next(0, 550));
            var investmentId = investmentIds[random.Next(investmentIds.Length)];
            var transactionType = transactionTypes[random.Next(transactionTypes.Length)];
            
            var quantity = random.Next(1, 100);
            var pricePerShare = random.Next(50, 500);
            var totalAmount = quantity * pricePerShare;
            var fees = random.Next(29, 150) + (decimal)(random.NextDouble() * 0.99);

            var transaction = new InvestmentTransaction
            {
                InvestmentTransactionId = i + 20, // Offset existing ones
                InvestmentId = investmentId,
                TransactionType = transactionType,
                Quantity = quantity,
                PricePerShare = pricePerShare,
                TotalAmount = totalAmount,
                Fees = fees,
                Currency = "SEK",
                TransactionDate = transactionDate,
                Notes = GetInvestmentTransactionDescription(transactionType, investmentId),
                CreatedAt = transactionDate.AddHours(2),
                UserId = userId
            };

            investmentTransactions.Add(transaction);
        }

        context.InvestmentTransactions.AddRange(investmentTransactions);
        context.SaveChanges();
    }

    private static string GetInvestmentTransactionDescription(string type, int investmentId)
    {
        var investmentNames = new Dictionary<int, string>
        {
            { 1, "Volvo B" },
            { 2, "SEB A" },
            { 3, "Investor B" },
            { 4, "SPP Aktiefond Global" },
            { 5, "Länsförsäkringar Sverige" },
            { 6, "Avanza Global" },
            { 7, "Ericsson B" },
            { 8, "Nordea" }
        };

        var name = investmentNames.GetValueOrDefault(investmentId, "Okänd investering");
        
        return type switch
        {
            "Köp" => $"Köp av {name}",
            "Sälj" => $"Försäljning av {name}",
            _ => $"Transaktion för {name}"
        };
    }

    private static void SeedMoreNetWorthSnapshots(PrivatekonomyContext context, string userId)
    {
        // Only seed if we don't have many snapshots
        if (context.NetWorthSnapshots.Count() >= 20)
        {
            return;
        }

        var snapshots = new List<NetWorthSnapshot>();
        var random = new Random(42);
        var startDate = DateTime.UtcNow.AddDays(-550);

        // Generate monthly net worth snapshots over 18 months
        for (int i = 0; i < 18; i++)
        {
            var snapshotDate = startDate.AddDays(i * 30);
            var baseNetWorth = 850000m + (i * 15000m); // Growing net worth over time
            var variation = random.Next(-20000, 30000);
            var totalNetWorth = baseNetWorth + variation;

            var assets = totalNetWorth * (decimal)(0.85 + random.NextDouble() * 0.1); // 85-95% of net worth
            var liabilities = totalNetWorth - assets;

            // Breakdown of assets
            var bankBalance = assets * 0.25m;
            var investmentValue = assets * 0.45m;
            var physicalAssetValue = assets * 0.30m;
            var loanBalance = Math.Abs(liabilities);

            var snapshot = new NetWorthSnapshot
            {
                NetWorthSnapshotId = i + 5, // Offset existing ones
                UserId = userId,
                Date = snapshotDate,
                TotalAssets = Math.Max(0, assets),
                TotalLiabilities = Math.Max(0, loanBalance),
                NetWorth = totalNetWorth,
                BankBalance = bankBalance,
                InvestmentValue = investmentValue,
                PhysicalAssetValue = physicalAssetValue,
                LoanBalance = loanBalance,
                Notes = i % 6 == 0 ? "Halvårsutvärdering av ekonomin" : 
                       i % 3 == 0 ? "Kvartalsutvärdering" : null,
                CreatedAt = snapshotDate,
                IsManual = i % 4 == 0 // Every 4th snapshot is manually created
            };

            snapshots.Add(snapshot);
        }

        context.NetWorthSnapshots.AddRange(snapshots);
        context.SaveChanges();
    }

    private static void SeedMoreTransactions(PrivatekonomyContext context, string userId)
    {
        // Only seed if we don't have too many transactions already
        if (context.Transactions.Count() >= 500)
        {
            return;
        }

        var transactions = new List<Transaction>();
        var random = new Random(42);
        var startDate = DateTime.UtcNow.AddDays(-730); // 2 years of data

        var categories = context.Categories.ToList();
        var paymentMethods = new[] { "Swish", "Kort", "Banköverföring", "Autogiro", "Kontant" };
        
        // Common Swedish expense merchants
        var merchants = new[]
        {
            "ICA Maxi", "Coop", "Willys", "Hemköp", "Lidl",
            "Circle K", "Preem", "Shell", "OKQ8",
            "H&M", "Zara", "KappAhl", "Lindex",
            "IKEA", "Jula", "Bauhaus", "Hornbach",
            "Systembolaget", "Restaurang", "Café",
            "SL", "Västtrafik", "Skånetrafiken",
            "SF Bio", "Filmstaden", "Spotify", "Netflix"
        };

        // Generate 300 more transactions over 2 years
        for (int i = 0; i < 300; i++)
        {
            var transactionDate = startDate.AddDays(random.Next(0, 730));
            var isIncome = random.Next(0, 100) < 15; // 15% income transactions
            var amount = isIncome 
                ? random.Next(15000, 55000) // Salary or income
                : random.Next(25, 3500); // Expenses
            
            var merchant = isIncome ? "Lön" : merchants[random.Next(merchants.Length)];
            var paymentMethod = paymentMethods[random.Next(paymentMethods.Length)];
            
            var transaction = new Transaction
            {
                Amount = amount,
                Description = isIncome ? $"Lön {transactionDate:yyyy-MM}" : $"{merchant} - {transactionDate:yyyy-MM-dd}",
                Date = transactionDate,
                IsIncome = isIncome,
                Payee = merchant,
                PaymentMethod = paymentMethod,
                Currency = "SEK",
                Cleared = random.Next(0, 100) < 80, // 80% cleared
                Imported = random.Next(0, 100) < 60, // 60% imported
                ImportSource = random.Next(0, 100) < 60 ? "Swedbank API" : null,
                ValidFrom = transactionDate,
                CreatedAt = transactionDate.AddHours(random.Next(1, 24)),
                UserId = userId
            };
            
            transactions.Add(transaction);
        }

        context.Transactions.AddRange(transactions);
        context.SaveChanges();
    }

    private static void SeedMoreGoals(PrivatekonomyContext context, string userId)
    {
        // Only seed if we don't have many goals already
        if (context.Goals.Count() >= 10)
        {
            return;
        }

        var goals = new List<Goal>
        {
            new Goal
            {
                Name = "Ny cykel",
                Description = "En bra mountainbike för terräng",
                TargetAmount = 15000m,
                CurrentAmount = 8500m,
                TargetDate = DateTime.UtcNow.AddMonths(4),
                Priority = 2,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UserId = userId
            },
            new Goal
            {
                Name = "Nödfondsreserv",
                Description = "6 månaders levnadskostnader i buffert",
                TargetAmount = 180000m,
                CurrentAmount = 95000m,
                TargetDate = DateTime.UtcNow.AddMonths(14),
                Priority = 1,
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                UserId = userId
            },
            new Goal
            {
                Name = "Julklappar 2025",
                Description = "Budget för julklappar till familjen",
                TargetAmount = 12000m,
                CurrentAmount = 2400m,
                TargetDate = DateTime.UtcNow.AddMonths(11),
                Priority = 3,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UserId = userId
            },
            new Goal
            {
                Name = "Körkort",
                Description = "Spara ihop till körkort och körskola",
                TargetAmount = 18000m,
                CurrentAmount = 5000m,
                TargetDate = DateTime.UtcNow.AddMonths(8),
                Priority = 2,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UserId = userId
            },
            new Goal
            {
                Name = "Ny mobil",
                Description = "iPhone 16 Pro",
                TargetAmount = 14995m,
                CurrentAmount = 14995m,
                TargetDate = DateTime.UtcNow.AddMonths(-1),
                Priority = 3,
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UserId = userId
            }
        };

        context.Goals.AddRange(goals);
        context.SaveChanges();
    }

    private static void SeedMoreSubscriptions(PrivatekonomyContext context, string userId)
    {
        // Only seed if we don't have many subscriptions already
        if (context.Subscriptions.Count() >= 15)
        {
            return;
        }

        var subscriptions = new List<Subscription>
        {
            new Subscription
            {
                Name = "Apple Music Family",
                Description = "Musikstreaming för hela familjen",
                Price = 169m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                StartDate = DateTime.UtcNow.AddMonths(-18),
                NextBillingDate = DateTime.UtcNow.AddDays(12),
                IsActive = true,
                Notes = "Familjeplan med 6 konton",
                CreatedAt = DateTime.UtcNow.AddMonths(-18),
                UserId = userId
            },
            new Subscription
            {
                Name = "YouTube Premium",
                Description = "Reklamfri YouTube och YouTube Music",
                Price = 139m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                StartDate = DateTime.UtcNow.AddMonths(-8),
                NextBillingDate = DateTime.UtcNow.AddDays(20),
                IsActive = true,
                Notes = "Inkluderar YouTube Music",
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                UserId = userId
            },
            new Subscription
            {
                Name = "Viaplay Total",
                Description = "Sport, serier och filmer",
                Price = 699m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                StartDate = DateTime.UtcNow.AddMonths(-4),
                NextBillingDate = DateTime.UtcNow.AddDays(8),
                IsActive = true,
                Notes = "Familjepaket med alla kanaler",
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UserId = userId
            },
            new Subscription
            {
                Name = "Adobe Creative Cloud",
                Description = "Photoshop, Illustrator, och mer",
                Price = 599m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                StartDate = DateTime.UtcNow.AddMonths(-24),
                NextBillingDate = DateTime.UtcNow.AddDays(5),
                IsActive = true,
                Notes = "Alla Creative Cloud-appar ingår",
                CreatedAt = DateTime.UtcNow.AddMonths(-24),
                UserId = userId
            },
            new Subscription
            {
                Name = "Gym & Fitness",
                Description = "Medlemskap på lokalt gym",
                Price = 349m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                StartDate = DateTime.UtcNow.AddMonths(-12),
                NextBillingDate = DateTime.UtcNow.AddDays(15),
                IsActive = true,
                CancellationNoticeDays = 90,
                LastUsedDate = DateTime.UtcNow.AddDays(-3),
                Notes = "Medlemskap på hemmastudion",
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                UserId = userId
            },
            new Subscription
            {
                Name = "Nintendo Switch Online",
                Description = "Online-spel för Nintendo Switch",
                Price = 199m,
                Currency = "SEK",
                BillingFrequency = "Yearly",
                StartDate = DateTime.UtcNow.AddMonths(-9),
                NextBillingDate = DateTime.UtcNow.AddMonths(3),
                IsActive = true,
                Notes = "Familjeplan för upp till 8 användare",
                CreatedAt = DateTime.UtcNow.AddMonths(-9),
                UserId = userId
            },
            new Subscription
            {
                Name = "LinkedIn Premium",
                Description = "Karriärutveckling och nätverkande",
                Price = 399m,
                Currency = "SEK",
                BillingFrequency = "Monthly",
                StartDate = DateTime.UtcNow.AddMonths(-6),
                EndDate = DateTime.UtcNow.AddMonths(-2),
                IsActive = false,
                Notes = "Avslutad efter provperiod",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UserId = userId
            }
        };

        context.Subscriptions.AddRange(subscriptions);
        context.SaveChanges();
    }

    private static void SeedReceipts(PrivatekonomyContext context, string userId)
    {
        // Only seed if no receipts exist
        if (context.Receipts.Any())
        {
            return;
        }

        var categories = context.Categories.ToList();
        var foodCategoryId = categories.FirstOrDefault(c => c.Name.Contains("Mat"))?.CategoryId;

        var receipts = new List<Receipt>
        {
            new Receipt
            {
                UserId = userId,
                Merchant = "ICA Maxi",
                ReceiptDate = DateTime.UtcNow.AddDays(-5),
                TotalAmount = 487.50m,
                Currency = "SEK",
                ReceiptType = "Physical",
                ReceiptNumber = "ICA-2024-001234",
                PaymentMethod = "Kort",
                Notes = "Veckans matinköp",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ReceiptLineItems = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem
                    {
                        Description = "Mjölk 3% 1L",
                        Quantity = 2,
                        UnitPrice = 14.50m,
                        TotalPrice = 29.00m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Bröd Fullkorn",
                        Quantity = 3,
                        UnitPrice = 22.50m,
                        TotalPrice = 67.50m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Kött Nötfärs 500g",
                        Quantity = 2,
                        UnitPrice = 49.90m,
                        TotalPrice = 99.80m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Pasta 500g",
                        Quantity = 4,
                        UnitPrice = 12.90m,
                        TotalPrice = 51.60m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Grönsaker Blandade",
                        Quantity = 1,
                        UnitPrice = 89.60m,
                        TotalPrice = 89.60m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Ägg 12-pack",
                        Quantity = 2,
                        UnitPrice = 39.90m,
                        TotalPrice = 79.80m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Yoghurt Naturell 1kg",
                        Quantity = 1,
                        UnitPrice = 35.20m,
                        TotalPrice = 35.20m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Ost Kvibille 500g",
                        Quantity = 1,
                        UnitPrice = 35.00m,
                        TotalPrice = 35.00m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    }
                }
            },
            new Receipt
            {
                UserId = userId,
                Merchant = "Willys",
                ReceiptDate = DateTime.UtcNow.AddDays(-12),
                TotalAmount = 267.80m,
                Currency = "SEK",
                ReceiptType = "Physical",
                ReceiptNumber = "WILLYS-45678",
                PaymentMethod = "Swish",
                Notes = "Snabbhandling",
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                ReceiptLineItems = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem
                    {
                        Description = "Vatten Ramlösa 6-pack",
                        Quantity = 2,
                        UnitPrice = 39.90m,
                        TotalPrice = 79.80m,
                        TaxRate = 25m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Kaffe 500g",
                        Quantity = 1,
                        UnitPrice = 54.90m,
                        TotalPrice = 54.90m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Köttbullar Frysta 1kg",
                        Quantity = 1,
                        UnitPrice = 79.90m,
                        TotalPrice = 79.90m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Chips Orginal 175g",
                        Quantity = 2,
                        UnitPrice = 26.60m,
                        TotalPrice = 53.20m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    }
                }
            },
            new Receipt
            {
                UserId = userId,
                Merchant = "Coop",
                ReceiptDate = DateTime.UtcNow.AddDays(-20),
                TotalAmount = 156.40m,
                Currency = "SEK",
                ReceiptType = "E-Receipt",
                ReceiptNumber = "COOP-E-9876",
                PaymentMethod = "Kort",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                ReceiptLineItems = new List<ReceiptLineItem>
                {
                    new ReceiptLineItem
                    {
                        Description = "Frukt Äpplen 1kg",
                        Quantity = 2,
                        UnitPrice = 29.90m,
                        TotalPrice = 59.80m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Bananer 1kg",
                        Quantity = 1,
                        UnitPrice = 19.90m,
                        TotalPrice = 19.90m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Apelsiner 1kg",
                        Quantity = 1,
                        UnitPrice = 24.90m,
                        TotalPrice = 24.90m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Tomater 500g",
                        Quantity = 2,
                        UnitPrice = 15.90m,
                        TotalPrice = 31.80m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    },
                    new ReceiptLineItem
                    {
                        Description = "Gurka",
                        Quantity = 2,
                        UnitPrice = 9.95m,
                        TotalPrice = 19.90m,
                        TaxRate = 12m,
                        CategoryId = foodCategoryId,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    }
                }
            }
        };

        context.Receipts.AddRange(receipts);
        context.SaveChanges();
    }

    private static void SeedPortfolioAllocations(PrivatekonomyContext context, string userId)
    {
        // Only seed if no allocations exist
        if (context.PortfolioAllocations.Any())
        {
            return;
        }

        var allocations = new List<PortfolioAllocation>
        {
            new PortfolioAllocation
            {
                Name = "Balanserad Portfölj",
                AssetClass = "Aktier",
                TargetPercentage = 60m,
                MinPercentage = 55m,
                MaxPercentage = 65m,
                IsActive = true,
                Notes = "Huvudsaklig tillväxt genom svenska och globala aktier",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UserId = userId
            },
            new PortfolioAllocation
            {
                Name = "Balanserad Portfölj",
                AssetClass = "Obligationer",
                TargetPercentage = 30m,
                MinPercentage = 25m,
                MaxPercentage = 35m,
                IsActive = true,
                Notes = "Stabilitet genom statsobligationer och företagsobligationer",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UserId = userId
            },
            new PortfolioAllocation
            {
                Name = "Balanserad Portfölj",
                AssetClass = "Kontanter",
                TargetPercentage = 5m,
                MinPercentage = 3m,
                MaxPercentage = 10m,
                IsActive = true,
                Notes = "Likviditet för oväntade utgifter",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UserId = userId
            },
            new PortfolioAllocation
            {
                Name = "Balanserad Portfölj",
                AssetClass = "Fastigheter",
                TargetPercentage = 5m,
                MinPercentage = 0m,
                MaxPercentage = 10m,
                IsActive = true,
                Notes = "REITs och fastighetsfonder",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UserId = userId
            },
            new PortfolioAllocation
            {
                Name = "Aggressiv Tillväxt",
                AssetClass = "Aktier",
                TargetPercentage = 90m,
                MinPercentage = 85m,
                MaxPercentage = 95m,
                IsActive = false,
                Notes = "Maximal tillväxt, högre risk",
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                UserId = userId
            },
            new PortfolioAllocation
            {
                Name = "Aggressiv Tillväxt",
                AssetClass = "Obligationer",
                TargetPercentage = 5m,
                MinPercentage = 3m,
                MaxPercentage = 10m,
                IsActive = false,
                Notes = "Minimal obligationsandel",
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                UserId = userId
            },
            new PortfolioAllocation
            {
                Name = "Aggressiv Tillväxt",
                AssetClass = "Kontanter",
                TargetPercentage = 5m,
                MinPercentage = 2m,
                MaxPercentage = 5m,
                IsActive = false,
                Notes = "Minimal kassa",
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                UserId = userId
            }
        };

        context.PortfolioAllocations.AddRange(allocations);
        context.SaveChanges();
    }

    private static void SeedSavingsGroups(PrivatekonomyContext context, string userId)
    {
        // Only seed if no groups exist
        if (context.SavingsGroups.Any())
        {
            return;
        }

        var groups = new List<SavingsGroup>
        {
            new SavingsGroup
            {
                Name = "Familjen Andersson",
                Description = "Familjespargrupp för gemensamma mål",
                CreatedByUserId = userId,
                GroupType = SavingsGroupType.Family,
                PrivacyLevel = GroupPrivacyLevel.Private,
                AnonymousMembers = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new SavingsGroup
            {
                Name = "Jobbkompisarna",
                Description = "Spara tillsammans med kollegor",
                CreatedByUserId = userId,
                GroupType = SavingsGroupType.Friends,
                PrivacyLevel = GroupPrivacyLevel.Restricted,
                AnonymousMembers = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            },
            new SavingsGroup
            {
                Name = "Semesterkassan",
                Description = "Gruppresa till Thailand 2025",
                CreatedByUserId = userId,
                GroupType = SavingsGroupType.Challenge,
                PrivacyLevel = GroupPrivacyLevel.Restricted,
                AnonymousMembers = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-4)
            },
            new SavingsGroup
            {
                Name = "Ekonomi Community Stockholm",
                Description = "Öppen grupp för att dela spartips och motivation",
                CreatedByUserId = userId,
                GroupType = SavingsGroupType.Community,
                PrivacyLevel = GroupPrivacyLevel.Public,
                AnonymousMembers = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            }
        };

        context.SavingsGroups.AddRange(groups);
        context.SaveChanges();
    }

    private static void SeedLifeTimelineScenarios(PrivatekonomyContext context, string userId)
    {
        // Only seed if no scenarios exist
        if (context.LifeTimelineScenarios.Any())
        {
            return;
        }

        var scenarios = new List<LifeTimelineScenario>
        {
            new LifeTimelineScenario
            {
                Name = "Realistisk",
                Description = "Baserat på historisk avkastning och försiktig prognos",
                ExpectedReturnRate = 7.0m,
                MonthlySavings = 5000m,
                RetirementAge = 65,
                ExpectedMonthlyPension = 18500m,
                ProjectedRetirementWealth = 4250000m,
                InflationRate = 2.0m,
                SalaryIncreaseRate = 2.5m,
                IsActive = true,
                IsBaseline = true,
                Notes = "Huvudscenario baserat på realistiska antaganden",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UserId = userId
            },
            new LifeTimelineScenario
            {
                Name = "Optimistisk",
                Description = "Högre avkastning och ökade besparingar",
                ExpectedReturnRate = 10.0m,
                MonthlySavings = 7000m,
                RetirementAge = 62,
                ExpectedMonthlyPension = 25000m,
                ProjectedRetirementWealth = 6500000m,
                InflationRate = 1.5m,
                SalaryIncreaseRate = 3.5m,
                IsActive = false,
                IsBaseline = false,
                Notes = "Best case scenario med högre sparande och avkastning",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UserId = userId
            },
            new LifeTimelineScenario
            {
                Name = "Pessimistisk",
                Description = "Lägre avkastning och försiktig ekonomi",
                ExpectedReturnRate = 4.0m,
                MonthlySavings = 3500m,
                RetirementAge = 67,
                ExpectedMonthlyPension = 14500m,
                ProjectedRetirementWealth = 2100000m,
                InflationRate = 3.0m,
                SalaryIncreaseRate = 1.5m,
                IsActive = false,
                IsBaseline = false,
                Notes = "Worst case scenario för att vara förberedd",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UserId = userId
            },
            new LifeTimelineScenario
            {
                Name = "Tidigt Pension vid 60",
                Description = "Scenario för att gå i pension vid 60 år",
                ExpectedReturnRate = 7.5m,
                MonthlySavings = 8500m,
                RetirementAge = 60,
                ExpectedMonthlyPension = 16000m,
                ProjectedRetirementWealth = 4800000m,
                InflationRate = 2.0m,
                SalaryIncreaseRate = 2.5m,
                IsActive = false,
                IsBaseline = false,
                Notes = "Kräver högre månadssparande för tidigare pension",
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UserId = userId
            }
        };

        context.LifeTimelineScenarios.AddRange(scenarios);
        context.SaveChanges();
    }

    private static void SeedGoalShares(PrivatekonomyContext context, string userId)
    {
        // Only seed if no goal shares exist
        if (context.GoalShares.Any())
        {
            return;
        }

        // Get existing goals to share
        var goals = context.Goals.Where(g => g.UserId == userId).Take(3).ToList();
        
        if (!goals.Any())
        {
            return; // No goals to share
        }

        var shares = new List<GoalShare>();
        
        foreach (var goal in goals)
        {
            shares.Add(new GoalShare
            {
                GoalId = goal.GoalId,
                ShareToken = Guid.NewGuid().ToString("N").Substring(0, 16),
                UserId = userId,
                ShowCurrentAmount = true,
                ShowTargetAmount = true,
                ShowTargetDate = true,
                ShowTransactions = false,
                ShowOwnerName = false,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                IsActive = true,
                ViewCount = new Random().Next(0, 25)
            });
        }

        // Add one expired share
        if (goals.Count > 0)
        {
            shares.Add(new GoalShare
            {
                GoalId = goals[0].GoalId,
                ShareToken = Guid.NewGuid().ToString("N").Substring(0, 16),
                UserId = userId,
                ShowCurrentAmount = true,
                ShowTargetAmount = true,
                ShowTargetDate = false,
                ShowTransactions = false,
                ShowOwnerName = true,
                ExpiresAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-35),
                IsActive = false,
                ViewCount = 47
            });
        }

        context.GoalShares.AddRange(shares);
        context.SaveChanges();
    }
}
