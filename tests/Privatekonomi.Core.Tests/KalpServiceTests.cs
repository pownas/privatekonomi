using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Services;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Data;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class KalpServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly Mock<IKonsumentverketComparisonService> _mockKonsumentverketService;
    private readonly KalpService _service;

    public KalpServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PrivatekonomyContext(options);

        _mockKonsumentverketService = new Mock<IKonsumentverketComparisonService>();
        _service = new KalpService(_context, _mockKonsumentverketService.Object);
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

    [TestMethod]
    public void CalculateKalp_BasicIncome_ReturnsCorrectCalculation()
    {
        // Arrange
        var input = new KalpInput
        {
            MonthlyIncome = 30000m,
            FixedExpenses = new Dictionary<string, decimal>
            {
                { "Hyra", 10000m },
                { "El", 500m }
            },
            Loans = new List<LoanPayment>
            {
                new LoanPayment { LoanName = "Bolån", LoanType = "Bolån", MonthlyPayment = 5000m }
            }
        };

        // Act
        var result = _service.CalculateKalp(input);

        // Assert
        Assert.AreEqual(30000m, result.TotalIncome);
        Assert.AreEqual(10500m, result.FixedExpenses);
        Assert.AreEqual(5000m, result.LoanPayments);
        Assert.AreEqual(14500m, result.KalpAmount); // 30000 - 10500 - 5000
        Assert.AreEqual(48.33m, Math.Round(result.KalpPercentage, 2)); // (14500/30000)*100
    }

    [TestMethod]
    public void CalculateKalp_NoFixedExpensesOrLoans_ReturnsFullIncome()
    {
        // Arrange
        var input = new KalpInput
        {
            MonthlyIncome = 25000m,
            FixedExpenses = new Dictionary<string, decimal>(),
            Loans = new List<LoanPayment>()
        };

        // Act
        var result = _service.CalculateKalp(input);

        // Assert
        Assert.AreEqual(25000m, result.TotalIncome);
        Assert.AreEqual(0m, result.FixedExpenses);
        Assert.AreEqual(0m, result.LoanPayments);
        Assert.AreEqual(25000m, result.KalpAmount);
        Assert.AreEqual(100m, result.KalpPercentage);
    }

    [TestMethod]
    public void CalculateKalp_ExpensesExceedIncome_ReturnsNegativeKalp()
    {
        // Arrange
        var input = new KalpInput
        {
            MonthlyIncome = 20000m,
            FixedExpenses = new Dictionary<string, decimal>
            {
                { "Hyra", 12000m },
                { "El", 800m }
            },
            Loans = new List<LoanPayment>
            {
                new LoanPayment { LoanName = "Lån", LoanType = "Privatlån", MonthlyPayment = 8000m }
            }
        };

        // Act
        var result = _service.CalculateKalp(input);

        // Assert
        Assert.AreEqual(20000m, result.TotalIncome);
        Assert.AreEqual(12800m, result.FixedExpenses);
        Assert.AreEqual(8000m, result.LoanPayments);
        Assert.AreEqual(-800m, result.KalpAmount); // 20000 - 12800 - 8000 = -800
    }

    [TestMethod]
    public void CalculateKalp_MultipleLoansOfSameType_AggregatesCorrectly()
    {
        // Arrange
        var input = new KalpInput
        {
            MonthlyIncome = 40000m,
            FixedExpenses = new Dictionary<string, decimal>
            {
                { "Hyra", 10000m }
            },
            Loans = new List<LoanPayment>
            {
                new LoanPayment { LoanName = "Bolån 1", LoanType = "Bolån", MonthlyPayment = 3000m },
                new LoanPayment { LoanName = "Bolån 2", LoanType = "Bolån", MonthlyPayment = 2000m },
                new LoanPayment { LoanName = "CSN", LoanType = "CSN-lån", MonthlyPayment = 1500m }
            }
        };

        // Act
        var result = _service.CalculateKalp(input);

        // Assert
        Assert.AreEqual(6500m, result.LoanPayments); // 3000 + 2000 + 1500
        Assert.AreEqual(23500m, result.KalpAmount); // 40000 - 10000 - 6500
        Assert.IsTrue(result.LoanPaymentBreakdown.ContainsKey("Bolån"));
        Assert.AreEqual(5000m, result.LoanPaymentBreakdown["Bolån"]); // 3000 + 2000
        Assert.AreEqual(1500m, result.LoanPaymentBreakdown["CSN-lån"]);
    }

    [TestMethod]
    public void CalculateKalp_WithHouseholdMembers_CalculatesRecommendedMinimum()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = false }
        };

        var input = new KalpInput
        {
            MonthlyIncome = 30000m,
            FixedExpenses = new Dictionary<string, decimal>
            {
                { "Hyra", 8000m }
            },
            Loans = new List<LoanPayment>(),
            HouseholdMembers = members
        };

        // Mock Konsumentverket service
        _mockKonsumentverketService
            .Setup(s => s.CalculateReferenceCosts(members))
            .Returns(new KonsumentverketComparisonResult
            {
                ReferenceFoodCosts = 3730m,
                ReferenceIndividualCosts = 2090m,
                ReferenceCommonCosts = 3400m
            });

        // Act
        var result = _service.CalculateKalp(input);

        // Assert
        Assert.IsNotNull(result.RecommendedMinimumKalp);
        Assert.AreEqual(5820m, result.RecommendedMinimumKalp); // Food (3730) + Individual (2090)
        Assert.IsTrue(result.MeetsRecommendedMinimum); // 22000 >= 5820
    }

    [TestMethod]
    public void CalculateKalp_BelowRecommendedMinimum_ReturnsFalse()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = false }
        };

        var input = new KalpInput
        {
            MonthlyIncome = 20000m,
            FixedExpenses = new Dictionary<string, decimal>
            {
                { "Hyra", 12000m }
            },
            Loans = new List<LoanPayment>
            {
                new LoanPayment { LoanName = "Lån", LoanType = "Privatlån", MonthlyPayment = 3000m }
            },
            HouseholdMembers = members
        };

        // Mock Konsumentverket service
        _mockKonsumentverketService
            .Setup(s => s.CalculateReferenceCosts(members))
            .Returns(new KonsumentverketComparisonResult
            {
                ReferenceFoodCosts = 3730m,
                ReferenceIndividualCosts = 2090m
            });

        // Act
        var result = _service.CalculateKalp(input);

        // Assert
        Assert.AreEqual(5000m, result.KalpAmount); // 20000 - 12000 - 3000
        Assert.AreEqual(5820m, result.RecommendedMinimumKalp);
        Assert.IsFalse(result.MeetsRecommendedMinimum); // 5000 < 5820
    }

    [TestMethod]
    public void CalculateKalp_FixedExpenseBreakdown_ContainsAllCategories()
    {
        // Arrange
        var input = new KalpInput
        {
            MonthlyIncome = 30000m,
            FixedExpenses = new Dictionary<string, decimal>
            {
                { "Hyra", 10000m },
                { "El", 500m },
                { "Internet", 300m },
                { "Försäkring", 200m }
            },
            Loans = new List<LoanPayment>()
        };

        // Act
        var result = _service.CalculateKalp(input);

        // Assert
        Assert.AreEqual(4, result.FixedExpenseBreakdown.Count);
        Assert.AreEqual(10000m, result.FixedExpenseBreakdown["Hyra"]);
        Assert.AreEqual(500m, result.FixedExpenseBreakdown["El"]);
        Assert.AreEqual(300m, result.FixedExpenseBreakdown["Internet"]);
        Assert.AreEqual(200m, result.FixedExpenseBreakdown["Försäkring"]);
    }

    [TestMethod]
    public void CalculateRecommendedMinimumKalp_EmptyHousehold_ReturnsZero()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>();

        // Act
        var result = _service.CalculateRecommendedMinimumKalp(members);

        // Assert
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void CalculateRecommendedMinimumKalp_SingleAdult_ReturnsCorrectAmount()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = false }
        };

        // Mock Konsumentverket service
        _mockKonsumentverketService
            .Setup(s => s.CalculateReferenceCosts(members))
            .Returns(new KonsumentverketComparisonResult
            {
                ReferenceFoodCosts = 3730m,
                ReferenceIndividualCosts = 2090m,
                ReferenceCommonCosts = 3400m
            });

        // Act
        var result = _service.CalculateRecommendedMinimumKalp(members);

        // Assert
        // Should be food + individual costs only (common costs are fixed expenses)
        Assert.AreEqual(5820m, result); // 3730 + 2090
    }

    [TestMethod]
    public void CalculateRecommendedMinimumKalp_FamilyOfFour_ReturnsCorrectAmount()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = true },
            new KonsumentverketHouseholdMember { Age = 38, HasLunchOutOnWeekdays = true },
            new KonsumentverketHouseholdMember { Age = 8, HasLunchOutOnWeekdays = true },
            new KonsumentverketHouseholdMember { Age = 5, HasLunchOutOnWeekdays = true }
        };

        // Mock Konsumentverket service
        _mockKonsumentverketService
            .Setup(s => s.CalculateReferenceCosts(members))
            .Returns(new KonsumentverketComparisonResult
            {
                ReferenceFoodCosts = 8790m,
                ReferenceIndividualCosts = 8160m,
                ReferenceCommonCosts = 6130m
            });

        // Act
        var result = _service.CalculateRecommendedMinimumKalp(members);

        // Assert
        Assert.AreEqual(16950m, result); // 8790 + 8160
    }

    [TestMethod]
    public void CalculateKalpWithComparison_WithoutHouseholdMembers_NoKonsumentverketComparison()
    {
        // Arrange
        var input = new KalpInput
        {
            MonthlyIncome = 30000m,
            FixedExpenses = new Dictionary<string, decimal> { { "Hyra", 10000m } },
            Loans = new List<LoanPayment>(),
            HouseholdMembers = null
        };

        // Act
        var result = _service.CalculateKalpWithComparison(input);

        // Assert
        Assert.IsNotNull(result.KalpCalculation);
        Assert.AreEqual(30000m, result.KalpCalculation.TotalIncome);
        Assert.IsNull(result.KonsumentverketComparison);
    }

    [TestMethod]
    public void CalculateKalpWithComparison_WithHouseholdMembers_IncludesKonsumentverketComparison()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = false }
        };

        var input = new KalpInput
        {
            MonthlyIncome = 30000m,
            FixedExpenses = new Dictionary<string, decimal> { { "Mat", 4000m } },
            Loans = new List<LoanPayment>(),
            HouseholdMembers = members
        };

        var mockComparisonResult = new KonsumentverketComparisonResult
        {
            ReferenceFoodCosts = 3730m,
            ReferenceIndividualCosts = 2090m
        };

        // Mock Konsumentverket service for both methods
        _mockKonsumentverketService
            .Setup(s => s.CalculateReferenceCosts(It.IsAny<List<KonsumentverketHouseholdMember>>()))
            .Returns(mockComparisonResult);
        
        _mockKonsumentverketService
            .Setup(s => s.CompareWithReference(It.IsAny<List<KonsumentverketHouseholdMember>>(), It.IsAny<UserHouseholdCosts>()))
            .Returns(mockComparisonResult);

        // Act
        var result = _service.CalculateKalpWithComparison(input);

        // Assert
        Assert.IsNotNull(result.KonsumentverketComparison);
        Assert.AreEqual(3730m, result.KonsumentverketComparison.ReferenceFoodCosts);
    }
}
