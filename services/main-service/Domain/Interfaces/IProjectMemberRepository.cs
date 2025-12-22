using MainService.Domain.Entities;
using MainService.Domain.Enums;

namespace MainService.Domain.Interfaces;

public interface IProjectMemberRepository
{
    Task<ProjectMemberDomain?> GetAsync(string id);
    Task<ProjectMemberDomain?> GetByProjectAndUserAsync(string projectId, string userId);
    Task<IEnumerable<ProjectMemberDomain>> GetProjectMembersAsync(string projectId, int page, int limit);
    Task<int> GetProjectMembersCountAsync(string projectId);
    Task<(IEnumerable<ProjectMemberDomain> Members, int TotalCount)> SearchProjectMembersAsync(SearchProjectMemberQueryParams param);
    Task<ProjectMemberDomain> AddAsync(ProjectMemberDomain member);
    Task<ProjectMemberDomain> UpdateAsync(ProjectMemberDomain member);
    Task DeleteAsync(string id);
    Task<bool> IsUserProjectMemberAsync(string projectId, string userId);
    Task<bool> HasProjectRole(string projectId, string userId, TeamMemberRole role);
    Task<IEnumerable<ProjectMemberDomain>> GetUserProjectsAsync(string userId, int page, int limit);
    Task<int> GetUserProjectsCountAsync(string userId);
    Task<ProjectMemberDomain> ApproveMemberAsync(string projectId, string userId);
    Task<bool> RejectMemberAsync(string projectId, string userId);
    Task<bool> AddMembersToTeamAsync(string projectId, string teamId, List<string> userIds);
}

public class SearchProjectMemberQueryParams
{
    public string ProjectId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
}