using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;

namespace Privatekonomi.Core.Tests;

[TestClass]
public class StandardCategoryInitializerTests
{
    [TestMethod]
    public async Task EnsureCategoriesAsync_AddsDigitalPhoneAndDanceCategoriesIdempotently()
    {
        await using var context = new PrivatekonomyContext(new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        await context.Database.EnsureCreatedAsync();

        await StandardCategoryInitializer.EnsureCategoriesAsync(context);
        context.ChangeTracker.Clear();

        var digitalServices = await context.Categories.SingleAsync(category =>
            category.Name == "Digitala tjänster & abonnemang");
        var phone = await context.Categories.SingleAsync(category => category.Name == "Telefoni");
        var dance = await context.Categories.SingleAsync(category => category.Name == "Dans");

        Assert.AreEqual("6950", digitalServices.AccountNumber);
        Assert.AreEqual(9, digitalServices.ParentId);
        Assert.IsTrue(digitalServices.IsSystemCategory);
        Assert.AreEqual("4350", phone.AccountNumber);
        Assert.AreEqual(3, phone.ParentId);
        Assert.AreEqual("7350", dance.AccountNumber);
        Assert.AreEqual(4, dance.ParentId);

        await StandardCategoryInitializer.EnsureCategoriesAsync(context);

        Assert.AreEqual(3, await context.Categories.CountAsync(category =>
            category.Name == "Digitala tjänster & abonnemang" ||
            category.Name == "Telefoni" ||
            category.Name == "Dans"));
    }
}