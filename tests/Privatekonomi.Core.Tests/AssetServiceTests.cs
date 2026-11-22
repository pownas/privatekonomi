using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests;

[TestClass]
public sealed class AssetServiceTests
{
    [TestMethod]
    public async Task GetAllAssetsAsyncOrdersAssetsWhenLegacyDecimalTextUsesDotUnderSwedishCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var swedishCulture = CultureInfo.GetCultureInfo("sv-SE");

        try
        {
            CultureInfo.CurrentCulture = swedishCulture;
            CultureInfo.CurrentUICulture = swedishCulture;

            var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            await using var context = new PrivatekonomyContext(options);
            await context.Database.OpenConnectionAsync();
            await context.Database.EnsureCreatedAsync();

            context.Assets.AddRange(
                new Asset
                {
                    Name = "Test property",
                    Type = "Fastighet",
                    PurchaseValue = 2000000m,
                    CurrentValue = 2400000m,
                    Currency = "SEK",
                    CreatedAt = DateTime.UtcNow,
                    ValidFrom = DateTime.UtcNow
                },
                new Asset
                {
                    Name = "Second property",
                    Type = "Fastighet",
                    PurchaseValue = 9000000m,
                    CurrentValue = 10000000m,
                    Currency = "SEK",
                    CreatedAt = DateTime.UtcNow,
                    ValidFrom = DateTime.UtcNow
                },
                new Asset
                {
                    Name = "Third property",
                    Type = "Fastighet",
                    PurchaseValue = 100000m,
                    CurrentValue = 120000m,
                    Currency = "SEK",
                    CreatedAt = DateTime.UtcNow,
                    ValidFrom = DateTime.UtcNow
                });
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Assets SET CurrentValue = {0} WHERE Name = {1}",
                "2400000.0",
                "Test property");
            context.ChangeTracker.Clear();

            var assets = (await new AssetService(context).GetAllAssetsAsync()).ToList();

            Assert.AreEqual(3, assets.Count);
            Assert.AreEqual(10000000m, assets[0].CurrentValue);
            Assert.AreEqual(2400000m, assets[1].CurrentValue);
            Assert.AreEqual(120000m, assets[2].CurrentValue);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [TestMethod]
    public async Task UpdateAssetAsyncUpdatesTrackedAssetWithoutAttachingDuplicate()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var context = new PrivatekonomyContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var original = new Asset
        {
            Name = "Bostaden",
            Type = "Fastighet",
            Description = "Hemma bostaden",
            PurchaseValue = 2_100_000m,
            CurrentValue = 2_400_000m,
            Currency = "EUR",
            CreatedAt = new DateTime(2025, 1, 17),
            ValidFrom = new DateTime(2025, 1, 17)
        };
        context.Assets.Add(original);
        await context.SaveChangesAsync();

        var service = new AssetService(context);
        var trackedAsset = await service.GetAssetByIdAsync(original.AssetId);
        var editedAsset = new Asset
        {
            AssetId = original.AssetId,
            Name = "Uppdaterad bostad",
            Type = "Fastighet",
            Description = "Uppdaterad beskrivning",
            PurchaseValue = 2_200_000m,
            CurrentValue = 2_500_000m,
            PurchaseDate = new DateTime(2025, 1, 17),
            Location = "Ny adress",
            ChartOfAccountsCode = "1110"
        };

        await service.UpdateAssetAsync(editedAsset);

        Assert.AreSame(trackedAsset, await service.GetAssetByIdAsync(original.AssetId));
        Assert.AreEqual("Uppdaterad bostad", trackedAsset!.Name);
        Assert.AreEqual(2_500_000m, trackedAsset.CurrentValue);
        Assert.AreEqual("EUR", trackedAsset.Currency);
        Assert.AreEqual(original.CreatedAt, trackedAsset.CreatedAt);
        Assert.AreEqual(original.ValidFrom, trackedAsset.ValidFrom);
        Assert.IsNotNull(trackedAsset.UpdatedAt);
    }

    [TestMethod]
    public async Task CreateAssetAsyncWorksAfterLoadingExistingAssets()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new PrivatekonomyContext(options);
        context.Assets.Add(new Asset
        {
            Name = "Bostaden",
            Type = "Fastighet",
            PurchaseValue = 2_100_000m,
            CurrentValue = 2_400_000m,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new AssetService(context);
        await service.GetAllAssetsAsync();

        Assert.AreEqual(0, context.ChangeTracker.Entries<Asset>().Count());

        var created = await service.CreateAssetAsync(new Asset
        {
            Name = "Ford Focus",
            Type = "Fordon",
            PurchaseValue = 190_000m,
            CurrentValue = 160_000m
        });

        Assert.AreNotEqual(0, created.AssetId);
        Assert.AreEqual(2, await context.Assets.CountAsync());
    }
}
