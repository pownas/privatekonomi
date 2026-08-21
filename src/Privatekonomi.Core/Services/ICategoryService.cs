using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category> CreateCategoryAsync(Category category);
    Task<Category> UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(int id);
    Task<Category?> ResetSystemCategoryAsync(int categoryId);
    Task<IEnumerable<Transaction>> GetTransactionsByCategoryAsync(int categoryId, DateTime? from = null, DateTime? toDate = null);
    Task<CategoryStatistics> GetCategoryStatisticsAsync(int categoryId, int months);
    Task<IEnumerable<CategoryStatistics>> GetAllCategoryStatisticsAsync(int months);
}
