using Microsoft.VisualStudio.TestTools.UnitTesting;
using Privatekonomi.Core.Services;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class KonsumentverketComparisonServiceTests
{
    private readonly KonsumentverketComparisonService _service;

    public KonsumentverketComparisonServiceTests()
    {
        _service = new KonsumentverketComparisonService();
    }

    [TestMethod]
    public void CalculateReferenceCosts_EmptyHousehold_ReturnsZeroCosts()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>();

        // Act
        var result = _service.CalculateReferenceCosts(members);

        // Assert
        Assert.AreEqual(0, result.ReferenceFoodCosts);
        Assert.AreEqual(0, result.ReferenceIndividualCosts);
        Assert.AreEqual(0, result.ReferenceCommonCosts);
        Assert.AreEqual(0, result.ReferenceTotalCosts);
    }

    [TestMethod]
    public void CalculateReferenceCosts_SingleAdult_ReturnsCorrectCosts()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = false }
        };

        // Act
        var result = _service.CalculateReferenceCosts(members);

        // Assert
        // Food cost for 31-60 years, all food at home: 3730 SEK
        Assert.AreEqual(3730, result.ReferenceFoodCosts);
        
        // Individual costs for 26-49 years: 800+590+120+580 = 2090 SEK
        Assert.AreEqual(2090, result.ReferenceIndividualCosts);
        
        // Household common costs for 1 person: 150+920+970+630+380+200+150 = 3400 SEK
        Assert.AreEqual(3400, result.ReferenceCommonCosts);
        
        // Total: 3730 + 2090 + 3400 = 9220 SEK
        Assert.AreEqual(9220, result.ReferenceTotalCosts);
    }

    [TestMethod]
    public void CalculateReferenceCosts_SingleAdultWithLunchOut_ReturnsCorrectCosts()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = true }
        };

        // Act
        var result = _service.CalculateReferenceCosts(members);

        // Assert
        // Food cost for 31-60 years, lunch out on weekdays: 2860 SEK
        Assert.AreEqual(2860, result.ReferenceFoodCosts);
        
        // Individual costs for 26-49 years: 2090 SEK
        Assert.AreEqual(2090, result.ReferenceIndividualCosts);
        
        // Household common costs for 1 person: 3400 SEK
        Assert.AreEqual(3400, result.ReferenceCommonCosts);
        
        // Total: 2860 + 2090 + 3400 = 8350 SEK
        Assert.AreEqual(8350, result.ReferenceTotalCosts);
    }

    [TestMethod]
    public void CalculateReferenceCosts_FamilyWithTwoAdultsAndTwoChildren_ReturnsCorrectCosts()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = true },
            new KonsumentverketHouseholdMember { Age = 38, HasLunchOutOnWeekdays = true },
            new KonsumentverketHouseholdMember { Age = 8, HasLunchOutOnWeekdays = true },
            new KonsumentverketHouseholdMember { Age = 5, HasLunchOutOnWeekdays = true }
        };

        // Act
        var result = _service.CalculateReferenceCosts(members);

        // Assert
        // Food costs with lunch out:
        // Adult 35: 2860 (31-60 years)
        // Adult 38: 2860 (31-60 years)
        // Child 8: 1830 (6-9 years)
        // Child 5: 1240 (2-5 years)
        // Total: 8790 SEK
        Assert.AreEqual(8790, result.ReferenceFoodCosts);
        
        // Individual costs:
        // Adult 35 (26-49): 2090
        // Adult 38 (26-49): 2090
        // Child 8 (7-10): 2020
        // Child 5 (4-6): 1960
        // Total: 8160 SEK
        Assert.AreEqual(8160, result.ReferenceIndividualCosts);
        
        // Household common costs for 4 persons: 360+1570+1620+710+830+790+250 = 6130 SEK
        Assert.AreEqual(6130, result.ReferenceCommonCosts);
        
        // Total: 8790 + 8160 + 6130 = 23080 SEK
        Assert.AreEqual(23080, result.ReferenceTotalCosts);
    }

    [TestMethod]
    public void CalculateReferenceCosts_LargeHousehold_CapsAt7Members()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>();
        for (int i = 0; i < 10; i++)
        {
            members.Add(new KonsumentverketHouseholdMember { Age = 30, HasLunchOutOnWeekdays = false });
        }

        // Act
        var result = _service.CalculateReferenceCosts(members);

        // Assert
        // Household common costs should use data for 7 persons (max in table)
        Assert.AreEqual(8670, result.ReferenceCommonCosts);
    }

    [TestMethod]
    public void CalculateReferenceCosts_InfantAndToddler_ReturnsCorrectCosts()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 0, HasLunchOutOnWeekdays = false },
            new KonsumentverketHouseholdMember { Age = 2, HasLunchOutOnWeekdays = false }
        };

        // Act
        var result = _service.CalculateReferenceCosts(members);

        // Assert
        // Food costs:
        // Infant 0 (6-11 months): 1050
        // Toddler 2 (2-5 years): 1610
        // Total: 2660 SEK
        Assert.AreEqual(2660, result.ReferenceFoodCosts);
        
        // Individual costs:
        // Infant 0 years: 2870
        // Toddler 1-3 years: 2670
        // Total: 5540 SEK
        Assert.AreEqual(5540, result.ReferenceIndividualCosts);
        
        // Household common costs for 2 persons: 4170 SEK
        Assert.AreEqual(4170, result.ReferenceCommonCosts);
    }

    [TestMethod]
    public void CompareWithReference_CalculatesCorrectDifferences()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = false }
        };

        var userCosts = new UserHouseholdCosts
        {
            FoodCosts = 4000,
            ClothesCosts = 1000,
            LeisureCosts = 500,
            MobileCosts = 150,
            HygieneCosts = 600,
            ConsumablesCosts = 200,
            HomeEquipmentCosts = 1000,
            InternetMobileCosts = 1000,
            OtherMediaCosts = 700,
            ElectricityCosts = 400,
            WaterDrainageCosts = 250,
            HomeInsuranceCosts = 200
        };

        // Act
        var result = _service.CompareWithReference(members, userCosts);

        // Assert
        Assert.IsNotNull(result.CategoryComparisons);
        Assert.IsTrue(result.CategoryComparisons.ContainsKey("Livsmedel"));
        
        var foodComparison = result.CategoryComparisons["Livsmedel"];
        Assert.AreEqual(4000, foodComparison.UserCost);
        Assert.AreEqual(3730, foodComparison.ReferenceCost);
        Assert.AreEqual(270, foodComparison.Difference);
        Assert.IsTrue(foodComparison.DifferencePercentage > 0);
    }

    [TestMethod]
    public void CompareWithReference_CalculatesNegativeDifference()
    {
        // Arrange
        var members = new List<KonsumentverketHouseholdMember>
        {
            new KonsumentverketHouseholdMember { Age = 35, HasLunchOutOnWeekdays = false }
        };

        var userCosts = new UserHouseholdCosts
        {
            FoodCosts = 3000 // Less than reference 3730
        };

        // Act
        var result = _service.CompareWithReference(members, userCosts);

        // Assert
        var foodComparison = result.CategoryComparisons["Livsmedel"];
        Assert.AreEqual(3000, foodComparison.UserCost);
        Assert.AreEqual(3730, foodComparison.ReferenceCost);
        Assert.AreEqual(-730, foodComparison.Difference);
        Assert.IsTrue(foodComparison.DifferencePercentage < 0);
    }

    [DataTestMethod]
    [DataRow(0, IndividualAgeGroup.ZeroYears)]
    [DataRow(1, IndividualAgeGroup.OneToThreeYears)]
    [DataRow(3, IndividualAgeGroup.OneToThreeYears)]
    [DataRow(5, IndividualAgeGroup.FourToSixYears)]
    [DataRow(10, IndividualAgeGroup.SevenToTenYears)]
    [DataRow(14, IndividualAgeGroup.ElevenToFourteenYears)]
    [DataRow(17, IndividualAgeGroup.FifteenToSeventeenYears)]
    [DataRow(25, IndividualAgeGroup.EighteenToTwentyFiveYears)]
    [DataRow(40, IndividualAgeGroup.TwentySixToFortyNineYears)]
    [DataRow(60, IndividualAgeGroup.FiftyToSixtyFourYears)]
    [DataRow(70, IndividualAgeGroup.SixtyFivePlusYears)]
    public void GetIndividualAgeGroup_CorrectMapping(int age, IndividualAgeGroup expectedGroup)
    {
        // Arrange
        var member = new KonsumentverketHouseholdMember { Age = age };

        // Act
        var ageGroup = member.GetIndividualAgeGroup();

        // Assert
        Assert.AreEqual(expectedGroup, ageGroup);
    }

    [DataTestMethod]
    [DataRow(0, AgeGroup.SixToElevenMonths)]
    [DataRow(1, AgeGroup.OneYear)]
    [DataRow(3, AgeGroup.TwoToFiveYears)]
    [DataRow(7, AgeGroup.SixToNineYears)]
    [DataRow(12, AgeGroup.TenToThirteenYears)]
    [DataRow(16, AgeGroup.FourteenToSeventeenYears)]
    [DataRow(25, AgeGroup.EighteenToThirtyYears)]
    [DataRow(45, AgeGroup.ThirtyOneToSixtyYears)]
    [DataRow(70, AgeGroup.SixtyOneToSeventyFourYears)]
    [DataRow(80, AgeGroup.SeventyFivePlusYears)]
    public void GetFoodAgeGroup_CorrectMapping(int age, AgeGroup expectedGroup)
    {
        // Arrange
        var member = new KonsumentverketHouseholdMember { Age = age };

        // Act
        var ageGroup = member.GetFoodAgeGroup();

        // Assert
        Assert.AreEqual(expectedGroup, ageGroup);
    }

    [TestMethod]
    public void GetAgeGroupLabel_ReturnsSwedishLabels()
    {
        // Act & Assert
        Assert.AreEqual("6-11 mån", _service.GetAgeGroupLabel(AgeGroup.SixToElevenMonths));
        Assert.AreEqual("1 år", _service.GetAgeGroupLabel(AgeGroup.OneYear));
        Assert.AreEqual("18-30 år", _service.GetAgeGroupLabel(AgeGroup.EighteenToThirtyYears));
        Assert.AreEqual("75 år -", _service.GetAgeGroupLabel(AgeGroup.SeventyFivePlusYears));
    }

    [TestMethod]
    public void GetIndividualAgeGroupLabel_ReturnsSwedishLabels()
    {
        // Act & Assert
        Assert.AreEqual("0 år", _service.GetIndividualAgeGroupLabel(IndividualAgeGroup.ZeroYears));
        Assert.AreEqual("1-3 år", _service.GetIndividualAgeGroupLabel(IndividualAgeGroup.OneToThreeYears));
        Assert.AreEqual("65 år -", _service.GetIndividualAgeGroupLabel(IndividualAgeGroup.SixtyFivePlusYears));
    }
}
