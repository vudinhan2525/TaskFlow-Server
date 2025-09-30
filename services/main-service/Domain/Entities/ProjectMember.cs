using MainService.Domain.Enums;

namespace MainService.Domain.Entities;

public class ProjectMemberDomain
{
    public string? Id { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public List<string> TeamIds { get; set; } = new List<string>();
    public string UserId { get; set; } = string.Empty;
    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
    public bool IsPending { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual UserDomain? User { get; set; }
    public virtual ProjectDomain? Project { get; set; }
}