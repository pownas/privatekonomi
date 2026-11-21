using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Api.Exceptions;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private readonly IBillService _billService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BillsController> _logger;

    public BillsController(
        IBillService billService,
        ICurrentUserService currentUserService,
        ILogger<BillsController> logger)
    {
        _billService = billService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Hämta alla räkningar för inloggad användare
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bill>>> GetBills()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var bills = await _billService.GetBillsAsync(userId);
            return Ok(bills);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bills");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta en specifik räkning
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Bill>> GetBill(int id)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var bill = await _billService.GetBillByIdAsync(id, userId);
            if (bill == null)
            {
                throw new NotFoundException("Bill", id);
            }
            return Ok(bill);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bill {BillId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta väntande räkningar
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<Bill>>> GetPendingBills()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var bills = await _billService.GetPendingBillsAsync(userId);
            return Ok(bills);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending bills");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta försenade räkningar
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<Bill>>> GetOverdueBills()
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var bills = await _billService.GetOverdueBillsAsync(userId);
            return Ok(bills);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue bills");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta räkningar som förfaller snart
    /// </summary>
    [HttpGet("due-soon")]
    public async Task<ActionResult<IEnumerable<Bill>>> GetBillsDueSoon([FromQuery] int daysAhead = 7)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var bills = await _billService.GetBillsDueSoonAsync(userId, daysAhead);
            return Ok(bills);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bills due soon");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Skapa ny räkning
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Bill>> CreateBill(Bill bill)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            bill.UserId = userId;
            var created = await _billService.CreateBillAsync(bill);
            return CreatedAtAction(nameof(GetBill), new { id = created.BillId }, created);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bill");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Uppdatera en räkning
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBill(int id, Bill bill)
    {
        if (id != bill.BillId)
        {
            throw new BadRequestException("Bill ID in URL does not match bill ID in body");
        }

        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var existing = await _billService.GetBillByIdAsync(id, userId);
            if (existing == null)
            {
                throw new NotFoundException("Bill", id);
            }
            bill.UserId = userId;
            await _billService.UpdateBillAsync(bill);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bill {BillId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta bort en räkning
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBill(int id)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var existing = await _billService.GetBillByIdAsync(id, userId);
            if (existing == null)
            {
                throw new NotFoundException("Bill", id);
            }
            await _billService.DeleteBillAsync(id, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bill {BillId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Markera en räkning som betald
    /// </summary>
    [HttpPost("{id}/mark-paid")]
    public async Task<IActionResult> MarkBillAsPaid(int id, [FromQuery] DateTime? paidDate = null, [FromQuery] int? transactionId = null)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var existing = await _billService.GetBillByIdAsync(id, userId);
            if (existing == null)
            {
                throw new NotFoundException("Bill", id);
            }
            await _billService.MarkBillAsPaidAsync(id, paidDate ?? DateTime.UtcNow, transactionId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking bill {BillId} as paid", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Lägg till påminnelse för en räkning
    /// </summary>
    [HttpPost("{id}/reminders")]
    public async Task<IActionResult> AddReminder(int id, [FromBody] AddReminderRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var existing = await _billService.GetBillByIdAsync(id, userId);
            if (existing == null)
            {
                throw new NotFoundException("Bill", id);
            }
            await _billService.AddReminderAsync(id, request.ReminderDate, request.ReminderMethod ?? "Notification");
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reminder to bill {BillId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request model for adding a bill reminder
/// </summary>
public record AddReminderRequest(DateTime ReminderDate, string? ReminderMethod);
