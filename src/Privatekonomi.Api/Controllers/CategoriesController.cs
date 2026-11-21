using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Exceptions;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }
        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        var createdCategory = await _categoryService.CreateCategoryAsync(category);
        return CreatedAtAction(nameof(GetCategory), new { id = createdCategory.CategoryId }, createdCategory);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, Category category)
    {
        if (id != category.CategoryId)
        {
            throw new BadRequestException("Category ID in URL does not match category ID in body");
        }

        await _categoryService.UpdateCategoryAsync(category);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _categoryService.DeleteCategoryAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/reset")]
    public async Task<ActionResult<Category>> ResetSystemCategory(int id)
    {
        var category = await _categoryService.ResetSystemCategoryAsync(id);
        if (category == null)
        {
            throw new NotFoundException("Category not found or is not a system category", id);
        }
        return Ok(category);
    }
}
