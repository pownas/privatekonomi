using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class BudgetAccountOwnershipTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly BudgetService _budgetService;
    private readonly BankSourceService _bankSourceService;
    private const string CurrentUserId = "user-a";
    private const string OtherUserId = "user-b";

    public BudgetAccountOwnershipTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new PrivatekonomyContext(options);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(service => service.IsAuthenticated).Returns(true);
        currentUser.Setup(service => service.UserId).Returns(CurrentUserId);

        _budgetService = new BudgetService(_context, currentUser.Object);
        _bankSourceService = new BankSourceService(_context, currentUser.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task BudgetReadsAndCalculationsOnlyIncludeCurrentUsersData()
    {
        var startDate = DateTime.Today.AddDays(-1);
        var category = new Category { Name = "Groceries" };
        var ownBudget = new Budget
        {
            Name = "Own",
            UserId = CurrentUserId,
            StartDate = startDate,
            EndDate = DateTime.Today.AddDays(1)
        };
        var otherBudget = new Budget
        {
            Name = "Other",
            UserId = OtherUserId,
            StartDate = startDate,
            EndDate = DateTime.Today.AddDays(1)
        };
        var ownTransaction = new Transaction
        {
            UserId = CurrentUserId,
            Date = DateTime.Today,
            Amount = 10,
            TransactionCategories = [new TransactionCategory { Category = category, CategoryId = 1, Amount = 10 }]
        };
        var otherTransaction = new Transaction
        {
            UserId = OtherUserId,
            Date = DateTime.Today,
            Amount = 90,
            TransactionCategories = [new TransactionCategory { Category = category, CategoryId = 1, Amount = 90 }]
        };

        _context.Budgets.AddRange(ownBudget, otherBudget);
        _context.Transactions.AddRange(ownTransaction, otherTransaction);
        await _context.SaveChangesAsync();

        var budgets = await _budgetService.GetAllBudgetsAsync();
        var ownBudgetById = await _budgetService.GetBudgetByIdAsync(ownBudget.BudgetId);
        var inaccessibleBudgetById = await _budgetService.GetBudgetByIdAsync(otherBudget.BudgetId);
        var activeBudgets = await _budgetService.GetActiveBudgetsAsync();
        var amounts = await _budgetService.GetActualAmountsByCategoryAsync(ownBudget.BudgetId);
        var transactions = await _budgetService.GetTransactionsForBudgetAsync(ownBudget.BudgetId);
        var transactionsByCategory = await _budgetService.GetTransactionsByCategoryForBudgetAsync(ownBudget.BudgetId);
        var inaccessibleTransactions = await _budgetService.GetTransactionsForBudgetAsync(otherBudget.BudgetId);
        var inaccessibleAmounts = await _budgetService.GetActualAmountsByCategoryAsync(otherBudget.BudgetId);

        Assert.AreEqual(1, budgets.Count());
        Assert.AreEqual("Own", ownBudgetById?.Name);
        Assert.IsNull(inaccessibleBudgetById);
        Assert.AreEqual(1, activeBudgets.Count());
        Assert.AreEqual("Own", activeBudgets.Single().Name);
        Assert.AreEqual(10m, amounts[1]);
        Assert.AreEqual(1, transactions.Count());
        Assert.AreEqual(1, transactionsByCategory[1].Count);
        Assert.AreEqual(0, inaccessibleTransactions.Count());
        Assert.AreEqual(0, inaccessibleAmounts.Count);
    }

    [TestMethod]
    public async Task BudgetUpdatesAndDeletesCannotAffectAnotherUsersBudget()
    {
        var otherBudget = new Budget
        {
            Name = "Other",
            UserId = OtherUserId,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1)
        };
        _context.Budgets.Add(otherBudget);
        await _context.SaveChangesAsync();

        await AssertInvalidOperationAsync(() =>
            _budgetService.UpdateBudgetAsync(new Budget { BudgetId = otherBudget.BudgetId, Name = "Changed" }));
        await _budgetService.DeleteBudgetAsync(otherBudget.BudgetId);

        Assert.AreEqual("Other", (await _context.Budgets.FindAsync(otherBudget.BudgetId))?.Name);
    }

    [TestMethod]
    public async Task BudgetCreationAndUpdateUseServerSideOwner()
    {
        var created = await _budgetService.CreateBudgetAsync(new Budget
        {
            Name = "Budget",
            UserId = OtherUserId,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1)
        });

        Assert.AreEqual(CurrentUserId, created.UserId);

        created.UserId = OtherUserId;
        created.Name = "Updated";
        var updated = await _budgetService.UpdateBudgetAsync(created);

        Assert.AreEqual(CurrentUserId, updated.UserId);
        Assert.AreEqual("Updated", updated.Name);
    }

    [TestMethod]
    public async Task BankSourceUpdateAndDeleteCannotAffectAnotherUsersAccount()
    {
        var otherAccount = new BankSource
        {
            Name = "Other account",
            UserId = OtherUserId
        };
        var ownAccount = new BankSource
        {
            Name = "Own account",
            UserId = CurrentUserId
        };
        _context.BankSources.AddRange(otherAccount, ownAccount);
        await _context.SaveChangesAsync();

        var accounts = await _bankSourceService.GetAllBankSourcesAsync();
        var inaccessibleAccount = await _bankSourceService.GetBankSourceByIdAsync(otherAccount.BankSourceId);
        await AssertInvalidOperationAsync(() =>
            _bankSourceService.UpdateBankSourceAsync(new BankSource
            {
                BankSourceId = otherAccount.BankSourceId,
                Name = "Changed",
                UserId = CurrentUserId
            }));
        await _bankSourceService.DeleteBankSourceAsync(otherAccount.BankSourceId);

        Assert.AreEqual(1, accounts.Count());
        Assert.IsNull(inaccessibleAccount);
        Assert.AreEqual("Other account", (await _context.BankSources.FindAsync(otherAccount.BankSourceId))?.Name);
    }

    [TestMethod]
    public async Task BankSourceCreationAndUpdateUseServerSideOwner()
    {
        var created = await _bankSourceService.CreateBankSourceAsync(new BankSource
        {
            Name = "Account",
            UserId = OtherUserId
        });

        Assert.AreEqual(CurrentUserId, created.UserId);

        created.UserId = OtherUserId;
        created.Name = "Updated account";
        var updated = await _bankSourceService.UpdateBankSourceAsync(created);

        Assert.AreEqual(CurrentUserId, updated.UserId);
        Assert.AreEqual("Updated account", updated.Name);
    }

    [TestMethod]
    public async Task AuthenticatedUserWithoutIdCannotReadOrModifyOwnedRecords()
    {
        var budget = new Budget
        {
            Name = "Owned budget",
            UserId = CurrentUserId,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1)
        };
        var account = new BankSource
        {
            Name = "Owned account",
            UserId = CurrentUserId
        };
        _context.AddRange(budget, account);
        await _context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(service => service.IsAuthenticated).Returns(true);
        currentUser.Setup(service => service.UserId).Returns((string?)null);
        var budgetService = new BudgetService(_context, currentUser.Object);
        var bankSourceService = new BankSourceService(_context, currentUser.Object);

        Assert.AreEqual(0, (await budgetService.GetAllBudgetsAsync()).Count());
        Assert.IsNull(await budgetService.GetBudgetByIdAsync(budget.BudgetId));
        Assert.AreEqual(0, (await budgetService.GetActiveBudgetsAsync()).Count());
        Assert.IsNull(await bankSourceService.GetBankSourceByIdAsync(account.BankSourceId));
        Assert.AreEqual(0, (await bankSourceService.GetAllBankSourcesAsync()).Count());

        await budgetService.DeleteBudgetAsync(budget.BudgetId);
        await bankSourceService.DeleteBankSourceAsync(account.BankSourceId);
        await AssertInvalidOperationAsync(() =>
            budgetService.UpdateBudgetAsync(new Budget { BudgetId = budget.BudgetId }));
        await AssertInvalidOperationAsync(() =>
            bankSourceService.UpdateBankSourceAsync(new BankSource { BankSourceId = account.BankSourceId }));

        Assert.IsNotNull(await _context.Budgets.FindAsync(budget.BudgetId));
        Assert.IsNotNull(await _context.BankSources.FindAsync(account.BankSourceId));
    }

    private static async Task AssertInvalidOperationAsync(Func<Task> action)
    {
        try
        {
            await action();
            Assert.Fail("Expected an InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
        }
    }
}
