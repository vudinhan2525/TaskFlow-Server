using MainService.Domain.Entities;
using MainService.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MainService.Infras.Entities;

[BsonIgnoreExtraElements]
public class Issue
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [BsonElement("creator_id")]
    public string CreatorId { get; set; } = string.Empty;

    [BsonElement("sprint_id")]
    public string? SprintId { get; set; }

    [BsonElement("assignee_id")]
    public string? AssigneeId { get; set; }

    [BsonElement("parent_id")]
    public string ParentId { get; set; } = string.Empty;

    [BsonElement("reporter_id")]
    public string ReporterId { get; set; } = string.Empty;
    
    [BsonElement("team_id")]
    public string TeamId { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public IssueType? Type { get; set; }

    [BsonElement("column_id")]
    [BsonRepresentation(BsonType.String)]
    public string ColumnId { get; set; } = string.Empty;

    [BsonElement("column")]
    public ProjectColumn? Column { get; set; }

    [BsonElement("priority")]
    [BsonRepresentation(BsonType.String)]
    public IssuePriority? Priority { get; set; }

    [BsonElement("summary")]
    public string Summary { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("story_point")]
    public int StoryPoint { get; set; }

    [BsonElement("attachments")]
    public List<string> Attachments { get; set; } = new();

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completed_at")]
    public DateTime CompletedAt { get; set; } = DateTime.MinValue;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("due_date_to")]
    public DateTime DueDateTo { get; set; } = DateTime.MinValue;

    [BsonElement("due_date_from")]
    public DateTime DueDateFrom { get; set; } = DateTime.MinValue;

    [BsonElement("key")]
    [BsonIgnoreIfDefault]
    public string Key { get; set; } = string.Empty;
    



    public static Issue FromDomain(IssueDomain domain)
    {
        return new Issue
        {
            Id = domain.Id,
            CreatorId = domain.CreatorId,
            Title = domain.Title,
            ProjectId = domain.ProjectId,
            SprintId = domain.SprintId,
            AssigneeId = domain.AssigneeId,
            ParentId = domain.ParentId,
            ReporterId = domain.ReporterId,
            Type = domain.Type,
            ColumnId = domain.ColumnId,
            Priority = domain.Priority,
            Summary = domain.Summary,
            Description = domain.Description,
            StoryPoint = domain.StoryPoint,
            Attachments = domain.Attachments,
            CreatedAt = domain.CreatedAt,
            CompletedAt = domain.CompletedAt,
            UpdatedAt = domain.UpdatedAt,
            DueDateTo = domain.DueDateTo,
            DueDateFrom = domain.DueDateFrom,
            Key = domain.Key

        };
    }

    public IssueDomain ToDomain()
    {
        var domain = new IssueDomain()
        {
            Id = Id,
            CreatorId = CreatorId,
            Title = Title,
            ParentId = ParentId,
            Type = Type,
            ColumnId = ColumnId,
            Priority = Priority,
            Summary = Summary,
            Description = Description,
            StoryPoint = StoryPoint,
            Attachments = Attachments,
            ProjectId = ProjectId,
            ReporterId = ReporterId,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            CompletedAt = CompletedAt,
            DueDateFrom = DueDateFrom,
            DueDateTo = DueDateTo,
            Key = Key
        };

        domain.AssignToSprint(SprintId);
        domain.AssignToUser(AssigneeId);

        return domain;
    }
}
