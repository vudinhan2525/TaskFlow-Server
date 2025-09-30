using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Domain.Enums;
using Grpc.Core;

namespace MainService.Domain.UseCases;

public class ProjectTeamUseCase
{
    private readonly IProjectTeamRepository _projectTeamRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProjectTeamUseCase> _logger;

    public ProjectTeamUseCase(
        IProjectTeamRepository projectTeamRepository,
        IUserRepository userRepository,
        ILogger<ProjectTeamUseCase> logger)
    {
        _projectTeamRepository = projectTeamRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

     public async Task<ProjectTeamDomain> CreateProjectTeam(CreateTeamParams param)
    {
        // Validate project exists and project_id is valid ObjectId
        if (string.IsNullOrEmpty(param.ProjectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId is required"));
        }
        
        if (string.IsNullOrEmpty(param.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Team name is required"));
        }
        
        return await _projectTeamRepository.CreateProjectTeam(param);
    }
    public async Task<List<ProjectTeamDomain>> ListProjectTeams(ListProjectTeamsParams param)
    {
        return await _projectTeamRepository.ListProjectTeams(param);
    }
    public async Task<List<PermissionDomain>> GetAllPermissions()
    {
        return await _projectTeamRepository.GetAllPermissions();
    }

    public async Task<ProjectTeamDomain> GetProjectTeam(string projectId, string teamId)
    {
        var team = await _projectTeamRepository.GetProjectTeamById(projectId, teamId);
        if (team == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Project team not found"));
        }
        return team;
    }

    public async Task<ProjectTeamDomain> UpdateProjectTeam(UpdateTeamParams param)
    {
        if (string.IsNullOrEmpty(param.ProjectId) || string.IsNullOrEmpty(param.TeamId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId and TeamId are required"));
        }
        var updated = await _projectTeamRepository.UpdateProjectTeam(param);
        if (updated == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Project team not found"));
        }
        return updated;
    }
}