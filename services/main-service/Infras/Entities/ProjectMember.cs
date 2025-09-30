using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MainService.Domain.Enums;

namespace MainService.Infras.Entities;

public class ProjectMember
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("project_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = string.Empty;

    [BsonElement("team_ids")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public List<string> TeamIds { get; set; } = new List<string>();


    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("role")]
    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;

    [BsonElement("is_pending")]
    public bool IsPending { get; set; } = true;


    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonIgnore]
    public virtual User? User { get; set; }

    [BsonIgnore]
    public virtual Project? Project { get; set; }
}