using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class GoalsController : ControllerBase
{
    private readonly IGoalService _goalService;
    private readonly ILogger<GoalsController> _logger;

    public GoalsController(IGoalService goalService, ILogger<GoalsController> logger)
    {
        _goalService = goalService;
        _logger = logger;
    }

    /// <summary>
    /// Lista sparmål
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Goal>>> GetGoals()
    {
        try
        {
            var goals = await _goalService.GetAllGoalsAsync();
            return Ok(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving goals");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta specifikt sparmål
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Goal>> GetGoal(int id)
    {
        try
        {
            var goal = await _goalService.GetGoalByIdAsync(id);
            if (goal == null)
            {
                return NotFound();
            }
            return Ok(goal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving goal {GoalId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta aktiva sparmål
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Goal>>> GetActiveGoals()
    {
        try
        {
            var goals = await _goalService.GetActiveGoalsAsync();
            return Ok(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active goals");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta total framsteg för alla aktiva sparmål
    /// </summary>
    [HttpGet("progress")]
    public async Task<ActionResult<object>> GetTotalProgress()
    {
        try
        {
            var progress = await _goalService.GetTotalProgress();
            return Ok(new { progress });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total progress");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Skapa sparmål
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Goal>> CreateGoal(Goal goal)
    {
        try
        {
            var createdGoal = await _goalService.CreateGoalAsync(goal);
            return CreatedAtAction(nameof(GetGoal), new { id = createdGoal.GoalId }, createdGoal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating goal");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Uppdatera sparmål
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGoal(int id, Goal goal)
    {
        if (id != goal.GoalId)
        {
            return BadRequest();
        }

        try
        {
            await _goalService.UpdateGoalAsync(goal);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating goal {GoalId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta bort sparmål
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGoal(int id)
    {
        try
        {
            await _goalService.DeleteGoalAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting goal {GoalId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
