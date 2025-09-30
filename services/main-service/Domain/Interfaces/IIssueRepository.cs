using MainService.Domain.Entities;
using MainService.Domain.Enums;
using TaskFlow.UserService;
namespace MainService.Domain.Interfaces;

public interface IIssueRepository
{
    Task<IssueDomain> CreateIssue(IssueDomain issue);
    Task<IssueDomain> GetIssue(string id);
    Task<IssueDomain> UpdateIssue(UpdateIssueParams issue);
    Task<UserStats> GetStats(string id, bool isSprintId);
    Task DeleteIssue(string id);
    Task<(List<IssueDomain> Issues, int TotalCount)> ListIssues(GetIssuesParams param);
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
    public string? DueDateFrom;
    public string? DueDateTo;
    public string? CreatedAtFrom;
    public string? CreatedAtTo;
    public List<string>? ColumnIds;
    public List<string>? AssigneeIds;
    public List<string>? SprintIds;
    public List<string>? IssueIds;
    public List<string>? Types;
    public List<string>? Priorities;
    public string? Keyword;
    public int Page = 1;
    public int Limit = 10;
}
public class UpdateIssueParams
{
    public required string Id { get; set; }
    public string? CreatorId { get; set; }
    public string? Title { get; set; }
    public string? ProjectId { get; set; }
    public string? SprintId { get; set; }
    public string? AssigneeId { get; set; }
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? TeamId { get; set; }
    public int? StoryPoint { get; set; }
    public string? ReporterId { get; set; }
    public string? ColumnId { get; set; }
    public string? ParentId { get; set; }
    public IssueType? Type { get; set; }
    public IssuePriority? Priority { get; set; }
    public List<string>? Attachments { get; set; }

    public string? DueDateFrom { get; set; }
    public string? DueDateTo { get; set; }
}