using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[Authorize]
[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class SalaryHistoryController : ControllerBase
{
    private readonly ISalaryHistoryService _salaryHistoryService;
    private readonly ILogger<SalaryHistoryController> _logger;

    public SalaryHistoryController(ISalaryHistoryService salaryHistoryService, ILogger<SalaryHistoryController> logger)
    {
        _salaryHistoryService = salaryHistoryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalaryHistory>>> GetAll()
    {
        try
        {
            var salaries = await _salaryHistoryService.GetAllSalaryHistoriesAsync(string.Empty);
            return Ok(salaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary histories");
            return StatusCode(500, "An error occurred while retrieving salary histories");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SalaryHistory>> GetById(int id)
    {
        try
        {
            var salary = await _salaryHistoryService.GetSalaryHistoryByIdAsync(id, string.Empty);
            
            if (salary == null)
                return NotFound();
            
            return Ok(salary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary history {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the salary history");
        }
    }

    [HttpGet("current")]
    public async Task<ActionResult<SalaryHistory>> GetCurrent()
    {
        try
        {
            var salary = await _salaryHistoryService.GetCurrentSalaryAsync(string.Empty);
            
            if (salary == null)
                return NotFound();
            
            return Ok(salary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current salary");
            return StatusCode(500, "An error occurred while retrieving the current salary");
        }
    }

    [HttpGet("period")]
    public async Task<ActionResult<IEnumerable<SalaryHistory>>> GetByPeriod(
        [FromQuery] DateTime startPeriod,
        [FromQuery] DateTime endPeriod)
    {
        try
        {
            var salaries = await _salaryHistoryService.GetSalaryHistoriesByPeriodAsync(string.Empty, startPeriod, endPeriod);
            return Ok(salaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary histories by period");
            return StatusCode(500, "An error occurred while retrieving salary histories");
        }
    }

    [HttpGet("average")]
    public async Task<ActionResult<decimal>> GetAverage([FromQuery] int months = 12)
    {
        try
        {
            var average = await _salaryHistoryService.GetAverageSalaryAsync(string.Empty, months);
            return Ok(new { averageSalary = average, months });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating average salary");
            return StatusCode(500, "An error occurred while calculating average salary");
        }
    }

    [HttpGet("growth")]
    public async Task<ActionResult<decimal>> GetGrowth([FromQuery] int months = 12)
    {
        try
        {
            var growth = await _salaryHistoryService.GetSalaryGrowthPercentageAsync(string.Empty, months);
            return Ok(new { growthPercentage = growth, months });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating salary growth");
            return StatusCode(500, "An error occurred while calculating salary growth");
        }
    }

    [HttpPost]
    public async Task<ActionResult<SalaryHistory>> Create([FromBody] SalaryHistory salaryHistory)
    {
        try
        {
            var created = await _salaryHistoryService.AddSalaryHistoryAsync(salaryHistory);
            return CreatedAtAction(nameof(GetById), new { id = created.SalaryHistoryId }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary history");
            return StatusCode(500, "An error occurred while creating the salary history");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SalaryHistory>> Update(int id, [FromBody] SalaryHistory salaryHistory)
    {
        try
        {
            // Verify ownership
            var existing = await _salaryHistoryService.GetSalaryHistoryByIdAsync(id, string.Empty);
            if (existing == null)
                return NotFound();
            
            salaryHistory.SalaryHistoryId = id;
            
            var updated = await _salaryHistoryService.UpdateSalaryHistoryAsync(salaryHistory);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary history {Id}", id);
            return StatusCode(500, "An error occurred while updating the salary history");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            // Verify ownership
            var existing = await _salaryHistoryService.GetSalaryHistoryByIdAsync(id, string.Empty);
            if (existing == null)
                return NotFound();
            
            await _salaryHistoryService.DeleteSalaryHistoryAsync(id, string.Empty);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting salary history {Id}", id);
            return StatusCode(500, "An error occurred while deleting the salary history");
        }
    }
}
