using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class BankConnectionsController : ControllerBase
{
    private readonly IBankConnectionService _bankConnectionService;
    private readonly IOAuthStateService _oauthStateService;
    private readonly ILogger<BankConnectionsController> _logger;

    public BankConnectionsController(
        IBankConnectionService bankConnectionService,
        IOAuthStateService oauthStateService,
        ILogger<BankConnectionsController> logger)
    {
        _bankConnectionService = bankConnectionService;
        _oauthStateService = oauthStateService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available banks with API support
    /// </summary>
    [HttpGet("available-banks")]
    public ActionResult<List<string>> GetAvailableBanks()
    {
        try
        {
            var banks = _bankConnectionService.GetAvailableBanks();
            return Ok(banks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available banks");
            return StatusCode(500, new { error = "Ett fel uppstod vid hämtning av banker" });
        }
    }

    /// <summary>
    /// Gets all bank connections, optionally filtered by bank source
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BankConnection>>> GetConnections([FromQuery] int? bankSourceId = null)
    {
        try
        {
            var connections = await _bankConnectionService.GetConnectionsAsync(bankSourceId);
            return Ok(connections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank connections");
            return StatusCode(500, new { error = "Ett fel uppstod vid hämtning av bankkopplingar" });
        }
    }

    /// <summary>
    /// Gets a specific bank connection
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BankConnection>> GetConnection(int id)
    {
        try
        {
            var connection = await _bankConnectionService.GetConnectionAsync(id);
            if (connection == null)
                return NotFound(new { error = "Bankkoppling hittades inte" });

            return Ok(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank connection {Id}", id);
            return StatusCode(500, new { error = "Ett fel uppstod vid hämtning av bankkoppling" });
        }
    }

    /// <summary>
    /// Initiates OAuth authorization flow for a bank
    /// </summary>
    [HttpPost("authorize")]
    public async Task<ActionResult<AuthorizationResponse>> InitiateAuthorization([FromBody] AuthorizationRequest request)
    {
        try
        {
            var bankService = _bankConnectionService.GetBankApiService(request.BankName);
            if (bankService == null)
                return BadRequest(new { error = $"Bank '{request.BankName}' stöds inte" });

            var redirectUri = $"{Request.Scheme}://{Request.Host}/api/bankconnections/callback";
            
            // Generate and store state for CSRF protection
            var state = _oauthStateService.GenerateState(request.BankName);
            
            var authUrl = await bankService.GetAuthorizationUrlAsync(redirectUri, state);

            return Ok(new AuthorizationResponse
            {
                AuthorizationUrl = authUrl,
                State = state,
                RedirectUri = redirectUri
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating authorization for {BankName}", request.BankName);
            return StatusCode(500, new { error = "Ett fel uppstod vid initiering av auktorisering" });
        }
    }

    /// <summary>
    /// OAuth callback endpoint - exchanges authorization code for tokens
    /// </summary>
    [HttpGet("callback")]
    public IActionResult HandleCallback([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("OAuth callback received error: {Error}", error);
                return Redirect($"/bank-connections?error={Uri.EscapeDataString(error)}");
            }

            // TODO: Validate state parameter to prevent CSRF
            // In production, store state in session or cache and validate here

            if (string.IsNullOrEmpty(code))
                return BadRequest(new { error = "Authorization code saknas" });

            // Return to frontend with code for final processing
            return Redirect($"/bank-connections?code={code}&state={state}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OAuth callback");
            return Redirect("/bank-connections?error=callback_error");
        }
    }

    /// <summary>
    /// Completes the connection by exchanging code for tokens
    /// </summary>
    [HttpPost("connect")]
    public async Task<ActionResult<BankConnection>> CompleteConnection([FromBody] ConnectRequest request)
    {
        try
        {
            // Validate state to prevent CSRF attacks
            if (!_oauthStateService.ValidateState(request.State, request.BankName))
            {
                // Sanitize bank name by validating it against known banks before logging
                var sanitizedBankName = _bankConnectionService.GetAvailableBanks()
                    .Contains(request.BankName) ? request.BankName : "unknown";
                _logger.LogWarning("Invalid OAuth state received for bank {BankName}", sanitizedBankName);
                return BadRequest(new { error = "Ogiltig eller utgången state-parameter" });
            }

            var bankService = _bankConnectionService.GetBankApiService(request.BankName);
            if (bankService == null)
                return BadRequest(new { error = $"Bank '{request.BankName}' stöds inte" });

            // Exchange code for tokens
            var connection = await bankService.ExchangeCodeForTokenAsync(request.Code, request.RedirectUri);
            
            // Set bank source
            connection.BankSourceId = request.BankSourceId;

            // Save connection (tokens will be encrypted automatically)
            var savedConnection = await _bankConnectionService.CreateConnectionAsync(connection);

            // Remove used state token
            _oauthStateService.RemoveState(request.State);

            return Ok(savedConnection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing bank connection for {BankName}", request.BankName);
            return StatusCode(500, new { error = "Ett fel uppstod vid anslutning till banken" });
        }
    }

    /// <summary>
    /// Gets accounts available for a connection
    /// </summary>
    [HttpGet("{id}/accounts")]
    public async Task<ActionResult<List<BankApiAccount>>> GetAccounts(int id)
    {
        try
        {
            var connection = await _bankConnectionService.GetConnectionAsync(id);
            if (connection == null)
                return NotFound(new { error = "Bankkoppling hittades inte" });

            if (connection.BankSource == null)
                return BadRequest(new { error = "Bankkälla saknas för denna koppling" });

            var bankService = _bankConnectionService.GetBankApiService(connection.BankSource.Name);
            if (bankService == null)
                return BadRequest(new { error = "Ingen API-tjänst tillgänglig för denna bank" });

            var accounts = await bankService.GetAccountsAsync(connection);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts for connection {Id}", id);
            return StatusCode(500, new { error = "Ett fel uppstod vid hämtning av konton" });
        }
    }

    /// <summary>
    /// Imports transactions from a bank API
    /// </summary>
    [HttpPost("{id}/import")]
    public async Task<ActionResult<BankApiImportResult>> ImportTransactions(
        int id,
        [FromBody] ImportRequest request)
    {
        try
        {
            var fromDate = request.FromDate ?? DateTime.Now.AddDays(-90);
            var toDate = request.ToDate ?? DateTime.Now;

            var result = await _bankConnectionService.SyncTransactionsAsync(
                id,
                request.AccountId,
                fromDate,
                toDate,
                request.SkipDuplicates);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing transactions for connection {Id}", id);
            return StatusCode(500, new { error = $"Ett fel uppstod vid import: {ex.Message}" });
        }
    }

    /// <summary>
    /// Updates an existing bank connection
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<BankConnection>> UpdateConnection(int id, [FromBody] BankConnection connection)
    {
        try
        {
            if (id != connection.BankConnectionId)
                return BadRequest(new { error = "ID matchar inte" });

            var existing = await _bankConnectionService.GetConnectionAsync(id);
            if (existing == null)
                return NotFound(new { error = "Bankkoppling hittades inte" });

            var updated = await _bankConnectionService.UpdateConnectionAsync(connection);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bank connection {Id}", id);
            return StatusCode(500, new { error = "Ett fel uppstod vid uppdatering av bankkoppling" });
        }
    }

    /// <summary>
    /// Deletes a bank connection
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConnection(int id)
    {
        try
        {
            await _bankConnectionService.DeleteConnectionAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bank connection {Id}", id);
            return StatusCode(500, new { error = "Ett fel uppstod vid borttagning av bankkoppling" });
        }
    }

    // Request/Response DTOs
    public class AuthorizationRequest
    {
        public string BankName { get; set; } = string.Empty;
    }

    public class AuthorizationResponse
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }

    public class ConnectRequest
    {
        public string BankName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public int BankSourceId { get; set; }
    }

    public class ImportRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool SkipDuplicates { get; set; } = true;
    }
}
