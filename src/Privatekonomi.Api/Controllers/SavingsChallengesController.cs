using Microsoft.AspNetCore.Mvc;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Controllers;

[LegacyApiDisabled]
[ApiController]
[Route("api/[controller]")]
public class SavingsChallengesController : ControllerBase
{
    private readonly ISavingsChallengeService _challengeService;
    private readonly ILogger<SavingsChallengesController> _logger;

    public SavingsChallengesController(ISavingsChallengeService challengeService, ILogger<SavingsChallengesController> logger)
    {
        _challengeService = challengeService;
        _logger = logger;
    }

    /// <summary>
    /// Hämta alla sparmåls-utmaningar
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SavingsChallenge>>> GetChallenges()
    {
        try
        {
            var challenges = await _challengeService.GetAllChallengesAsync();
            return Ok(challenges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving savings challenges");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta specifik sparmåls-utmaning
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SavingsChallenge>> GetChallenge(int id)
    {
        try
        {
            var challenge = await _challengeService.GetChallengeByIdAsync(id);
            if (challenge == null)
            {
                return NotFound();
            }
            return Ok(challenge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving savings challenge {ChallengeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta aktiva sparmåls-utmaningar
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<SavingsChallenge>>> GetActiveChallenges()
    {
        try
        {
            var challenges = await _challengeService.GetActiveChallengesAsync();
            return Ok(challenges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active savings challenges");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta genomförda sparmåls-utmaningar
    /// </summary>
    [HttpGet("completed")]
    public async Task<ActionResult<IEnumerable<SavingsChallenge>>> GetCompletedChallenges()
    {
        try
        {
            var challenges = await _challengeService.GetCompletedChallengesAsync();
            return Ok(challenges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving completed savings challenges");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta sparmåls-utmaningar efter typ
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<ActionResult<IEnumerable<SavingsChallenge>>> GetChallengesByType(ChallengeType type)
    {
        try
        {
            var challenges = await _challengeService.GetChallengesByTypeAsync(type);
            return Ok(challenges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving savings challenges by type {Type}", type);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Skapa sparmåls-utmaning
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SavingsChallenge>> CreateChallenge(SavingsChallenge challenge)
    {
        try
        {
            var createdChallenge = await _challengeService.CreateChallengeAsync(challenge);
            return CreatedAtAction(nameof(GetChallenge), new { id = createdChallenge.SavingsChallengeId }, createdChallenge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating savings challenge");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Uppdatera sparmåls-utmaning
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChallenge(int id, SavingsChallenge challenge)
    {
        if (id != challenge.SavingsChallengeId)
        {
            return BadRequest();
        }

        try
        {
            await _challengeService.UpdateChallengeAsync(challenge);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating savings challenge {ChallengeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Ta bort sparmåls-utmaning
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChallenge(int id)
    {
        try
        {
            await _challengeService.DeleteChallengeAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting savings challenge {ChallengeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Registrera framsteg för sparmåls-utmaning
    /// </summary>
    [HttpPost("{id}/progress")]
    public async Task<ActionResult<SavingsChallengeProgress>> RecordProgress(
        int id, 
        [FromBody] ProgressRequest request)
    {
        try
        {
            var progress = await _challengeService.RecordProgressAsync(
                id, 
                request.Date, 
                request.Completed, 
                request.AmountSaved, 
                request.Notes);
            return Ok(progress);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording progress for savings challenge {ChallengeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta framsteg för sparmåls-utmaning
    /// </summary>
    [HttpGet("{id}/progress")]
    public async Task<ActionResult<IEnumerable<SavingsChallengeProgress>>> GetChallengeProgress(int id)
    {
        try
        {
            var progress = await _challengeService.GetChallengeProgressAsync(id);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving progress for savings challenge {ChallengeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Uppdatera status för sparmåls-utmaning
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateChallengeStatus(int id, [FromBody] ChallengeStatus status)
    {
        try
        {
            await _challengeService.UpdateChallengeStatusAsync(id, status);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for savings challenge {ChallengeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta statistik för sparmåls-utmaningar
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetStatistics()
    {
        try
        {
            var stats = new
            {
                TotalActive = await _challengeService.GetTotalActiveChallengesAsync(),
                TotalCompleted = await _challengeService.GetTotalCompletedChallengesAsync(),
                TotalAmountSaved = await _challengeService.GetTotalAmountSavedAsync()
            };
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving savings challenge statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta alla tillgängliga utmanings-mallar
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<ChallengeTemplate>>> GetTemplates()
    {
        try
        {
            var templates = await _challengeService.GetAllTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving challenge templates");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Hämta specifik utmanings-mall
    /// </summary>
    [HttpGet("templates/{id}")]
    public async Task<ActionResult<ChallengeTemplate>> GetTemplate(int id)
    {
        try
        {
            var template = await _challengeService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound();
            }
            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving challenge template {TemplateId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Skapa sparmåls-utmaning från mall
    /// </summary>
    [HttpPost("templates/{id}/start")]
    public async Task<ActionResult<SavingsChallenge>> StartChallengeFromTemplate(int id)
    {
        try
        {
            var challenge = await _challengeService.CreateChallengeFromTemplateAsync(id);
            return CreatedAtAction(nameof(GetChallenge), new { id = challenge.SavingsChallengeId }, challenge);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message, templateId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating challenge from template {TemplateId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class ProgressRequest
{
    public DateTime Date { get; set; }
    public bool Completed { get; set; }
    public decimal AmountSaved { get; set; }
    public string? Notes { get; set; }
}
