using System.ComponentModel.DataAnnotations;

namespace Privatekonomi.Api.Models;

/// <summary>Credentials for requesting an API bearer token.</summary>
public sealed class ApiLoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(1024)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>A personal transaction owned by the authenticated user.</summary>
public sealed record PersonalTransactionResponse(
    int Id, decimal Amount, DateTime Date, string Description, bool IsIncome,
    string Currency, string? Payee, string? Notes);

/// <summary>A page of personal transactions ordered by descending ID.</summary>
public sealed record PersonalTransactionPage(
    IReadOnlyList<PersonalTransactionResponse> Transactions, int? NextBeforeId);
