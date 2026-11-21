using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Api.Models;
using Privatekonomi.Api.Services;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Api.Controllers;

[ApiController]
[Route("api/personal/transactions")]
[Authorize(AuthenticationSchemes = ApiAuthentication.Scheme)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PersonalTransactionsController(
    PrivatekonomyContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PersonalTransactionPage>> GetAll(
        [FromQuery] int limit = 50, [FromQuery] int? beforeId = null, CancellationToken cancellationToken = default)
    {
        var userId = await GetValidUserIdAsync();
        if (userId == null)
            return Unauthorized();
        if (limit is < 1 or > 100 || beforeId is <= 0)
            return BadRequest();

        var query = context.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.HouseholdId == null);
        if (beforeId.HasValue)
            query = query.Where(t => t.TransactionId < beforeId.Value);

        var entries = new List<PersonalTransactionResponse>();
        await foreach (var transaction in query.OrderByDescending(t => t.TransactionId)
            .AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            // Some database collations consider distinct user IDs equal.
            if (!string.Equals(transaction.UserId, userId, StringComparison.Ordinal))
                continue;
            entries.Add(ToResponse(transaction));
            if (entries.Count > limit)
                break;
        }

        var hasMore = entries.Count > limit;
        if (hasMore)
            entries.RemoveAt(entries.Count - 1);
        return Ok(new PersonalTransactionPage(entries, hasMore ? entries[^1].Id : null));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PersonalTransactionResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = await GetValidUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var transaction = await context.Transactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TransactionId == id && t.UserId == userId && t.HouseholdId == null, cancellationToken);
        if (transaction == null || !string.Equals(transaction.UserId, userId, StringComparison.Ordinal))
            return NotFound();

        return Ok(ToResponse(transaction));
    }

    private async Task<string?> GetValidUserIdAsync()
    {
        if (!Request.IsHttps)
            return null;

        var ids = User.FindAll(ClaimTypes.NameIdentifier).Select(c => c.Value).ToArray();
        if (ids.Length != 1 || string.IsNullOrWhiteSpace(ids[0]))
            return null;

        var user = await signInManager.ValidateSecurityStampAsync(User);
        if (user == null || !string.Equals(user.Id, ids[0], StringComparison.Ordinal)
            || !await signInManager.CanSignInAsync(user)
            || await users.IsLockedOutAsync(user)
            || await users.GetTwoFactorEnabledAsync(user))
            return null;

        return user.Id;
    }

    private static PersonalTransactionResponse ToResponse(Transaction transaction) => new(
        transaction.TransactionId, transaction.Amount, transaction.Date, transaction.Description,
        transaction.IsIncome, transaction.Currency, transaction.Payee, transaction.Notes);
}
