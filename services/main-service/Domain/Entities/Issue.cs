using MainService.Domain.Enums;
namespace MainService.Domain.Entities;

public class IssueDomain
{
    public string? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string? SprintId { get; set; }
    public string? CreatorId { get; set; }
    public string? TeamId { get; set; } 
    public string? AssigneeId { get; set; }
    public string? ParentId { get; set; }
    public string? ReporterId { get; set; }
    public IssueType? Type { get; set; }
    public string ColumnId { get; set; } = string.Empty;
    public ProjectColumnDomain? Column { get; set; }
    public IssuePriority? Priority { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StoryPoint { get; set; }
    public List<string> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; } = DateTime.MinValue;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueDateFrom { get; set; } = DateTime.MinValue;
    public DateTime DueDateTo { get; set; } = DateTime.MinValue;

    public string Key { get; set; } = string.Empty;

    public void AssignToSprint(string? sprintId)
    {
        SprintId = sprintId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToUser(string? assigneeId)
    {
        AssigneeId = assigneeId;
        UpdatedAt = DateTime.UtcNow;
    }
}