using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services;

public class SharedGoalService : ISharedGoalService
{
    private readonly PrivatekonomyContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SharedGoalService(PrivatekonomyContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private string GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId))
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }
        return _currentUserService.UserId;
    }

    // Shared Goal CRUD
    public async Task<IEnumerable<SharedGoal>> GetAllSharedGoalsAsync()
    {
        var userId = GetCurrentUserId();
        
        return await _context.SharedGoals
            .Include(sg => sg.Participants)
            .Include(sg => sg.CreatedByUser)
            .Where(sg => sg.Participants.Any(p => p.UserId == userId && p.InvitationStatus == InvitationStatus.Accepted))
            .OrderByDescending(sg => sg.CreatedAt)
            .ToListAsync();
    }

    public async Task<SharedGoal?> GetSharedGoalByIdAsync(int id)
    {
        var userId = GetCurrentUserId();
        
        var sharedGoal = await _context.SharedGoals
            .Include(sg => sg.Participants).ThenInclude(p => p.User)
            .Include(sg => sg.Participants).ThenInclude(p => p.InvitedByUser)
            .Include(sg => sg.CreatedByUser)
            .Include(sg => sg.Proposals).ThenInclude(p => p.ProposedByUser)
            .Include(sg => sg.Proposals).ThenInclude(p => p.Votes).ThenInclude(v => v.User)
            .Include(sg => sg.Transactions).ThenInclude(t => t.User)
            .FirstOrDefaultAsync(sg => sg.SharedGoalId == id);

        if (sharedGoal == null)
            return null;

        // Verify user has access
        if (!sharedGoal.Participants.Any(p => p.UserId == userId && p.InvitationStatus == InvitationStatus.Accepted))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        return sharedGoal;
    }

    public async Task<SharedGoal> CreateSharedGoalAsync(SharedGoal sharedGoal)
    {
        var userId = GetCurrentUserId();
        
        sharedGoal.CreatedByUserId = userId;
        sharedGoal.CreatedAt = DateTime.UtcNow;
        sharedGoal.Status = SharedGoalStatus.Active;
        
        _context.SharedGoals.Add(sharedGoal);
        await _context.SaveChangesAsync();

        // Add creator as owner participant
        var ownerParticipant = new SharedGoalParticipant
        {
            SharedGoalId = sharedGoal.SharedGoalId,
            UserId = userId,
            Role = ParticipantRole.Owner,
            InvitationStatus = InvitationStatus.Accepted,
            JoinedAt = DateTime.UtcNow
        };
        
        _context.SharedGoalParticipants.Add(ownerParticipant);
        await _context.SaveChangesAsync();

        return sharedGoal;
    }

    public async Task<SharedGoal> UpdateSharedGoalAsync(SharedGoal sharedGoal)
    {
        var userId = GetCurrentUserId();
        
        // Verify user has access
        if (!await IsParticipantAsync(sharedGoal.SharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        sharedGoal.UpdatedAt = DateTime.UtcNow;
        _context.Entry(sharedGoal).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        
        return sharedGoal;
    }

    public async Task DeleteSharedGoalAsync(int id)
    {
        var userId = GetCurrentUserId();
        
        // Only owner can delete
        if (!await IsOwnerAsync(id, userId))
            throw new UnauthorizedAccessException("Only the owner can delete the shared goal");

        var sharedGoal = await _context.SharedGoals.FindAsync(id);
        if (sharedGoal != null)
        {
            _context.SharedGoals.Remove(sharedGoal);
            await _context.SaveChangesAsync();
        }
    }

    // Participant Management
    public async Task<IEnumerable<SharedGoalParticipant>> GetParticipantsAsync(int sharedGoalId)
    {
        var userId = GetCurrentUserId();
        
        // Verify user has access
        if (!await IsParticipantAsync(sharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        return await _context.SharedGoalParticipants
            .Include(p => p.User)
            .Include(p => p.InvitedByUser)
            .Where(p => p.SharedGoalId == sharedGoalId)
            .ToListAsync();
    }

    public async Task<SharedGoalParticipant> InviteParticipantAsync(int sharedGoalId, string userEmail)
    {
        var userId = GetCurrentUserId();
        
        // Only owner can invite
        if (!await IsOwnerAsync(sharedGoalId, userId))
            throw new UnauthorizedAccessException("Only the owner can invite participants");

        // Find user by email
        var invitedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (invitedUser == null)
            throw new ArgumentException($"No user found with email: {userEmail}");

        // Check if already a participant
        var existing = await _context.SharedGoalParticipants
            .FirstOrDefaultAsync(p => p.SharedGoalId == sharedGoalId && p.UserId == invitedUser.Id);
        
        if (existing != null)
            throw new InvalidOperationException("User is already a participant or has a pending invitation");

        var participant = new SharedGoalParticipant
        {
            SharedGoalId = sharedGoalId,
            UserId = invitedUser.Id,
            Role = ParticipantRole.Participant,
            InvitationStatus = InvitationStatus.Pending,
            InvitedByUserId = userId,
            InvitedAt = DateTime.UtcNow,
            JoinedAt = DateTime.UtcNow
        };

        _context.SharedGoalParticipants.Add(participant);
        await _context.SaveChangesAsync();

        // Create notification for invited user
        var sharedGoal = await _context.SharedGoals.FindAsync(sharedGoalId);
        var inviterName = (await _context.Users.FindAsync(userId))?.Email ?? "Someone";
        await CreateNotificationAsync(
            sharedGoalId,
            invitedUser.Id,
            NotificationType.Invitation,
            $"{inviterName} har bjudit in dig till sparmålet '{sharedGoal?.Name}'");

        return participant;
    }

    public async Task<SharedGoalParticipant> AcceptInvitationAsync(int participantId)
    {
        var userId = GetCurrentUserId();
        
        var participant = await _context.SharedGoalParticipants
            .Include(p => p.SharedGoal)
            .FirstOrDefaultAsync(p => p.SharedGoalParticipantId == participantId && p.UserId == userId);

        if (participant == null)
            throw new ArgumentException("Invitation not found");

        if (participant.InvitationStatus != InvitationStatus.Pending)
            throw new InvalidOperationException("Invitation is not pending");

        participant.InvitationStatus = InvitationStatus.Accepted;
        participant.JoinedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        // Notify all participants
        var participants = await _context.SharedGoalParticipants
            .Where(p => p.SharedGoalId == participant.SharedGoalId && p.UserId != userId)
            .ToListAsync();

        var userName = (await _context.Users.FindAsync(userId))?.Email ?? "Someone";
        foreach (var p in participants)
        {
            await CreateNotificationAsync(
                participant.SharedGoalId,
                p.UserId,
                NotificationType.InvitationAccepted,
                $"{userName} har accepterat inbjudan till '{participant.SharedGoal?.Name}'");
        }

        return participant;
    }

    public async Task<SharedGoalParticipant> RejectInvitationAsync(int participantId)
    {
        var userId = GetCurrentUserId();
        
        var participant = await _context.SharedGoalParticipants
            .Include(p => p.SharedGoal)
            .FirstOrDefaultAsync(p => p.SharedGoalParticipantId == participantId && p.UserId == userId);

        if (participant == null)
            throw new ArgumentException("Invitation not found");

        if (participant.InvitationStatus != InvitationStatus.Pending)
            throw new InvalidOperationException("Invitation is not pending");

        participant.InvitationStatus = InvitationStatus.Rejected;
        await _context.SaveChangesAsync();

        // Notify the inviter
        if (participant.InvitedByUserId != null)
        {
            var userName = (await _context.Users.FindAsync(userId))?.Email ?? "Someone";
            await CreateNotificationAsync(
                participant.SharedGoalId,
                participant.InvitedByUserId,
                NotificationType.InvitationRejected,
                $"{userName} har avvisat inbjudan till '{participant.SharedGoal?.Name}'");
        }

        return participant;
    }

    public async Task RemoveParticipantAsync(int sharedGoalId, string userId)
    {
        var currentUserId = GetCurrentUserId();
        
        // Only owner can remove participants
        if (!await IsOwnerAsync(sharedGoalId, currentUserId))
            throw new UnauthorizedAccessException("Only the owner can remove participants");

        var participant = await _context.SharedGoalParticipants
            .Include(p => p.SharedGoal)
            .FirstOrDefaultAsync(p => p.SharedGoalId == sharedGoalId && p.UserId == userId);

        if (participant == null)
            throw new ArgumentException("Participant not found");

        if (participant.Role == ParticipantRole.Owner)
            throw new InvalidOperationException("Cannot remove the owner. Transfer ownership first.");

        _context.SharedGoalParticipants.Remove(participant);
        await _context.SaveChangesAsync();

        // Notify removed participant
        await CreateNotificationAsync(
            sharedGoalId,
            userId,
            NotificationType.ParticipantRemoved,
            $"Du har tagits bort från sparmålet '{participant.SharedGoal?.Name}'");

        // Notify other participants
        var otherParticipants = await _context.SharedGoalParticipants
            .Where(p => p.SharedGoalId == sharedGoalId && p.UserId != userId)
            .ToListAsync();

        var userName = (await _context.Users.FindAsync(userId))?.Email ?? "Someone";
        foreach (var p in otherParticipants)
        {
            await CreateNotificationAsync(
                sharedGoalId,
                p.UserId,
                NotificationType.ParticipantRemoved,
                $"{userName} har tagits bort från sparmålet");
        }
    }

    public async Task LeaveSharedGoalAsync(int sharedGoalId)
    {
        var userId = GetCurrentUserId();
        
        var participant = await _context.SharedGoalParticipants
            .Include(p => p.SharedGoal)
            .FirstOrDefaultAsync(p => p.SharedGoalId == sharedGoalId && p.UserId == userId);

        if (participant == null)
            throw new ArgumentException("You are not a participant of this shared goal");

        if (participant.Role == ParticipantRole.Owner)
        {
            var otherParticipants = await _context.SharedGoalParticipants
                .Where(p => p.SharedGoalId == sharedGoalId && p.UserId != userId && p.InvitationStatus == InvitationStatus.Accepted)
                .ToListAsync();

            if (otherParticipants.Any())
                throw new InvalidOperationException("Owner must transfer ownership before leaving");
            
            // If no other participants, archive the goal
            var sharedGoal = await _context.SharedGoals.FindAsync(sharedGoalId);
            if (sharedGoal != null)
            {
                sharedGoal.Status = SharedGoalStatus.Archived;
            }
        }

        _context.SharedGoalParticipants.Remove(participant);
        await _context.SaveChangesAsync();

        // Notify other participants
        var remainingParticipants = await _context.SharedGoalParticipants
            .Where(p => p.SharedGoalId == sharedGoalId)
            .ToListAsync();

        var userName = (await _context.Users.FindAsync(userId))?.Email ?? "Someone";
        foreach (var p in remainingParticipants)
        {
            await CreateNotificationAsync(
                sharedGoalId,
                p.UserId,
                NotificationType.ParticipantLeft,
                $"{userName} har lämnat sparmålet '{participant.SharedGoal?.Name}'");
        }
    }

    public async Task TransferOwnershipAsync(int sharedGoalId, string newOwnerUserId)
    {
        var userId = GetCurrentUserId();
        
        // Only current owner can transfer ownership
        if (!await IsOwnerAsync(sharedGoalId, userId))
            throw new UnauthorizedAccessException("Only the owner can transfer ownership");

        var currentOwner = await _context.SharedGoalParticipants
            .FirstOrDefaultAsync(p => p.SharedGoalId == sharedGoalId && p.UserId == userId);

        var newOwner = await _context.SharedGoalParticipants
            .Include(p => p.SharedGoal)
            .FirstOrDefaultAsync(p => p.SharedGoalId == sharedGoalId && p.UserId == newOwnerUserId);

        if (newOwner == null)
            throw new ArgumentException("New owner is not a participant of this shared goal");

        if (newOwner.InvitationStatus != InvitationStatus.Accepted)
            throw new InvalidOperationException("New owner has not accepted the invitation");

        // Swap roles
        if (currentOwner != null)
            currentOwner.Role = ParticipantRole.Participant;
        newOwner.Role = ParticipantRole.Owner;

        await _context.SaveChangesAsync();

        // Notify all participants
        var participants = await _context.SharedGoalParticipants
            .Where(p => p.SharedGoalId == sharedGoalId)
            .ToListAsync();

        var newOwnerName = (await _context.Users.FindAsync(newOwnerUserId))?.Email ?? "Someone";
        foreach (var p in participants)
        {
            await CreateNotificationAsync(
                sharedGoalId,
                p.UserId,
                NotificationType.OwnershipTransferred,
                $"Ägarskapet har överförts till {newOwnerName}");
        }
    }

    public async Task<bool> IsParticipantAsync(int sharedGoalId, string userId)
    {
        return await _context.SharedGoalParticipants
            .AnyAsync(p => p.SharedGoalId == sharedGoalId && p.UserId == userId && p.InvitationStatus == InvitationStatus.Accepted);
    }

    public async Task<bool> IsOwnerAsync(int sharedGoalId, string userId)
    {
        return await _context.SharedGoalParticipants
            .AnyAsync(p => p.SharedGoalId == sharedGoalId && p.UserId == userId && 
                          p.Role == ParticipantRole.Owner && p.InvitationStatus == InvitationStatus.Accepted);
    }

    // Proposal Management
    public async Task<IEnumerable<SharedGoalProposal>> GetProposalsAsync(int sharedGoalId)
    {
        var userId = GetCurrentUserId();
        
        // Verify user has access
        if (!await IsParticipantAsync(sharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        return await _context.SharedGoalProposals
            .Include(p => p.ProposedByUser)
            .Include(p => p.Votes).ThenInclude(v => v.User)
            .Where(p => p.SharedGoalId == sharedGoalId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<SharedGoalProposal> CreateProposalAsync(SharedGoalProposal proposal)
    {
        var userId = GetCurrentUserId();
        
        // Verify user has access
        if (!await IsParticipantAsync(proposal.SharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        proposal.ProposedByUserId = userId;
        proposal.CreatedAt = DateTime.UtcNow;
        proposal.Status = ProposalStatus.Pending;

        _context.SharedGoalProposals.Add(proposal);
        await _context.SaveChangesAsync();

        // Notify all participants except the proposer
        var participants = await _context.SharedGoalParticipants
            .Where(p => p.SharedGoalId == proposal.SharedGoalId && p.UserId != userId && p.InvitationStatus == InvitationStatus.Accepted)
            .ToListAsync();

        var proposerName = (await _context.Users.FindAsync(userId))?.Email ?? "Someone";
        var sharedGoal = await _context.SharedGoals.FindAsync(proposal.SharedGoalId);
        foreach (var p in participants)
        {
            await CreateNotificationAsync(
                proposal.SharedGoalId,
                p.UserId,
                NotificationType.ProposalCreated,
                $"{proposerName} har skapat ett nytt förslag för '{sharedGoal?.Name}'");
        }

        return proposal;
    }

    public async Task<SharedGoalProposalVote> VoteOnProposalAsync(int proposalId, VoteType vote, string? comment = null)
    {
        var userId = GetCurrentUserId();
        
        var proposal = await _context.SharedGoalProposals
            .Include(p => p.SharedGoal)
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.SharedGoalProposalId == proposalId);

        if (proposal == null)
            throw new ArgumentException("Proposal not found");

        // Verify user has access
        if (!await IsParticipantAsync(proposal.SharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        if (proposal.Status != ProposalStatus.Pending)
            throw new InvalidOperationException("Proposal is not pending");

        // Check if already voted
        var existingVote = await _context.SharedGoalProposalVotes
            .FirstOrDefaultAsync(v => v.SharedGoalProposalId == proposalId && v.UserId == userId);

        if (existingVote != null)
            throw new InvalidOperationException("User has already voted on this proposal");

        var proposalVote = new SharedGoalProposalVote
        {
            SharedGoalProposalId = proposalId,
            UserId = userId,
            Vote = vote,
            VotedAt = DateTime.UtcNow,
            Comment = comment
        };

        _context.SharedGoalProposalVotes.Add(proposalVote);
        await _context.SaveChangesAsync();

        // Check if all participants have voted
        await CheckAndApplyProposalAsync(proposalId);

        return proposalVote;
    }

    public async Task WithdrawProposalAsync(int proposalId)
    {
        var userId = GetCurrentUserId();
        
        var proposal = await _context.SharedGoalProposals
            .Include(p => p.SharedGoal)
            .FirstOrDefaultAsync(p => p.SharedGoalProposalId == proposalId);

        if (proposal == null)
            throw new ArgumentException("Proposal not found");

        if (proposal.ProposedByUserId != userId)
            throw new UnauthorizedAccessException("Only the proposer can withdraw the proposal");

        if (proposal.Status != ProposalStatus.Pending)
            throw new InvalidOperationException("Only pending proposals can be withdrawn");

        proposal.Status = ProposalStatus.Withdrawn;
        proposal.ResolvedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        // Notify all participants
        var participants = await _context.SharedGoalParticipants
            .Where(p => p.SharedGoalId == proposal.SharedGoalId && p.InvitationStatus == InvitationStatus.Accepted)
            .ToListAsync();

        foreach (var p in participants)
        {
            await CreateNotificationAsync(
                proposal.SharedGoalId,
                p.UserId,
                NotificationType.ProposalWithdrawn,
                $"Ett förslag har dragits tillbaka för '{proposal.SharedGoal?.Name}'");
        }
    }

    public async Task<bool> CheckAndApplyProposalAsync(int proposalId)
    {
        var proposal = await _context.SharedGoalProposals
            .Include(p => p.SharedGoal)
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.SharedGoalProposalId == proposalId);

        if (proposal == null || proposal.Status != ProposalStatus.Pending)
            return false;

        // Get all accepted participants
        var participantCount = await _context.SharedGoalParticipants
            .CountAsync(p => p.SharedGoalId == proposal.SharedGoalId && p.InvitationStatus == InvitationStatus.Accepted);

        var voteCount = proposal.Votes.Count;

        // If all have voted
        if (voteCount == participantCount)
        {
            var approveCount = proposal.Votes.Count(v => v.Vote == VoteType.Approve);
            var rejectCount = proposal.Votes.Count(v => v.Vote == VoteType.Reject);

            if (rejectCount > 0)
            {
                // Any rejection means proposal is rejected
                proposal.Status = ProposalStatus.Rejected;
                proposal.ResolvedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Notify all participants
                var participants = await _context.SharedGoalParticipants
                    .Where(p => p.SharedGoalId == proposal.SharedGoalId && p.InvitationStatus == InvitationStatus.Accepted)
                    .ToListAsync();

                foreach (var p in participants)
                {
                    await CreateNotificationAsync(
                        proposal.SharedGoalId,
                        p.UserId,
                        NotificationType.ProposalRejected,
                        $"Ett förslag har avslagits för '{proposal.SharedGoal?.Name}'");
                }

                return false;
            }

            if (approveCount == participantCount)
            {
                // All approved, apply the proposal
                proposal.Status = ProposalStatus.Approved;
                proposal.ResolvedAt = DateTime.UtcNow;

                var sharedGoal = proposal.SharedGoal;
                if (sharedGoal != null)
                {
                    switch (proposal.ProposalType)
                    {
                        case ProposalType.ChangeTargetAmount:
                            if (decimal.TryParse(proposal.ProposedValue, out var newTargetAmount))
                                sharedGoal.TargetAmount = newTargetAmount;
                            break;
                        case ProposalType.ChangeTargetDate:
                            if (DateTime.TryParse(proposal.ProposedValue, out var newTargetDate))
                                sharedGoal.TargetDate = newTargetDate;
                            break;
                        case ProposalType.ChangeName:
                            sharedGoal.Name = proposal.ProposedValue;
                            break;
                        case ProposalType.ChangeDescription:
                            sharedGoal.Description = proposal.ProposedValue;
                            break;
                        case ProposalType.ChangePriority:
                            if (int.TryParse(proposal.ProposedValue, out var newPriority))
                                sharedGoal.Priority = newPriority;
                            break;
                    }
                    sharedGoal.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Notify all participants
                var participants = await _context.SharedGoalParticipants
                    .Where(p => p.SharedGoalId == proposal.SharedGoalId && p.InvitationStatus == InvitationStatus.Accepted)
                    .ToListAsync();

                foreach (var p in participants)
                {
                    await CreateNotificationAsync(
                        proposal.SharedGoalId,
                        p.UserId,
                        NotificationType.ProposalApproved,
                        $"Ett förslag har godkänts och genomförts för '{proposal.SharedGoal?.Name}'");
                }

                return true;
            }
        }

        return false;
    }

    // Transaction Management
    public async Task<IEnumerable<SharedGoalTransaction>> GetTransactionsAsync(int sharedGoalId)
    {
        var userId = GetCurrentUserId();
        
        // Verify user has access
        if (!await IsParticipantAsync(sharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        return await _context.SharedGoalTransactions
            .Include(t => t.User)
            .Where(t => t.SharedGoalId == sharedGoalId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<SharedGoalTransaction> CreateTransactionAsync(SharedGoalTransaction transaction)
    {
        var userId = GetCurrentUserId();
        
        // Verify user has access
        if (!await IsParticipantAsync(transaction.SharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        transaction.UserId = userId;
        transaction.CreatedAt = DateTime.UtcNow;
        
        _context.SharedGoalTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction;
    }

    public async Task UpdateGoalAmountAsync(int sharedGoalId, decimal amount, TransactionType type, string? description = null)
    {
        var userId = GetCurrentUserId();
        
        // Verify user has access
        if (!await IsParticipantAsync(sharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        var sharedGoal = await _context.SharedGoals.FindAsync(sharedGoalId);
        if (sharedGoal == null)
            throw new ArgumentException("Shared goal not found");

        // Create transaction
        var transaction = new SharedGoalTransaction
        {
            SharedGoalId = sharedGoalId,
            UserId = userId,
            Amount = Math.Abs(amount),
            Type = type,
            Description = description,
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.SharedGoalTransactions.Add(transaction);

        // Update current amount
        if (type == TransactionType.Deposit)
        {
            sharedGoal.CurrentAmount += Math.Abs(amount);
        }
        else
        {
            sharedGoal.CurrentAmount -= Math.Abs(amount);
            if (sharedGoal.CurrentAmount < 0)
                sharedGoal.CurrentAmount = 0;
        }

        sharedGoal.UpdatedAt = DateTime.UtcNow;

        // Check if goal is completed
        if (sharedGoal.CurrentAmount >= sharedGoal.TargetAmount && sharedGoal.Status == SharedGoalStatus.Active)
        {
            sharedGoal.Status = SharedGoalStatus.Completed;
            
            // Notify all participants
            var participants = await _context.SharedGoalParticipants
                .Where(p => p.SharedGoalId == sharedGoalId && p.InvitationStatus == InvitationStatus.Accepted)
                .ToListAsync();

            foreach (var p in participants)
            {
                await CreateNotificationAsync(
                    sharedGoalId,
                    p.UserId,
                    NotificationType.GoalCompleted,
                    $"Grattis! Sparmålet '{sharedGoal.Name}' har uppnåtts!");
            }
        }

        await _context.SaveChangesAsync();

        // Notify participants for large transactions (over 1000 kr)
        if (Math.Abs(amount) >= 1000)
        {
            var participants = await _context.SharedGoalParticipants
                .Where(p => p.SharedGoalId == sharedGoalId && p.UserId != userId && p.InvitationStatus == InvitationStatus.Accepted)
                .ToListAsync();

            var userName = (await _context.Users.FindAsync(userId))?.Email ?? "Someone";
            var actionText = type == TransactionType.Deposit ? "satt in" : "tagit ut";
            foreach (var p in participants)
            {
                await CreateNotificationAsync(
                    sharedGoalId,
                    p.UserId,
                    NotificationType.TransactionMade,
                    $"{userName} har {actionText} {amount:N0} kr i '{sharedGoal.Name}'");
            }
        }
    }

    // Notification Management
    public async Task<IEnumerable<SharedGoalNotification>> GetNotificationsAsync()
    {
        var userId = GetCurrentUserId();
        
        return await _context.SharedGoalNotifications
            .Include(n => n.SharedGoal)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SharedGoalNotification>> GetUnreadNotificationsAsync()
    {
        var userId = GetCurrentUserId();
        
        return await _context.SharedGoalNotifications
            .Include(n => n.SharedGoal)
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<SharedGoalNotification> MarkNotificationAsReadAsync(int notificationId)
    {
        var userId = GetCurrentUserId();
        
        var notification = await _context.SharedGoalNotifications
            .FirstOrDefaultAsync(n => n.SharedGoalNotificationId == notificationId && n.UserId == userId);

        if (notification == null)
            throw new ArgumentException("Notification not found");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return notification;
    }

    public async Task DeleteNotificationAsync(int notificationId)
    {
        var userId = GetCurrentUserId();
        
        var notification = await _context.SharedGoalNotifications
            .FirstOrDefaultAsync(n => n.SharedGoalNotificationId == notificationId && n.UserId == userId);

        if (notification != null)
        {
            _context.SharedGoalNotifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task CreateNotificationAsync(int sharedGoalId, string userId, NotificationType type, string message)
    {
        var notification = new SharedGoalNotification
        {
            SharedGoalId = sharedGoalId,
            UserId = userId,
            Type = type,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.SharedGoalNotifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    // Statistics
    public async Task<decimal> GetTotalProgressAsync(int sharedGoalId)
    {
        var userId = GetCurrentUserId();
        
        // Verify user has access
        if (!await IsParticipantAsync(sharedGoalId, userId))
            throw new UnauthorizedAccessException("User does not have access to this shared goal");

        var sharedGoal = await _context.SharedGoals.FindAsync(sharedGoalId);
        if (sharedGoal == null)
            return 0;

        if (sharedGoal.TargetAmount == 0)
            return 0;

        return Math.Min(100, (sharedGoal.CurrentAmount / sharedGoal.TargetAmount) * 100);
    }

    public async Task<IEnumerable<SharedGoal>> GetActiveSharedGoalsAsync()
    {
        var userId = GetCurrentUserId();
        
        return await _context.SharedGoals
            .Include(sg => sg.Participants)
            .Where(sg => sg.Status == SharedGoalStatus.Active &&
                        sg.Participants.Any(p => p.UserId == userId && p.InvitationStatus == InvitationStatus.Accepted))
            .OrderBy(sg => sg.Priority)
            .ThenBy(sg => sg.TargetDate)
            .ToListAsync();
    }
}
