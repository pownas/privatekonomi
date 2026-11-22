using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Data;

/// <summary>
/// Adds standard expense categories to databases created before those categories were introduced.
/// </summary>
public static class StandardCategoryInitializer
{
    private static readonly StandardCategoryDefinition[] StandardCategories =
    [
        new("Digitala tjänster & abonnemang", "6950", "#7E57C2", 9),
        new("Telefoni", "4350", "#29B6F6", 3),
        new("Dans", "7350", "#EC407A", 4)
    ];

    public static async Task EnsureCategoriesAsync(
        PrivatekonomyContext context,
        CancellationToken cancellationToken = default)
    {
        var existingCategories = await context.Categories
            .Select(category => new { category.Name, category.OriginalName })
            .ToListAsync(cancellationToken);
        var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in existingCategories)
        {
            existingNames.Add(category.Name);
            if (!string.IsNullOrWhiteSpace(category.OriginalName))
                existingNames.Add(category.OriginalName);
        }

        var parentIds = StandardCategories.Select(category => category.ParentId).Distinct().ToArray();
        var availableParentIds = await context.Categories
            .Where(category => parentIds.Contains(category.CategoryId))
            .Select(category => category.CategoryId)
            .ToHashSetAsync(cancellationToken);

        var addedCategory = false;
        foreach (var definition in StandardCategories)
        {
            if (!existingNames.Add(definition.Name))
                continue;

            context.Categories.Add(new Category
            {
                Name = definition.Name,
                AccountNumber = definition.AccountNumber,
                Color = definition.Color,
                ParentId = availableParentIds.Contains(definition.ParentId) ? definition.ParentId : null,
                IsSystemCategory = true,
                OriginalName = definition.Name,
                OriginalColor = definition.Color,
                OriginalAccountNumber = definition.AccountNumber,
                CreatedAt = DateTime.UtcNow
            });
            addedCategory = true;
        }

        if (addedCategory)
            await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record StandardCategoryDefinition(string Name, string AccountNumber, string Color, int ParentId);
}