using MainService.Domain.Entities;
namespace MainService.Domain.Interfaces;

public interface IProjectTeamRepository
{
    Task<ProjectTeamDomain> CreateProjectTeam(CreateTeamParams param);
    Task<List<ProjectTeamDomain>> ListProjectTeams(ListProjectTeamsParams param);
    //Project Permission
    Task<List<PermissionDomain>> GetAllPermissions();
    Task<ProjectTeamDomain?> GetProjectTeamById(string projectId, string teamId);
    Task<ProjectTeamDomain?> UpdateProjectTeam(UpdateTeamParams param);
}

public class CreateTeamParams
{
    public required string ProjectId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string>? MemberIds { get; set; } = new List<string>();
}


public class ListProjectTeamsParams
{
    public required string ProjectId { get; set; }
}

public class UpdateTeamParams
{
    public required string ProjectId { get; set; }
    public required string TeamId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; } = new List<string>();
}
