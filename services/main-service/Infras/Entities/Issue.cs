using MainService.Domain.Entities;
using MainService.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MainService.Infras.Entities;

public class Issue
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [BsonElement("sprint_id")]
    public string? SprintId { get; set; }

    [BsonElement("assignee_id")]
    public string? AssigneeId { get; set; }

    [BsonElement("parent_id")]
    public string ParentId { get; set; } = string.Empty;

    [BsonElement("reporter_id")]
    public string ReporterId { get; set; } = string.Empty;

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
    [BsonSerializer(typeof(DescriptionSerializer))]
    public string Description { get; set; } = string.Empty;

    [BsonElement("story_point")]
    public int StoryPoint { get; set; }

    [BsonElement("attachments")]
    public List<string> Attachments { get; set; } = new();

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static Issue FromDomain(IssueDomain domain)
    {
        return new Issue
        {
            Id = domain.Id,
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
            UpdatedAt = domain.UpdatedAt
        };
    }

    public IssueDomain ToDomain()
    {
        var domain = new IssueDomain()
        {
            Id = Id,
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
            UpdatedAt = UpdatedAt
        };

        domain.AssignToSprint(SprintId);
        domain.AssignToUser(AssigneeId);

        return domain;
    }
}

public class DescriptionSerializer : SerializerBase<string>
{
    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.GetCurrentBsonType() == BsonType.Document)
        {
            var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
            if (document.Contains("content"))
            {
                return document["content"].AsString;
            }
            return string.Empty;
        }
        return context.Reader.ReadString();
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        context.Writer.WriteString(value);
    }
}
