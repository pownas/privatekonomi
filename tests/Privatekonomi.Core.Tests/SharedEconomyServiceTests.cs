using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class SharedEconomyServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly HouseholdService _householdService;

    public SharedEconomyServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _householdService = new HouseholdService(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Shared Budget Tests

    [TestMethod]
    public async Task CreateSharedBudgetAsync_CreatesSharedBudgetSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var member1 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 1" };
        var member2 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 2" };
        _context.HouseholdMembers.AddRange(member1, member2);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Shared Household Budget",
            Description = "Our joint budget",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            HouseholdId = household.HouseholdId
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member1.HouseholdMemberId, 60m },
            { member2.HouseholdMemberId, 40m }
        };

        // Act
        var result = await _householdService.CreateSharedBudgetAsync(budget, contributions);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Shared Household Budget", result.Name);
        Assert.AreEqual(household.HouseholdId, result.HouseholdId);

        var shares = await _context.HouseholdBudgetShares
            .Where(s => s.BudgetId == result.BudgetId)
            .ToListAsync();
        Assert.AreEqual(2, shares.Count);
        Assert.IsTrue(shares.Any(s => s.HouseholdMemberId == member1.HouseholdMemberId && s.SharePercentage == 60m));
        Assert.IsTrue(shares.Any(s => s.HouseholdMemberId == member2.HouseholdMemberId && s.SharePercentage == 40m));
    }

    [TestMethod]
    public async Task CreateSharedBudgetAsync_ThrowsWhenContributionsDoNotSumTo100()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var member1 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 1" };
        _context.HouseholdMembers.Add(member1);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Shared Household Budget",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member1.HouseholdMemberId, 80m }
        };

        // Act & Assert
        try
        {
            await _householdService.CreateSharedBudgetAsync(budget, contributions);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task GetHouseholdBudgetsAsync_ReturnsAllHouseholdBudgets()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var member = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 1" };
        _context.HouseholdMembers.Add(member);
        await _context.SaveChangesAsync();

        var budget1 = new Budget
        {
            Name = "Budget 1",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly
        };
        var budget2 = new Budget
        {
            Name = "Budget 2",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly
        };

        await _householdService.CreateSharedBudgetAsync(budget1, new Dictionary<int, decimal> { { member.HouseholdMemberId, 100m } });
        await _householdService.CreateSharedBudgetAsync(budget2, new Dictionary<int, decimal> { { member.HouseholdMemberId, 100m } });

        // Act
        var result = await _householdService.GetHouseholdBudgetsAsync(household.HouseholdId);

        // Assert
        var budgets = result.ToList();
        Assert.AreEqual(2, budgets.Count);
    }

    [TestMethod]
    public async Task GetBudgetContributionsAsync_ReturnsCorrectContributions()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var member1 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 1" };
        var member2 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 2" };
        _context.HouseholdMembers.AddRange(member1, member2);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Test Budget",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member1.HouseholdMemberId, 55m },
            { member2.HouseholdMemberId, 45m }
        };

        await _householdService.CreateSharedBudgetAsync(budget, contributions);

        // Act
        var result = await _householdService.GetBudgetContributionsAsync(budget.BudgetId);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(55m, result[member1.HouseholdMemberId]);
        Assert.AreEqual(45m, result[member2.HouseholdMemberId]);
    }

    #endregion

    #region Debt Settlement Tests

    [TestMethod]
    public async Task CreateDebtAsync_CreatesDebtSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var debtor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Debtor" };
        var creditor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Creditor" };
        _context.HouseholdMembers.AddRange(debtor, creditor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _householdService.CreateDebtAsync(
            household.HouseholdId,
            debtor.HouseholdMemberId,
            creditor.HouseholdMemberId,
            150.50m,
            "Groceries split");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(household.HouseholdId, result.HouseholdId);
        Assert.AreEqual(debtor.HouseholdMemberId, result.DebtorMemberId);
        Assert.AreEqual(creditor.HouseholdMemberId, result.CreditorMemberId);
        Assert.AreEqual(150.50m, result.Amount);
        Assert.AreEqual("Groceries split", result.Description);
        Assert.AreEqual(DebtSettlementStatus.Pending, result.Status);
    }

    [TestMethod]
    public async Task CreateDebtAsync_ThrowsWhenDebtorAndCreditorAreSame()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var member = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member" };
        _context.HouseholdMembers.Add(member);
        await _context.SaveChangesAsync();

        // Act & Assert
        try
        {
            await _householdService.CreateDebtAsync(
                household.HouseholdId,
                member.HouseholdMemberId,
                member.HouseholdMemberId,
                100m);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task CreateDebtAsync_ThrowsWhenAmountIsZeroOrNegative()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var debtor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Debtor" };
        var creditor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Creditor" };
        _context.HouseholdMembers.AddRange(debtor, creditor);
        await _context.SaveChangesAsync();

        // Act & Assert
        try
        {
            await _householdService.CreateDebtAsync(
                household.HouseholdId,
                debtor.HouseholdMemberId,
                creditor.HouseholdMemberId,
                0m);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task SettleDebtAsync_SettlesDebtSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var debtor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Debtor" };
        var creditor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Creditor" };
        _context.HouseholdMembers.AddRange(debtor, creditor);
        await _context.SaveChangesAsync();

        var debt = await _householdService.CreateDebtAsync(
            household.HouseholdId,
            debtor.HouseholdMemberId,
            creditor.HouseholdMemberId,
            100m);

        // Act
        var result = await _householdService.SettleDebtAsync(debt.DebtSettlementId, "Paid via Swish");

        // Assert
        Assert.IsTrue(result);
        var settledDebt = await _context.DebtSettlements.FindAsync(debt.DebtSettlementId);
        Assert.IsNotNull(settledDebt);
        Assert.AreEqual(DebtSettlementStatus.Settled, settledDebt.Status);
        Assert.IsNotNull(settledDebt.SettledDate);
        Assert.AreEqual("Paid via Swish", settledDebt.SettlementNote);
    }

    [TestMethod]
    public async Task CancelDebtAsync_CancelsDebtSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var debtor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Debtor" };
        var creditor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Creditor" };
        _context.HouseholdMembers.AddRange(debtor, creditor);
        await _context.SaveChangesAsync();

        var debt = await _householdService.CreateDebtAsync(
            household.HouseholdId,
            debtor.HouseholdMemberId,
            creditor.HouseholdMemberId,
            100m);

        // Act
        var result = await _householdService.CancelDebtAsync(debt.DebtSettlementId);

        // Assert
        Assert.IsTrue(result);
        var cancelledDebt = await _context.DebtSettlements.FindAsync(debt.DebtSettlementId);
        Assert.IsNotNull(cancelledDebt);
        Assert.AreEqual(DebtSettlementStatus.Cancelled, cancelledDebt.Status);
    }

    [TestMethod]
    public async Task GetHouseholdDebtsAsync_ReturnsAllDebts()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var debtor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Debtor" };
        var creditor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Creditor" };
        _context.HouseholdMembers.AddRange(debtor, creditor);
        await _context.SaveChangesAsync();

        await _householdService.CreateDebtAsync(household.HouseholdId, debtor.HouseholdMemberId, creditor.HouseholdMemberId, 100m);
        await _householdService.CreateDebtAsync(household.HouseholdId, debtor.HouseholdMemberId, creditor.HouseholdMemberId, 50m);

        // Act
        var result = await _householdService.GetHouseholdDebtsAsync(household.HouseholdId);

        // Assert
        var debts = result.ToList();
        Assert.AreEqual(2, debts.Count);
    }

    [TestMethod]
    public async Task GetHouseholdDebtsAsync_FiltersDebtsCorrectly()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var debtor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Debtor" };
        var creditor = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Creditor" };
        _context.HouseholdMembers.AddRange(debtor, creditor);
        await _context.SaveChangesAsync();

        var debt1 = await _householdService.CreateDebtAsync(household.HouseholdId, debtor.HouseholdMemberId, creditor.HouseholdMemberId, 100m);
        var debt2 = await _householdService.CreateDebtAsync(household.HouseholdId, debtor.HouseholdMemberId, creditor.HouseholdMemberId, 50m);
        
        await _householdService.SettleDebtAsync(debt1.DebtSettlementId);

        // Act
        var pendingDebts = await _householdService.GetHouseholdDebtsAsync(household.HouseholdId, DebtSettlementStatus.Pending);
        var settledDebts = await _householdService.GetHouseholdDebtsAsync(household.HouseholdId, DebtSettlementStatus.Settled);

        // Assert
        Assert.AreEqual(1, pendingDebts.Count());
        Assert.AreEqual(1, settledDebts.Count());
    }

    [TestMethod]
    public async Task GetMemberDebtBalanceAsync_CalculatesBalancesCorrectly()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var member1 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 1" };
        var member2 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 2" };
        var member3 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 3" };
        _context.HouseholdMembers.AddRange(member1, member2, member3);
        await _context.SaveChangesAsync();

        // Member1 owes Member2 100
        await _householdService.CreateDebtAsync(household.HouseholdId, member1.HouseholdMemberId, member2.HouseholdMemberId, 100m);
        // Member1 owes Member3 50
        await _householdService.CreateDebtAsync(household.HouseholdId, member1.HouseholdMemberId, member3.HouseholdMemberId, 50m);
        // Member2 owes Member3 30
        await _householdService.CreateDebtAsync(household.HouseholdId, member2.HouseholdMemberId, member3.HouseholdMemberId, 30m);

        // Act
        var balances = await _householdService.GetMemberDebtBalanceAsync(household.HouseholdId);

        // Assert
        Assert.AreEqual(-150m, balances[member1.HouseholdMemberId]); // Owes 150 total
        Assert.AreEqual(70m, balances[member2.HouseholdMemberId]);   // Is owed 100, owes 30 = +70
        Assert.AreEqual(80m, balances[member3.HouseholdMemberId]);   // Is owed 50 + 30 = +80
    }

    [TestMethod]
    public async Task CalculateOptimalSettlementAsync_CreatesOptimalSettlements()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var member1 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 1" };
        var member2 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 2" };
        var member3 = new HouseholdMember { HouseholdId = household.HouseholdId, Name = "Member 3" };
        _context.HouseholdMembers.AddRange(member1, member2, member3);
        await _context.SaveChangesAsync();

        // Member1 owes Member2 100
        await _householdService.CreateDebtAsync(household.HouseholdId, member1.HouseholdMemberId, member2.HouseholdMemberId, 100m);
        // Member3 owes Member2 50
        await _householdService.CreateDebtAsync(household.HouseholdId, member3.HouseholdMemberId, member2.HouseholdMemberId, 50m);

        // Act
        var settlements = await _householdService.CalculateOptimalSettlementAsync(household.HouseholdId);

        // Assert
        var settlementList = settlements.ToList();
        Assert.AreNotEqual(0, settlementList.Count());
        
        // Verify total amounts balance out
        var totalDebts = settlementList.Sum(s => s.Amount);
        Assert.IsTrue(totalDebts > 0); // At least some settlements were created
    }

    [TestMethod]
    public async Task CalculateOptimalSettlementAsync_HandlesEmptyDebts()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        // Act
        var settlements = await _householdService.CalculateOptimalSettlementAsync(household.HouseholdId);

        // Assert
        Assert.AreEqual(0, settlements.Count());
    }

    #endregion
}
