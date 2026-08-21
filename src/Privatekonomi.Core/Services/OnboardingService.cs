using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for managing user onboarding flow
/// </summary>
public class OnboardingService : IOnboardingService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public OnboardingService(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<bool> HasCompletedOnboardingAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.OnboardingCompleted ?? false;
    }

    public async Task CompleteOnboardingAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.OnboardingCompleted = true;
            user.OnboardingCompletedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
    }

    public async Task<int> GetCurrentStepAsync(string userId)
    {
        // For now, we'll use a simple approach without storing step info
        // In the future, this could be extended to store step data in UserPreferences
        var hasCompleted = await HasCompletedOnboardingAsync(userId);
        return hasCompleted ? -1 : 0;
    }

    public async Task SetCurrentStepAsync(string userId, int stepNumber)
    {
        // For now, this is a placeholder
        // In the future, we could store step progress in UserPreferences or a separate table
        await Task.CompletedTask;
    }
}
