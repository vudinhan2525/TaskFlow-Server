using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Domain.Enums;


namespace MainService.Domain.UseCases;

public class ProjectMemberUseCase
{
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPublisherService _publisher;


    public ProjectMemberUseCase(
        IProjectMemberRepository projectMemberRepository,
        IProjectRepository projectRepository,
        IPublisherService publisher)
    {
        _projectMemberRepository = projectMemberRepository;
        _projectRepository = projectRepository;
        _publisher = publisher;
    }

    public async Task<ProjectMemberDomain> AddProjectMemberAsync(string projectId, string requesterId, string userId, TeamMemberRole role)
    {
        var project = await _projectRepository.GetProject(projectId)
            ?? throw new KeyNotFoundException("Project not found");

        var existingMember = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId);
        if (existingMember != null)
        {
            throw new InvalidOperationException("User is already a member of this project");
        }

        // Check if requester is project owner
        var requester = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, requesterId);
        if (requester == null)
        {
            throw new KeyNotFoundException("Requester not found in project");
        }

        // If the requester is the owner, the member is automatically approved
        var isPending = requester.Role != TeamMemberRole.Owner;

        var member = new ProjectMemberDomain
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            IsPending = isPending
        };

        var addedMember = await _projectMemberRepository.AddAsync(member);
        // Send appropriate notification based on pending status
        if (isPending)
        {
            // Send invitation notification if pending approval
            await _publisher.EmitKafka(TopicName.NOTIFICATIONS, KafkaMessageAction.NOTIFICATIONS_CREATE_NEW_NOTIFICATION, new INotificationMessage
            {
                Type = NotificationType.PROJECT_INVITATION.ToString(),
                ActorId = requesterId,
                RecipientId = addedMember.Id,
            });
        }
        else
        {
            // Send team member added notification if directly approved (added by owner)
            await _publisher.EmitKafka(TopicName.NOTIFICATIONS, KafkaMessageAction.NOTIFICATIONS_CREATE_NEW_NOTIFICATION, new INotificationMessage
            {
                Type = NotificationType.PROJECT_TEAM_ADDED.ToString(),
                ActorId = requesterId,
                RecipientId = addedMember.Id,
            });
        }

        return addedMember;
    }

    public async Task<ProjectMemberDomain> UpdateProjectMemberRoleAsync(string projectId, string userId, TeamMemberRole newRole)
    {
        var member = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId)
            ?? throw new KeyNotFoundException("Project member not found");

        if (member.Role == TeamMemberRole.Owner && newRole != TeamMemberRole.Owner)
        {
            throw new InvalidOperationException("Cannot change the role of project owner");
        }

        member.Role = newRole;
        member.UpdatedAt = DateTime.UtcNow;

        return await _projectMemberRepository.UpdateAsync(member);
    }

    public async Task RemoveProjectMemberAsync(string projectId, string userId)
    {
        var member = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId)
            ?? throw new KeyNotFoundException("Project member not found");

        if (member.Role == TeamMemberRole.Owner)
        {
            throw new InvalidOperationException("Cannot remove project owner from project");
        }

        await _projectMemberRepository.DeleteAsync(member.Id!);
    }

    public async Task<ProjectMemberDomain?> GetByProjectAndUserAsync(string projectId, string userId)
    {
        return await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId);
    }

    public async Task<(IEnumerable<ProjectMemberDomain> Members, int TotalCount)> GetProjectMembersAsync(
        string projectId, int page, int limit)
    {
        var members = await _projectMemberRepository.GetProjectMembersAsync(projectId, page, limit);
        var totalCount = await _projectMemberRepository.GetProjectMembersCountAsync(projectId);

        return (members, totalCount);
    }

    public async Task<(IEnumerable<ProjectMemberDomain> Members, int TotalCount)> SearchProjectMembersAsync(
        SearchProjectMemberQueryParams param)
    {
        return await _projectMemberRepository.SearchProjectMembersAsync(param);
    }

    public async Task<bool> IsUserProjectMemberAsync(string projectId, string userId)
    {
        return await _projectMemberRepository.IsUserProjectMemberAsync(projectId, userId);
    }

    public async Task<bool> HasProjectRoleAsync(string projectId, string userId, TeamMemberRole role)
    {
        return await _projectMemberRepository.HasProjectRole(projectId, userId, role);
    }

    public async Task<(IEnumerable<ProjectMemberDomain> Projects, int TotalCount)> GetUserProjectsAsync(
        string userId, int page, int limit)
    {
        var projects = await _projectMemberRepository.GetUserProjectsAsync(userId, page, limit);
        var totalCount = await _projectMemberRepository.GetUserProjectsCountAsync(userId);

        return (projects, totalCount);
    }

    public async Task<ProjectMemberDomain> ApproveProjectMemberAsync(string projectId, string approverId, string userId)
    {
        // Check if approver is project owner
        var approver = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, approverId)
            ?? throw new KeyNotFoundException("Approver not found in project");

        if (approver.Role != TeamMemberRole.Owner)
        {
            throw new UnauthorizedAccessException("Only project owner can approve members");
        }

        var member = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId)
            ?? throw new KeyNotFoundException("Project member not found");

        if (!member.IsPending)
        {
            throw new InvalidOperationException("Member is already approved");
        }

        var approvedMember = await _projectMemberRepository.ApproveMemberAsync(projectId, userId);

        await _publisher.EmitKafka(TopicName.NOTIFICATIONS, KafkaMessageAction.NOTIFICATIONS_CREATE_NEW_NOTIFICATION, new INotificationMessage
        {
            Type = NotificationType.PROJECT_TEAM_ADDED.ToString(),
            ActorId = userId,
            RecipientId = approvedMember.Id,
        });

        return approvedMember;
    }

    public async Task RejectProjectMemberAsync(string projectId, string approverId, string userId)
    {
        // Check if approver is project owner
        var approver = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, approverId)
            ?? throw new KeyNotFoundException("Approver not found in project");

        if (approver.Role != TeamMemberRole.Owner)
        {
            throw new UnauthorizedAccessException("Only project owner can reject members");
        }

        var result = await _projectMemberRepository.RejectMemberAsync(projectId, userId);
        if (!result)
        {
            throw new KeyNotFoundException("Pending project member not found");
        }
    }

    public async Task<ProjectMemberDomain> AcceptInvitationAsync(string projectId, string userId)
    {
        var member = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId)
            ?? throw new KeyNotFoundException("Invitation not found");

        if (!member.IsPending)
        {
            throw new InvalidOperationException("Member is already approved");
        }

        var approvedMember = await _projectMemberRepository.ApproveMemberAsync(projectId, userId);


        return approvedMember;
    }

    public async Task RejectInvitationAsync(string projectId, string userId)
    {
        var member = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId)
            ?? throw new KeyNotFoundException("Invitation not found");

        if (!member.IsPending)
        {
            throw new InvalidOperationException("Cannot reject an already approved membership");
        }

        var result = await _projectMemberRepository.RejectMemberAsync(projectId, userId);
        if (!result)
        {
            throw new KeyNotFoundException("Failed to reject membership");
        }
    }

    public async Task AddMembersToTeamAsync(string projectId, string teamId, List<string> userIds)
    {
        if (userIds == null || !userIds.Any())
        {
            throw new ArgumentException("User IDs list cannot be empty");
        }

        // Check if all users are project members and not pending
        foreach (var userId in userIds)
        {
            var member = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId);
            if (member == null)
            {
                throw new KeyNotFoundException($"User {userId} is not a member of this project");
            }

            if (member.IsPending)
            {
                throw new InvalidOperationException($"Cannot add pending member {userId} to team");
            }
        }

        var result = await _projectMemberRepository.AddMembersToTeamAsync(projectId, teamId, userIds);
        if (!result)
        {
            throw new InvalidOperationException("Failed to add members to team");
        }
    }
}