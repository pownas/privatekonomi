using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Api.Models;
using Privatekonomi.Api.Tests.Infrastructure;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Privatekonomi.Api.Tests;

[TestClass]
public class TransactionsControllerTests
{
    private async Task<Transaction> CreateTestTransactionAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        var transaction = new Transaction
        {
            Amount = 100m,
            Date = DateTime.UtcNow.Date,
            Description = "Test Transaction",
            Payee = "Test Payee",
            Notes = "Test Notes",
            Tags = "test,tag",
            IsLocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        return transaction;
    }

    [TestMethod]
    public async Task UpdateTransaction_ValidRequest_ReturnsOkAndUpdatesTransaction()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var updateRequest = new UpdateTransactionRequest
        {
            Amount = 200m,
            Date = DateTime.UtcNow.Date.AddDays(1),
            Description = "Updated Description",
            Payee = "Updated Payee",
            Notes = "Updated Notes",
            Tags = "updated,tag",
            UpdatedAt = transaction.UpdatedAt
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}",
            updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var updatedTransaction = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.IsNotNull(updatedTransaction);
        Assert.AreEqual(200m, updatedTransaction.Amount);
        Assert.AreEqual("Updated Description", updatedTransaction.Description);
        Assert.AreEqual("Updated Payee", updatedTransaction.Payee);
        Assert.AreEqual("Updated Notes", updatedTransaction.Notes);
        Assert.AreEqual("updated,tag", updatedTransaction.Tags);
    }

    [TestMethod]
    public async Task UpdateTransaction_InvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var updateRequest = new UpdateTransactionRequest
        {
            Amount = 0m, // Invalid amount
            Date = DateTime.UtcNow.Date,
            Description = "Valid Description",
            UpdatedAt = transaction.UpdatedAt
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}",
            updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTransaction_EmptyDescription_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var updateRequest = new UpdateTransactionRequest
        {
            Amount = 100m,
            Date = DateTime.UtcNow.Date,
            Description = "", // Invalid description
            UpdatedAt = transaction.UpdatedAt
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}",
            updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTransaction_LockedTransaction_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        var transaction = new Transaction
        {
            Amount = 100m,
            Date = DateTime.UtcNow.Date,
            Description = "Test Transaction",
            IsLocked = true, // Transaction is locked
            CreatedAt = DateTime.UtcNow
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateTransactionRequest
        {
            Amount = 200m,
            Date = DateTime.UtcNow.Date,
            Description = "Updated Description"
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}",
            updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTransaction_ConcurrentModification_ReturnsConflict()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var updateRequest = new UpdateTransactionRequest
        {
            Amount = 200m,
            Date = DateTime.UtcNow.Date,
            Description = "Updated Description",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10) // Old timestamp
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}",
            updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTransaction_TransactionNotFound_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        
        var updateRequest = new UpdateTransactionRequest
        {
            Amount = 200m,
            Date = DateTime.UtcNow.Date,
            Description = "Updated Description"
        };

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/transactions/999999", // Non-existent ID
            updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTransaction_WithCategories_UpdatesCategoriesCorrectly()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        // Create categories
        var category1 = new Category { Name = "Category1" };
        var category2 = new Category { Name = "Category2" };
        context.Categories.AddRange(category1, category2);
        await context.SaveChangesAsync();

        // Create transaction
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var updateRequest = new UpdateTransactionRequest
        {
            Amount = 100m,
            Date = DateTime.UtcNow.Date,
            Description = "Test Transaction",
            Categories = new List<TransactionCategoryDto>
            {
                new() { CategoryId = category1.CategoryId, Amount = 60m },
                new() { CategoryId = category2.CategoryId, Amount = 40m }
            },
            UpdatedAt = transaction.UpdatedAt
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}",
            updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // Verify categories were updated in the database
        var updatedTransaction = await context.Transactions
            .Include(t => t.TransactionCategories)
            .FirstOrDefaultAsync(t => t.TransactionId == transaction.TransactionId);

        Assert.IsNotNull(updatedTransaction);
        Assert.AreEqual(2, updatedTransaction.TransactionCategories.Count);
        Assert.IsTrue(updatedTransaction.TransactionCategories.Any(tc => tc.CategoryId == category1.CategoryId && tc.Amount == 60m));
        Assert.IsTrue(updatedTransaction.TransactionCategories.Any(tc => tc.CategoryId == category2.CategoryId && tc.Amount == 40m));
    }

    [TestMethod]
    public async Task UpdateTransaction_WithoutOptimisticLocking_AllowsUpdate()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var updateRequest = new UpdateTransactionRequest
        {
            Amount = 200m,
            Date = DateTime.UtcNow.Date,
            Description = "Updated Description",
            // No UpdatedAt provided - optimistic locking is optional
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}",
            updateRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTransaction_DateTimePayload_NormalizesAndSerializesDateOnly()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_" + Guid.NewGuid());
        var client = factory.CreateClient();
        var transaction = await CreateTestTransactionAsync(factory.Services);

        const string requestBody = """
            {
              "amount": 200,
              "date": "2026-08-29T15:45:00Z",
              "description": "Updated Description",
              "updatedAt": null
            }
            """;

        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync($"/api/transactions/{transaction.TransactionId}", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(responseBody, "\"date\":\"2026-08-29\"");
        Assert.IsFalse(responseBody.Contains("T15:45:00Z", StringComparison.Ordinal));

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var updatedTransaction = await context.Transactions.FindAsync(transaction.TransactionId);
        Assert.IsNotNull(updatedTransaction);
        Assert.AreEqual(new DateTime(2026, 8, 29), updatedTransaction.Date);
        Assert.AreEqual(TimeSpan.Zero, updatedTransaction.Date.TimeOfDay);
    }

    [TestMethod]
    public async Task QuickCategorize_ValidRequest_ReturnsOkAndCategorizes()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_QuickCategorize_" + Guid.NewGuid());
        var client = factory.CreateClient();
        
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        // Create a category
        var category = new Category { Name = "Matvaror", Color = "#4CAF50" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        // Create transaction
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var request = new QuickCategorizeRequest
        {
            CategoryId = category.CategoryId,
            CreateRule = false
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}/categorize",
            request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QuickCategorizeResponse>();
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Transaction);
        Assert.IsNull(result.CreatedRule);
    }

    [TestMethod]
    public async Task QuickCategorize_WithCreateRule_CreatesRuleAndCategorizes()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_QuickCategorize_Rule_" + Guid.NewGuid());
        var client = factory.CreateClient();
        
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        // Create a category
        var category = new Category { Name = "Transport", Color = "#2196F3" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        // Create transaction
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var request = new QuickCategorizeRequest
        {
            CategoryId = category.CategoryId,
            CreateRule = true,
            RulePattern = "Test Payee"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}/categorize",
            request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QuickCategorizeResponse>();
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Transaction);
        Assert.IsNotNull(result.CreatedRule);
        Assert.AreEqual("Test Payee", result.CreatedRule!.Pattern);
        Assert.AreEqual(category.CategoryId, result.CreatedRule.CategoryId);
    }

    [TestMethod]
    public async Task QuickCategorize_TransactionNotFound_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_QuickCategorize_NotFound_" + Guid.NewGuid());
        var client = factory.CreateClient();
        
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();

        // Create a category
        var category = new Category { Name = "Underhållning", Color = "#9C27B0" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var request = new QuickCategorizeRequest
        {
            CategoryId = category.CategoryId,
            CreateRule = false
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/transactions/999999/categorize", // Non-existent ID
            request);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task QuickCategorize_CategoryNotFound_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new ApiWebApplicationFactory("Test_QuickCategorize_CategoryNotFound_" + Guid.NewGuid());
        var client = factory.CreateClient();
        
        // Create transaction
        var transaction = await CreateTestTransactionAsync(factory.Services);

        var request = new QuickCategorizeRequest
        {
            CategoryId = 999999, // Non-existent category
            CreateRule = false
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/transactions/{transaction.TransactionId}/categorize",
            request);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
