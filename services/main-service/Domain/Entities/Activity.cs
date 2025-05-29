namespace MainService.Domain.Entities;

public class ActivityChange
{
    public string? Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class ActivityDomain
{
    public string? Id { get; set; }
    public required string IssueId { get; set; }
    public string? UserId { get; set; }
    public string? ActionType { get; set; }
    public List<ActivityChange>? Changes { get; set; }
    public string? SprintId { get; set; }  // Track sprint-related activities

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class ActivityAction
{
    public const string ISSUE_UPDATED = "ISSUE_UPDATED";
    public const string ISSUE_CREATED = "ISSUE_CREATED";
    public const string ISSUE_DUE_DATE_CHANGED = "ISSUE_DUE_DATE_CHANGED";
    public const string ISSUE_MOVED_TO_SPRINT = "ISSUE_MOVED_TO_SPRINT";
    public const string ISSUE_REMOVED_FROM_SPRINT = "ISSUE_REMOVED_FROM_SPRINT";
    public const string ISSUE_STATUS_CHANGED = "ISSUE_STATUS_CHANGED";
    public const string SPRINT_PROGRESS_UPDATED = "SPRINT_PROGRESS_UPDATED";
}

public static class ActivityField
{
    public const string DUE_DATE = "due_date";
    public const string SPRINT = "sprint";
    public const string STATUS = "status";
    public const string STORY_POINTS = "story_points";
    public const string COMPLETED_COUNT = "completed_count";
    public const string REMAINING_COUNT = "remaining_count";
}