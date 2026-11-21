using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class PocketsController : ControllerBase
{
    private readonly IPocketService _pocketService;
    private readonly ILogger<PocketsController> _logger;

    public PocketsController(IPocketService pocketService, ILogger<PocketsController> logger)
    {
        _pocketService = pocketService;
        _logger = logger;
    }

    /// <summary>
    /// Hämta alla fickor för användaren
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pocket>>> GetPockets()
    {
        try
        {
            var pockets = await _pocketService.GetAllPocketsAsync();
            return Ok(pockets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pockets");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta fickor för ett specifikt sparkonto
    /// </summary>
    [HttpGet("banksource/{bankSourceId}")]
    public async Task<ActionResult<IEnumerable<Pocket>>> GetPocketsByBankSource(int bankSourceId)
    {
        try
        {
            var pockets = await _pocketService.GetPocketsByBankSourceAsync(bankSourceId);
            return Ok(pockets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pockets for bank source {BankSourceId}", bankSourceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta en specifik ficka
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Pocket>> GetPocket(int id)
    {
        try
        {
            var pocket = await _pocketService.GetPocketByIdAsync(id);
            if (pocket == null)
            {
                return NotFound();
            }
            return Ok(pocket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pocket {PocketId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Skapa ny ficka
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Pocket>> CreatePocket(Pocket pocket)
    {
        try
        {
            var createdPocket = await _pocketService.CreatePocketAsync(pocket);
            return CreatedAtAction(nameof(GetPocket), new { id = createdPocket.PocketId }, createdPocket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pocket");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Uppdatera ficka
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePocket(int id, Pocket pocket)
    {
        if (id != pocket.PocketId)
        {
            return BadRequest();
        }

        try
        {
            await _pocketService.UpdatePocketAsync(pocket);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pocket {PocketId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta bort ficka
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePocket(int id)
    {
        try
        {
            await _pocketService.DeletePocketAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pocket {PocketId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Allokera pengar till ficka
    /// </summary>
    [HttpPost("{id}/allocate")]
    public async Task<ActionResult<Pocket>> AllocateMoney(int id, [FromBody] MoneyAllocationRequest request)
    {
        try
        {
            var pocket = await _pocketService.AllocateMoneyAsync(id, request.Amount, request.Description);
            return Ok(pocket);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error allocating money to pocket {PocketId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta ut pengar från ficka
    /// </summary>
    [HttpPost("{id}/withdraw")]
    public async Task<ActionResult<Pocket>> WithdrawMoney(int id, [FromBody] MoneyAllocationRequest request)
    {
        try
        {
            var pocket = await _pocketService.WithdrawMoneyAsync(id, request.Amount, request.Description);
            return Ok(pocket);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing money from pocket {PocketId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Överför pengar mellan fickor
    /// </summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> TransferMoney([FromBody] MoneyTransferRequest request)
    {
        try
        {
            await _pocketService.TransferMoneyAsync(request.FromPocketId, request.ToPocketId, request.Amount, request.Description);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring money between pockets");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta transaktioner för en ficka
    /// </summary>
    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<IEnumerable<PocketTransaction>>> GetTransactions(int id)
    {
        try
        {
            var transactions = await _pocketService.GetPocketTransactionsAsync(id);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for pocket {PocketId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta totalt allokerat belopp för ett sparkonto
    /// </summary>
    [HttpGet("banksource/{bankSourceId}/total")]
    public async Task<ActionResult<decimal>> GetTotalAllocated(int bankSourceId)
    {
        try
        {
            var total = await _pocketService.GetTotalAllocatedForBankSourceAsync(bankSourceId);
            return Ok(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving total allocated for bank source {BankSourceId}", bankSourceId);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class MoneyAllocationRequest
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class MoneyTransferRequest
{
    public int FromPocketId { get; set; }
    public int ToPocketId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
