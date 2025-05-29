using MainService.Domain.Entities;
using MainService.Domain.Enums;
namespace MainService.Domain.Interfaces;

public interface IIssueRepository
{
    Task<IssueDomain> CreateIssue(IssueDomain issue);
    Task<IssueDomain> GetIssue(string id);
    Task<IssueDomain> UpdateIssue(UpdateIssueParams issue);
    Task<UserStats> GetStats(string userId);
    Task DeleteIssue(string id);
    Task<(List<IssueDomain> Issues, int TotalCount)> ListIssues(GetIssuesParams param);
    Task<List<IssueDomain>> GetIssuesBySprintId(string sprintId);
}

public class CreateIssueParams
{
    public required string ProjectId;
    public required string Title;
    public required string ReporterId;
    public string? ColumnId;
    // public required string Status;
    public string? SprintId;
    public string? AssigneeId;
}

public class GetIssuesParams
{
    public string? ProjectId;
    public List<string>? ColumnIds;
    public List<string>? AssigneeIds;
    public List<string>? SprintIds;
    public string? Keyword;
    public int Page;
    public int Limit;
}

public class UpdateIssueParams
{
    public required string IssueId;
    public required string CreatorId;
    public string? Title;
    public string? ProjectId;
    public string? SprintId;
    public string? AssigneeId;
    public string? Description;
    public string? Summary;
    public int? StoryPoint;
    public string? ReporterId;
    public string? ColumnId;
    public string? ParentId;
    public IssueType? Type;
    public IssuePriority? Priority;
    public List<string>? Attachments;
}

public class UserStats
{
    public int TotalIssues { get; set; }
    public int CompletedIssues { get; set; }
    public int InProgressIssues { get; set; }
    public int TodoIssues { get; set; }
    public Dictionary<string, int> IssuesByType { get; set; } = new();
    public Dictionary<string, int> IssuesByPriority { get; set; } = new();
}