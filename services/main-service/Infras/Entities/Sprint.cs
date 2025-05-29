using MainService.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MainService.Infras.Entities;

public class SprintProgress
{
    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("planned_total")]
    public int PlannedTotal { get; set; }

    [BsonElement("completed_count")]
    public int CompletedCount { get; set; }

    [BsonElement("remaining_count")]
    public int RemainingCount { get; set; }

    [BsonElement("completed_story_points")]
    public int CompletedStoryPoints { get; set; }

    [BsonElement("remaining_story_points")]
    public int RemainingStoryPoints { get; set; }
}

public class SprintStatistics
{
    [BsonElement("total_issues")]
    public int TotalIssues { get; set; }

    [BsonElement("completed_issues")]
    public int CompletedIssues { get; set; }

    [BsonElement("total_story_points")]
    public int TotalStoryPoints { get; set; }

    [BsonElement("completed_story_points")]
    public int CompletedStoryPoints { get; set; }

    [BsonElement("issues_by_type")]
    public Dictionary<string, int> IssuesByType { get; set; } = new();

    [BsonElement("issues_by_status")]
    public Dictionary<string, int> IssuesByStatus { get; set; } = new();

    [BsonElement("daily_progress")]
    public List<SprintProgress> DailyProgress { get; set; } = new();
}

public class Sprint
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("date_started")]
    public DateTime DateStarted { get; set; }

    [BsonElement("date_ended")]
    public DateTime DateEnded { get; set; }

    [BsonElement("duration")]
    public int Duration { get; set; }

    [BsonElement("goal")]
    public string Goal { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [BsonElement("statistics")]
    public SprintStatistics Statistics { get; set; } = new();
}