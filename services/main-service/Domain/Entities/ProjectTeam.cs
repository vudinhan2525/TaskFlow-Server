using MainService.Domain.Enums;
namespace MainService.Domain.Entities;

public class ProjectTeamDomain
{
    public string? Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public List<string> PermissionKeys { get; set; } = new List<string>();
    public List<string> MemberIds { get; set; } = new List<string>();
}

public class PermissionDomain
{
    public string? Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
