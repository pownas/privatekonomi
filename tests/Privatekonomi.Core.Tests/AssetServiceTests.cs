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
}
