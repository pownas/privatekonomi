using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public interface ISavingsChallengeService
{
    // Challenge CRUD operations
    Task<IEnumerable<SavingsChallenge>> GetAllChallengesAsync();
    Task<SavingsChallenge?> GetChallengeByIdAsync(int id);
    Task<SavingsChallenge> CreateChallengeAsync(SavingsChallenge challenge);
    Task<SavingsChallenge> UpdateChallengeAsync(SavingsChallenge challenge);
    Task DeleteChallengeAsync(int id);
    
    // Challenge filtering
    Task<IEnumerable<SavingsChallenge>> GetActiveChallengesAsync();
    Task<IEnumerable<SavingsChallenge>> GetCompletedChallengesAsync();
    Task<IEnumerable<SavingsChallenge>> GetChallengesByTypeAsync(ChallengeType type);
    
    // Challenge templates
    Task<IEnumerable<ChallengeTemplate>> GetAllTemplatesAsync();
    Task<ChallengeTemplate?> GetTemplateByIdAsync(int id);
    Task<SavingsChallenge> CreateChallengeFromTemplateAsync(int templateId);
    
    // Progress tracking
    Task<SavingsChallengeProgress> RecordProgressAsync(int challengeId, DateTime progressDate, bool completed, decimal amountSaved, string? notes = null);
    Task<IEnumerable<SavingsChallengeProgress>> GetChallengeProgressAsync(int challengeId);
    Task UpdateChallengeStatusAsync(int challengeId, ChallengeStatus status);
    Task<int> CalculateCurrentStreakAsync(int challengeId);
    
    // Statistics
    Task<int> GetTotalActiveChallengesAsync();
    Task<int> GetTotalCompletedChallengesAsync();
    Task<decimal> GetTotalAmountSavedAsync();
}
