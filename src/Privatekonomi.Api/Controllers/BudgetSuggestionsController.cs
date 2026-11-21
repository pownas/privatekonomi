using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Exceptions;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/budget-suggestions")]
public class BudgetSuggestionsController : ControllerBase
{
    private readonly IBudgetSuggestionService _suggestionService;
    private readonly ILogger<BudgetSuggestionsController> _logger;

    public BudgetSuggestionsController(
        IBudgetSuggestionService suggestionService,
        ILogger<BudgetSuggestionsController> logger)
    {
        _suggestionService = suggestionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all budget suggestions for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BudgetSuggestion>>> GetSuggestions()
    {
        var suggestions = await _suggestionService.GetAllSuggestionsAsync();
        return Ok(suggestions);
    }

    /// <summary>
    /// Get pending (not yet accepted) budget suggestions
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<BudgetSuggestion>>> GetPendingSuggestions()
    {
        var suggestions = await _suggestionService.GetPendingSuggestionsAsync();
        return Ok(suggestions);
    }

    /// <summary>
    /// Get a specific budget suggestion by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetSuggestion>> GetSuggestion(int id)
    {
        var suggestion = await _suggestionService.GetSuggestionByIdAsync(id);
        if (suggestion == null)
        {
            throw new NotFoundException("BudgetSuggestion", id);
        }
        return Ok(suggestion);
    }

    /// <summary>
    /// Get available distribution models with descriptions
    /// </summary>
    [HttpGet("models")]
    public ActionResult<IEnumerable<DistributionModelDto>> GetAvailableModels()
    {
        var models = _suggestionService.GetAvailableModels()
            .Select(m => new DistributionModelDto
            {
                Model = m.Model,
                Name = m.Name,
                Description = m.Description
            });
        return Ok(models);
    }

    /// <summary>
    /// Generate a new budget suggestion based on income and distribution model
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BudgetSuggestion>> CreateSuggestion([FromBody] CreateSuggestionRequest request)
    {
        if (request.TotalIncome <= 0)
        {
            throw new BadRequestException("Total income must be greater than zero");
        }

        var suggestion = await _suggestionService.GenerateSuggestionAsync(
            request.TotalIncome,
            request.DistributionModel,
            request.Name,
            request.HouseholdId);

        return CreatedAtAction(nameof(GetSuggestion), new { id = suggestion.BudgetSuggestionId }, suggestion);
    }

    /// <summary>
    /// Generate a suggestion based on transaction history
    /// </summary>
    [HttpPost("from-history")]
    public async Task<ActionResult<BudgetSuggestion>> CreateSuggestionFromHistory([FromBody] CreateSuggestionFromHistoryRequest request)
    {
        var suggestion = await _suggestionService.GenerateSuggestionFromHistoryAsync(
            request.DistributionModel,
            request.MonthsToAnalyze,
            request.HouseholdId);

        return CreatedAtAction(nameof(GetSuggestion), new { id = suggestion.BudgetSuggestionId }, suggestion);
    }

    /// <summary>
    /// Adjust a specific item in a suggestion
    /// </summary>
    [HttpPut("{id}/items/{categoryId}")]
    public async Task<ActionResult<BudgetSuggestionItem>> AdjustItem(
        int id, 
        int categoryId, 
        [FromBody] AdjustItemRequest request)
    {
        try
        {
            var item = await _suggestionService.AdjustSuggestionItemAsync(
                id,
                categoryId,
                request.NewAmount,
                request.Reason);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }
    }

    /// <summary>
    /// Transfer amount between two categories in a suggestion
    /// </summary>
    [HttpPost("{id}/transfer")]
    public async Task<IActionResult> TransferBetweenItems(int id, [FromBody] TransferRequest request)
    {
        try
        {
            await _suggestionService.TransferBetweenItemsAsync(
                id,
                request.FromCategoryId,
                request.ToCategoryId,
                request.Amount,
                request.Reason);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }
    }

    /// <summary>
    /// Calculate the effects of adjustments made to a suggestion
    /// </summary>
    [HttpGet("{id}/effects")]
    public async Task<ActionResult<BudgetSuggestionEffects>> GetEffects(int id)
    {
        try
        {
            var effects = await _suggestionService.CalculateEffectsAsync(id);
            return Ok(effects);
        }
        catch (InvalidOperationException)
        {
            throw new NotFoundException("BudgetSuggestion", id);
        }
    }

    /// <summary>
    /// Accept a suggestion and create a budget from it
    /// </summary>
    [HttpPost("{id}/accept")]
    public async Task<ActionResult<Budget>> AcceptSuggestion(int id, [FromBody] AcceptSuggestionRequest request)
    {
        try
        {
            var budget = await _suggestionService.AcceptSuggestionAsync(
                id,
                request.StartDate,
                request.EndDate,
                request.Period);
            return Ok(budget);
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }
    }

    /// <summary>
    /// Delete a budget suggestion
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSuggestion(int id)
    {
        await _suggestionService.DeleteSuggestionAsync(id);
        return NoContent();
    }
}

public class CreateSuggestionRequest
{
    public decimal TotalIncome { get; set; }
    public BudgetDistributionModel DistributionModel { get; set; }
    public string? Name { get; set; }
    public int? HouseholdId { get; set; }
}

public class CreateSuggestionFromHistoryRequest
{
    public BudgetDistributionModel DistributionModel { get; set; }
    public int MonthsToAnalyze { get; set; } = 3;
    public int? HouseholdId { get; set; }
}

public class AdjustItemRequest
{
    public decimal NewAmount { get; set; }
    public string? Reason { get; set; }
}

public class TransferRequest
{
    public int FromCategoryId { get; set; }
    public int ToCategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class AcceptSuggestionRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public BudgetPeriod Period { get; set; }
}

public class DistributionModelDto
{
    public BudgetDistributionModel Model { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
