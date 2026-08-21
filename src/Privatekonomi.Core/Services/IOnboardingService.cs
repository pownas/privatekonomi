namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for managing user onboarding flow
/// </summary>
public interface IOnboardingService
{
    /// <summary>
    /// Check if current user has completed onboarding
    /// </summary>
    Task<bool> HasCompletedOnboardingAsync(string userId);
    
    /// <summary>
    /// Mark onboarding as completed for current user
    /// </summary>
    Task CompleteOnboardingAsync(string userId);
    
    /// <summary>
    /// Get the current onboarding step for the user
    /// </summary>
    Task<int> GetCurrentStepAsync(string userId);
    
    /// <summary>
    /// Set the current onboarding step for the user
    /// </summary>
    Task SetCurrentStepAsync(string userId, int stepNumber);
}
