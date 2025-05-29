namespace MainService.Domain.Entities;

public class SprintProgressDomain
{
    public DateTime Date { get; set; }
    public int PlannedTotal { get; set; }
    public int CompletedCount { get; set; }
    public int RemainingCount { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int RemainingStoryPoints { get; set; }
}

public class SprintStatisticsDomain
{
    public int TotalIssues { get; set; }
    public int CompletedIssues { get; set; }
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public Dictionary<string, int> IssuesByType { get; set; } = new();
    public Dictionary<string, int> IssuesByStatus { get; set; } = new();
    public List<SprintProgressDomain> DailyProgress { get; set; } = new();
}

public class SprintDomain
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateStarted { get; set; }
    public DateTime DateEnded { get; set; }
    public int Duration { get; set; }
    public string Goal { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string ProjectId { get; set; } = string.Empty;
    public SprintStatisticsDomain Statistics { get; set; } = new();
}