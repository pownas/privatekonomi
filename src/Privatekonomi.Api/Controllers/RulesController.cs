using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

/// <summary>
/// API controller for managing user categorization rules.
/// This provides an alias to the CategoryRulesController at /api/rules as per the specification.
/// </summary>
[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly ICategoryRuleService _categoryRuleService;

    public RulesController(ICategoryRuleService categoryRuleService)
    {
        _categoryRuleService = categoryRuleService;
    }

    /// <summary>
    /// Gets all categorization rules for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryRule>>> GetAllRules()
    {
        var rules = await _categoryRuleService.GetAllRulesAsync();
        return Ok(rules);
    }

    /// <summary>
    /// Gets only active categorization rules for the current user.
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<CategoryRule>>> GetActiveRules()
    {
        var rules = await _categoryRuleService.GetActiveRulesAsync();
        return Ok(rules);
    }

    /// <summary>
    /// Gets a specific categorization rule by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryRule>> GetRule(int id)
    {
        var rule = await _categoryRuleService.GetRuleByIdAsync(id);
        
        if (rule == null)
        {
            return NotFound();
        }
        
        return Ok(rule);
    }

    /// <summary>
    /// Creates a new user categorization rule.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CategoryRule>> CreateRule([FromBody] CategoryRule rule)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Ensure it's created as a user rule
        rule.RuleType = RuleType.User;
        
        var createdRule = await _categoryRuleService.CreateRuleAsync(rule);
        return CreatedAtAction(nameof(GetRule), new { id = createdRule.CategoryRuleId }, createdRule);
    }

    /// <summary>
    /// Updates an existing categorization rule.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryRule>> UpdateRule(int id, [FromBody] CategoryRule rule)
    {
        if (id != rule.CategoryRuleId)
        {
            return BadRequest("Rule ID mismatch");
        }
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var existingRule = await _categoryRuleService.GetRuleByIdAsync(id);
        if (existingRule == null)
        {
            return NotFound();
        }
        
        var updatedRule = await _categoryRuleService.UpdateRuleAsync(rule);
        return Ok(updatedRule);
    }

    /// <summary>
    /// Deletes a categorization rule.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var existingRule = await _categoryRuleService.GetRuleByIdAsync(id);
        if (existingRule == null)
        {
            return NotFound();
        }
        
        await _categoryRuleService.DeleteRuleAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Tests a pattern against a description to see which rule matches.
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<CategoryRule>> TestRule([FromBody] TestRuleRequest request)
    {
        if (string.IsNullOrEmpty(request.Description))
        {
            return BadRequest("Description is required");
        }
        
        var matchingRule = await _categoryRuleService.FindMatchingRuleAsync(
            request.Description, 
            request.Payee);
        
        if (matchingRule == null)
        {
            return NotFound(new { message = "No matching rule found" });
        }
        
        return Ok(matchingRule);
    }

    /// <summary>
    /// Applies categorization rules to multiple transactions in bulk.
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<Dictionary<int, int>>> ApplyRulesToTransactions(
        [FromBody] int[] transactionIds)
    {
        if (transactionIds == null || transactionIds.Length == 0)
        {
            return BadRequest("Transaction IDs are required");
        }
        
        var results = await _categoryRuleService.ApplyRulesToTransactionsAsync(transactionIds);
        return Ok(results);
    }
}
