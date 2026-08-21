using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Exceptions;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(IBudgetService budgetService, ILogger<BudgetsController> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Budget>>> GetBudgets(
        [FromQuery] DateTime? periodStart,
        [FromQuery] DateTime? periodEnd)
    {
        var budgets = await _budgetService.GetAllBudgetsAsync();
        
        // Filter by period if provided
        if (periodStart.HasValue)
        {
            budgets = budgets.Where(b => b.EndDate >= periodStart.Value);
        }
        
        if (periodEnd.HasValue)
        {
            budgets = budgets.Where(b => b.StartDate <= periodEnd.Value);
        }
        
        return Ok(budgets);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Budget>> GetBudget(int id)
    {
        var budget = await _budgetService.GetBudgetByIdAsync(id);
        if (budget == null)
        {
            throw new NotFoundException("Budget", id);
        }
        return Ok(budget);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Budget>>> GetActiveBudgets()
    {
        var budgets = await _budgetService.GetActiveBudgetsAsync();
        return Ok(budgets);
    }

    [HttpGet("{id}/actual-amounts")]
    public async Task<ActionResult<Dictionary<int, decimal>>> GetActualAmounts(int id)
    {
        var actualAmounts = await _budgetService.GetActualAmountsByCategoryAsync(id);
        return Ok(actualAmounts);
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsForBudget(int id)
    {
        var transactions = await _budgetService.GetTransactionsForBudgetAsync(id);
        return Ok(transactions);
    }

    [HttpGet("{id}/transactions-by-category")]
    public async Task<ActionResult<Dictionary<int, List<Transaction>>>> GetTransactionsByCategoryForBudget(int id)
    {
        var transactions = await _budgetService.GetTransactionsByCategoryForBudgetAsync(id);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<Budget>> CreateBudget(Budget budget)
    {
        var createdBudget = await _budgetService.CreateBudgetAsync(budget);
        return CreatedAtAction(nameof(GetBudget), new { id = createdBudget.BudgetId }, createdBudget);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(int id, Budget budget)
    {
        if (id != budget.BudgetId)
        {
            throw new BadRequestException("Budget ID in URL does not match budget ID in body");
        }

        await _budgetService.UpdateBudgetAsync(budget);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(int id)
    {
        await _budgetService.DeleteBudgetAsync(id);
        return NoContent();
    }
}
