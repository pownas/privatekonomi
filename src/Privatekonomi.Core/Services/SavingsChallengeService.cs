using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public class SavingsChallengeService : ISavingsChallengeService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService? _currentUserService;

    public SavingsChallengeService(PrivatekonomyContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private IQueryable<SavingsChallenge> ApplyUserFilter(IQueryable<SavingsChallenge> query)
    {
        // Filter by current user if authenticated
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            query = query.Where(c => c.UserId == _currentUserService.UserId);
        }
        return query;
    }

    public async Task<IEnumerable<SavingsChallenge>> GetAllChallengesAsync()
    {
        var query = _context.SavingsChallenges
            .Include(c => c.ProgressEntries)
            .AsQueryable();

        query = ApplyUserFilter(query);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<SavingsChallenge?> GetChallengeByIdAsync(int id)
    {
        var query = _context.SavingsChallenges
            .Include(c => c.ProgressEntries)
            .AsQueryable();

        query = ApplyUserFilter(query);

        return await query.FirstOrDefaultAsync(c => c.SavingsChallengeId == id);
    }

    public async Task<SavingsChallenge> CreateChallengeAsync(SavingsChallenge challenge)
    {
        challenge.CreatedAt = DateTime.UtcNow;
        challenge.StartDate = challenge.StartDate == default ? DateTime.UtcNow : challenge.StartDate;
        challenge.EndDate = challenge.StartDate.AddDays(challenge.DurationDays);
        
        // Only set status to Active if not explicitly set
        if (challenge.Status == default)
        {
            challenge.Status = ChallengeStatus.Active;
        }
        
        // Set user ID for new challenges
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            challenge.UserId = _currentUserService.UserId;
        }
        
        _context.SavingsChallenges.Add(challenge);
        await _context.SaveChangesAsync();
        return challenge;
    }

    public async Task<SavingsChallenge> UpdateChallengeAsync(SavingsChallenge challenge)
    {
        // Verify ownership
        var existingChallenge = await GetChallengeByIdAsync(challenge.SavingsChallengeId);
        if (existingChallenge == null)
        {
            throw new InvalidOperationException($"Challenge with ID {challenge.SavingsChallengeId} not found or you don't have permission to update it.");
        }

        challenge.UpdatedAt = DateTime.UtcNow;
        _context.Entry(challenge).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return challenge;
    }

    public async Task DeleteChallengeAsync(int id)
    {
        // Verify ownership before deleting
        var challenge = await GetChallengeByIdAsync(id);
        if (challenge != null)
        {
            _context.SavingsChallenges.Remove(challenge);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<SavingsChallenge>> GetActiveChallengesAsync()
    {
        var query = _context.SavingsChallenges
            .Include(c => c.ProgressEntries)
            .Where(c => c.Status == ChallengeStatus.Active)
            .AsQueryable();

        query = ApplyUserFilter(query);

        return await query.OrderBy(c => c.EndDate).ToListAsync();
    }

    public async Task<IEnumerable<SavingsChallenge>> GetCompletedChallengesAsync()
    {
        var query = _context.SavingsChallenges
            .Include(c => c.ProgressEntries)
            .Where(c => c.Status == ChallengeStatus.Completed)
            .AsQueryable();

        query = ApplyUserFilter(query);

        return await query.OrderByDescending(c => c.UpdatedAt).ToListAsync();
    }

    public async Task<IEnumerable<SavingsChallenge>> GetChallengesByTypeAsync(ChallengeType type)
    {
        var query = _context.SavingsChallenges
            .Include(c => c.ProgressEntries)
            .Where(c => c.Type == type)
            .AsQueryable();

        query = ApplyUserFilter(query);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<SavingsChallengeProgress> RecordProgressAsync(int challengeId, DateTime progressDate, bool completed, decimal amountSaved, string? notes = null)
    {
        var challenge = await _context.SavingsChallenges
            .Include(c => c.ProgressEntries)
            .FirstOrDefaultAsync(c => c.SavingsChallengeId == challengeId);

        if (challenge == null)
        {
            throw new InvalidOperationException($"Challenge with ID {challengeId} not found.");
        }

        // Verify ownership
        if (_currentUserService?.IsAuthenticated == true && 
            _currentUserService.UserId != null && 
            challenge.UserId != _currentUserService.UserId)
        {
            throw new InvalidOperationException($"You don't have permission to update this challenge.");
        }

        // Check if progress for this date already exists
        var existingProgress = challenge.ProgressEntries.FirstOrDefault(p => p.Date.Date == progressDate.Date);
        
        if (existingProgress != null)
        {
            // Adjust the current amount by removing the old amount and adding the new amount
            challenge.CurrentAmount -= existingProgress.AmountSaved;
            challenge.CurrentAmount += amountSaved;
            
            // Update existing progress
            existingProgress.Completed = completed;
            existingProgress.AmountSaved = amountSaved;
            existingProgress.Notes = notes;
            _context.Entry(existingProgress).State = EntityState.Modified;
        }
        else
        {
            // Create new progress entry
            var progress = new SavingsChallengeProgress
            {
                SavingsChallengeId = challengeId,
                Date = progressDate.Date,
                Completed = completed,
                AmountSaved = amountSaved,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.SavingsChallengeProgress.Add(progress);
            existingProgress = progress;
            
            // Add the new amount
            challenge.CurrentAmount += amountSaved;
        }

        // Update challenge streak
        challenge.CurrentStreak = await CalculateCurrentStreakAsync(challengeId);
        
        if (challenge.CurrentStreak > challenge.BestStreak)
        {
            challenge.BestStreak = challenge.CurrentStreak;
        }

        // Check if challenge is completed
        if (challenge.DaysCompleted >= challenge.DurationDays || challenge.CurrentAmount >= challenge.TargetAmount)
        {
            challenge.Status = ChallengeStatus.Completed;
        }

        challenge.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return existingProgress;
    }

    public async Task<IEnumerable<SavingsChallengeProgress>> GetChallengeProgressAsync(int challengeId)
    {
        return await _context.SavingsChallengeProgress
            .Where(p => p.SavingsChallengeId == challengeId)
            .OrderBy(p => p.Date)
            .ToListAsync();
    }

    public async Task UpdateChallengeStatusAsync(int challengeId, ChallengeStatus status)
    {
        // Verify ownership before updating
        var challenge = await GetChallengeByIdAsync(challengeId);
        if (challenge != null)
        {
            challenge.Status = status;
            challenge.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> CalculateCurrentStreakAsync(int challengeId)
    {
        var progressEntries = await _context.SavingsChallengeProgress
            .Where(p => p.SavingsChallengeId == challengeId && p.Completed)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        if (!progressEntries.Any())
        {
            return 0;
        }

        int streak = 0;
        DateTime expectedDate = DateTime.UtcNow.Date;

        foreach (var entry in progressEntries)
        {
            if (entry.Date.Date == expectedDate || entry.Date.Date == expectedDate.AddDays(-1))
            {
                streak++;
                expectedDate = entry.Date.Date.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    public async Task<int> GetTotalActiveChallengesAsync()
    {
        var query = _context.SavingsChallenges
            .Where(c => c.Status == ChallengeStatus.Active)
            .AsQueryable();

        query = ApplyUserFilter(query);

        return await query.CountAsync();
    }

    public async Task<int> GetTotalCompletedChallengesAsync()
    {
        var query = _context.SavingsChallenges
            .Where(c => c.Status == ChallengeStatus.Completed)
            .AsQueryable();

        query = ApplyUserFilter(query);

        return await query.CountAsync();
    }

    public async Task<decimal> GetTotalAmountSavedAsync()
    {
        var query = _context.SavingsChallenges.AsQueryable();

        query = ApplyUserFilter(query);

        return await query.SumAsync(c => c.CurrentAmount);
    }

    // Challenge template methods
    public async Task<IEnumerable<ChallengeTemplate>> GetAllTemplatesAsync()
    {
        return await _context.ChallengeTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task<ChallengeTemplate?> GetTemplateByIdAsync(int id)
    {
        return await _context.ChallengeTemplates
            .FirstOrDefaultAsync(t => t.ChallengeTemplateId == id && t.IsActive);
    }

    public async Task<SavingsChallenge> CreateChallengeFromTemplateAsync(int templateId)
    {
        var template = await GetTemplateByIdAsync(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template with ID {templateId} not found.");
        }

        var challenge = template.ToChallenge();
        
        // Set user ID for new challenges
        if (_currentUserService?.IsAuthenticated == true && _currentUserService.UserId != null)
        {
            challenge.UserId = _currentUserService.UserId;
        }

        return await CreateChallengeAsync(challenge);
    }
}
