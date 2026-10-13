using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class HouseholdBalanceSheetServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly HouseholdBalanceSheetService _service;

    public HouseholdBalanceSheetServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new PrivatekonomyContext(options);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns("user-1");
        currentUser.SetupGet(service => service.IsAuthenticated).Returns(true);
        _service = new HouseholdBalanceSheetService(_context, currentUser.Object);
    }

    [TestMethod]
    public async Task SharedBalanceSheetIncludesOnlyActiveLinkedMembersOfEnabledHousehold()
    {
        _context.Households.AddRange(
            new Household
            {
                Name = "Shared",
                ShareBalanceSheet = true,
                Members =
                [
                    new HouseholdMember { Name = "One", UserId = "user-1", IsActive = true },
                    new HouseholdMember { Name = "Two", UserId = "user-2", IsActive = true },
                    new HouseholdMember { Name = "Former", UserId = "user-3", IsActive = false }
                ]
            },
            new Household
            {
                Name = "Private",
                ShareBalanceSheet = false,
                Members = [new HouseholdMember { Name = "Other", UserId = "user-4", IsActive = true }]
            });
        _context.Assets.AddRange(
            new Asset { Name = "Own", UserId = "user-1" },
            new Asset { Name = "Partner", UserId = "user-2" },
            new Asset { Name = "Former", UserId = "user-3" },
            new Asset { Name = "Unrelated", UserId = "user-4" },
            new Asset { Name = "Unlinked", UserId = null });
        await _context.SaveChangesAsync();

        var household = await _context.Households.FirstAsync(h => h.Name == "Shared");
        var available = await _service.GetAvailableSharedHouseholdsAsync();
        var data = await _service.GetSharedBalanceSheetAsync(household.HouseholdId);

        Assert.AreEqual(1, available.Count);
        Assert.IsNotNull(data);
        CollectionAssert.AreEquivalent(new[] { "Own", "Partner" }, data.Assets.Select(asset => asset.Name).ToArray());
    }

    [TestMethod]
    public async Task SharingCannotBeChangedByNonMember()
    {
        var household = new Household
        {
            Name = "Other",
            Members = [new HouseholdMember { Name = "Other", UserId = "user-4", IsActive = true }]
        };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        try
        {
            await _service.SetBalanceSheetSharingAsync(household.HouseholdId, true);
            Assert.Fail("Expected a non-member to be denied.");
        }
        catch (UnauthorizedAccessException)
        {
        }

        Assert.IsFalse((await _context.Households.FindAsync(household.HouseholdId))!.ShareBalanceSheet);
    }

    [TestMethod]
    public async Task DisabledSharingDoesNotExposeHouseholdRecords()
    {
        var household = new Household
        {
            Name = "Private",
            ShareBalanceSheet = false,
            Members = [new HouseholdMember { Name = "One", UserId = "user-1", IsActive = true }]
        };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        Assert.IsNull(await _service.GetSharedBalanceSheetAsync(household.HouseholdId));
        Assert.AreEqual(0, (await _service.GetAvailableSharedHouseholdsAsync()).Count);
    }

    [TestMethod]
    public async Task CurrentUserCanLinkOnlyAnUnlinkedMemberWithMatchingAccountEmail()
    {
        _context.Users.Add(new ApplicationUser { Id = "user-1", Email = "one@example.com", UserName = "one@example.com" });
        var household = new Household
        {
            Name = "Household",
            Members =
            [
                new HouseholdMember { Name = "One", Email = "ONE@example.com", IsActive = true },
                new HouseholdMember { Name = "Mismatch", Email = "other@example.com", IsActive = true }
            ]
        };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var member = household.Members.First();
        await _service.LinkCurrentUserToMemberAsync(household.HouseholdId, member.HouseholdMemberId);

        Assert.AreEqual("user-1", (await _context.HouseholdMembers.FindAsync(member.HouseholdMemberId))!.UserId);
        try
        {
            await _service.LinkCurrentUserToMemberAsync(household.HouseholdId, household.Members.Last().HouseholdMemberId);
            Assert.Fail("Expected an email mismatch to be denied.");
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
