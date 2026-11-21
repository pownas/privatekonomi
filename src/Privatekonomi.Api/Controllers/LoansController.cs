using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Exceptions;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Loan>>> GetLoans()
    {
        var loans = await _loanService.GetAllLoansAsync();
        return Ok(loans);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Loan>> GetLoan(int id)
    {
        var loan = await _loanService.GetLoanByIdAsync(id);
        if (loan == null)
        {
            throw new NotFoundException("Loan", id);
        }
        return Ok(loan);
    }

    [HttpPost]
    public async Task<ActionResult<Loan>> CreateLoan([FromBody] Loan loan)
    {
        var createdLoan = await _loanService.CreateLoanAsync(loan);
        return CreatedAtAction(nameof(GetLoan), new { id = createdLoan.LoanId }, createdLoan);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Loan>> UpdateLoan(int id, [FromBody] Loan loan)
    {
        if (id != loan.LoanId)
        {
            throw new BadRequestException("Loan ID in URL does not match loan ID in body");
        }

        var updatedLoan = await _loanService.UpdateLoanAsync(loan);
        return Ok(updatedLoan);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLoan(int id)
    {
        await _loanService.DeleteLoanAsync(id);
        return NoContent();
    }
}
