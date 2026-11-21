using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IBankSourceService _bankSourceService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IBankSourceService bankSourceService, ILogger<AccountsController> logger)
    {
        _bankSourceService = bankSourceService;
        _logger = logger;
    }

    /// <summary>
    /// Lista konton för användaren (mapped from BankSource)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BankSource>>> GetAccounts()
    {
        try
        {
            var accounts = await _bankSourceService.GetAllBankSourcesAsync();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta konto (mapped from BankSource)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BankSource>> GetAccount(int id)
    {
        try
        {
            var account = await _bankSourceService.GetBankSourceByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {AccountId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Skapa konto (mapped from BankSource)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BankSource>> CreateAccount(BankSource account)
    {
        try
        {
            var createdAccount = await _bankSourceService.CreateBankSourceAsync(account);
            return CreatedAtAction(nameof(GetAccount), new { id = createdAccount.BankSourceId }, createdAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Uppdatera konto (mapped from BankSource)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAccount(int id, BankSource account)
    {
        if (id != account.BankSourceId)
        {
            return BadRequest();
        }

        try
        {
            await _bankSourceService.UpdateBankSourceAsync(account);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account {AccountId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta bort konto (soft-delete rekommenderas)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        try
        {
            await _bankSourceService.DeleteBankSourceAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account {AccountId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
