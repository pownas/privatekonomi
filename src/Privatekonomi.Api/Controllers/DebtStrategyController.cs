using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class DebtStrategyController : ControllerBase
{
    private readonly IDebtStrategyService _debtStrategyService;
    private readonly ILoanService _loanService;

    public DebtStrategyController(IDebtStrategyService debtStrategyService, ILoanService loanService)
    {
        _debtStrategyService = debtStrategyService;
        _loanService = loanService;
    }

    /// <summary>
    /// Get amortization schedule for a specific loan
    /// </summary>
    /// <param name="loanId">Loan ID</param>
    /// <param name="extraMonthlyPayment">Extra monthly payment amount</param>
    [HttpGet("amortization-schedule/{loanId}")]
    public async Task<ActionResult<List<AmortizationScheduleEntry>>> GetAmortizationSchedule(
        int loanId, 
        [FromQuery] decimal extraMonthlyPayment = 0)
    {
        var loan = await _loanService.GetLoanByIdAsync(loanId);
        if (loan == null)
        {
            return NotFound("Loan not found");
        }

        var schedule = _debtStrategyService.GenerateAmortizationSchedule(loan, extraMonthlyPayment);
        return Ok(schedule);
    }

    /// <summary>
    /// Calculate snowball debt payoff strategy
    /// </summary>
    /// <param name="availableMonthlyPayment">Total available monthly payment amount</param>
    [HttpGet("snowball")]
    public async Task<ActionResult<DebtPayoffStrategy>> GetSnowballStrategy(
        [FromQuery] decimal availableMonthlyPayment)
    {
        if (availableMonthlyPayment <= 0)
        {
            return BadRequest("Available monthly payment must be greater than 0");
        }

        var strategy = await _debtStrategyService.CalculateSnowballStrategy(availableMonthlyPayment);
        return Ok(strategy);
    }

    /// <summary>
    /// Calculate avalanche debt payoff strategy
    /// </summary>
    /// <param name="availableMonthlyPayment">Total available monthly payment amount</param>
    [HttpGet("avalanche")]
    public async Task<ActionResult<DebtPayoffStrategy>> GetAvalancheStrategy(
        [FromQuery] decimal availableMonthlyPayment)
    {
        if (availableMonthlyPayment <= 0)
        {
            return BadRequest("Available monthly payment must be greater than 0");
        }

        var strategy = await _debtStrategyService.CalculateAvalancheStrategy(availableMonthlyPayment);
        return Ok(strategy);
    }

    /// <summary>
    /// Compare snowball and avalanche strategies
    /// </summary>
    /// <param name="availableMonthlyPayment">Total available monthly payment amount</param>
    [HttpGet("compare")]
    public async Task<ActionResult<object>> CompareStrategies(
        [FromQuery] decimal availableMonthlyPayment)
    {
        if (availableMonthlyPayment <= 0)
        {
            return BadRequest("Available monthly payment must be greater than 0");
        }

        var (snowball, avalanche) = await _debtStrategyService.CompareStrategies(availableMonthlyPayment);
        return Ok(new { Snowball = snowball, Avalanche = avalanche });
    }

    /// <summary>
    /// Analyze the impact of extra payments on a loan
    /// </summary>
    /// <param name="loanId">Loan ID</param>
    /// <param name="extraMonthlyPayment">Extra monthly payment amount</param>
    [HttpGet("extra-payment-analysis/{loanId}")]
    public async Task<ActionResult<ExtraPaymentAnalysis>> AnalyzeExtraPayment(
        int loanId,
        [FromQuery] decimal extraMonthlyPayment)
    {
        if (extraMonthlyPayment <= 0)
        {
            return BadRequest("Extra monthly payment must be greater than 0");
        }

        var loan = await _loanService.GetLoanByIdAsync(loanId);
        if (loan == null)
        {
            return NotFound("Loan not found");
        }

        var analysis = _debtStrategyService.AnalyzeExtraPayment(loan, extraMonthlyPayment);
        return Ok(analysis);
    }

    /// <summary>
    /// Calculate debt-free date for all current loans
    /// </summary>
    [HttpGet("debt-free-date")]
    public async Task<ActionResult<DateTime?>> GetDebtFreeDate()
    {
        var debtFreeDate = await _debtStrategyService.CalculateDebtFreeDate();
        return Ok(new { DebtFreeDate = debtFreeDate });
    }
    
    /// <summary>
    /// Export amortization schedule to CSV
    /// </summary>
    /// <param name="loanId">Loan ID</param>
    /// <param name="extraMonthlyPayment">Extra monthly payment amount</param>
    [HttpGet("export-amortization-schedule/{loanId}")]
    public async Task<IActionResult> ExportAmortizationSchedule(
        int loanId,
        [FromQuery] decimal extraMonthlyPayment = 0)
    {
        var loan = await _loanService.GetLoanByIdAsync(loanId);
        if (loan == null)
        {
            return NotFound("Loan not found");
        }

        var csv = _debtStrategyService.ExportAmortizationScheduleToCsv(loan, extraMonthlyPayment);
        var fileName = $"amorteringsplan_{loan.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv";
        
        return File(csv, "text/csv", fileName);
    }
    
    /// <summary>
    /// Export debt payoff strategy to CSV
    /// </summary>
    /// <param name="strategyType">Strategy type: "snowball" or "avalanche"</param>
    /// <param name="availableMonthlyPayment">Total available monthly payment amount</param>
    [HttpGet("export-strategy")]
    public async Task<IActionResult> ExportStrategy(
        [FromQuery] string strategyType,
        [FromQuery] decimal availableMonthlyPayment)
    {
        if (string.IsNullOrWhiteSpace(strategyType))
        {
            return BadRequest("Strategy type is required. Use 'snowball' or 'avalanche'");
        }

        if (availableMonthlyPayment <= 0)
        {
            return BadRequest("Available monthly payment must be greater than 0");
        }

        DebtPayoffStrategy strategy;
        if (string.Equals(strategyType, "snowball", StringComparison.OrdinalIgnoreCase))
        {
            strategy = await _debtStrategyService.CalculateSnowballStrategy(availableMonthlyPayment);
        }
        else if (string.Equals(strategyType, "avalanche", StringComparison.OrdinalIgnoreCase))
        {
            strategy = await _debtStrategyService.CalculateAvalancheStrategy(availableMonthlyPayment);
        }
        else
        {
            return BadRequest("Invalid strategy type. Use 'snowball' or 'avalanche'");
        }

        var loans = (await _loanService.GetAllLoansAsync()).ToList();
        var csv = _debtStrategyService.ExportStrategyToCsv(strategy, loans);
        var fileName = $"avbetalningsstrategi_{strategyType}_{DateTime.Now:yyyyMMdd}.csv";
        
        return File(csv, "text/csv", fileName);
    }
    
    /// <summary>
    /// Get detailed debt payoff strategy with month-by-month breakdown
    /// </summary>
    /// <param name="strategyType">Strategy type: "snowball" or "avalanche"</param>
    /// <param name="availableMonthlyPayment">Total available monthly payment amount</param>
    [HttpGet("detailed-strategy")]
    public async Task<ActionResult<DetailedDebtPayoffStrategy>> GetDetailedStrategy(
        [FromQuery] string strategyType,
        [FromQuery] decimal availableMonthlyPayment)
    {
        if (string.IsNullOrWhiteSpace(strategyType))
        {
            return BadRequest("Strategy type is required. Use 'snowball' or 'avalanche'");
        }

        if (availableMonthlyPayment <= 0)
        {
            return BadRequest("Available monthly payment must be greater than 0");
        }

        if (!string.Equals(strategyType, "snowball", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(strategyType, "avalanche", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid strategy type. Use 'snowball' or 'avalanche'");
        }

        var strategy = await _debtStrategyService.GenerateDetailedStrategy(strategyType, availableMonthlyPayment);
        return Ok(strategy);
    }
}
