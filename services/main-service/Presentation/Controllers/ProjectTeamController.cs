using AutoMapper;
using FluentValidation;
using Grpc.Core;
using MainService.Domain.UseCases;
using MainService.Domain.Entities;
using TaskFlow.ProjectTeamService;

using MainService.Domain.Interfaces;
using BaseService;

public class ProjectTeamController : ProjectTeamService.ProjectTeamServiceBase
{
    private readonly ProjectTeamUseCase _projectTeamUseCase;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectTeamController> _logger;

    public ProjectTeamController(ProjectTeamUseCase projectTeamUseCase, IMapper mapper, ILogger<ProjectTeamController> logger)
    {
        _projectTeamUseCase = projectTeamUseCase;
        _mapper = mapper;
        _logger = logger;
    }


    
     public override async Task<CreateProjectTeamRes> CreateProjectTeam(CreateProjectTeamReq request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ProjectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId is required"));
        }
        if (string.IsNullOrEmpty(request.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required"));
        }

        var createTeamParams = new CreateTeamParams
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            Description = request.Description,
            MemberIds = request.MemberIds?.ToList()
        };

        var projectTeam = await _projectTeamUseCase.CreateProjectTeam(createTeamParams);
        return new CreateProjectTeamRes
        {
            Data = _mapper.Map<ProjectTeamRes>(projectTeam),

        };
    }
    public override async Task<ListProjectTeamsRes> ListProjectTeams(ListProjectTeamsReq request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ProjectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId is required"));
        }

        var projectTeams = await _projectTeamUseCase.ListProjectTeams(new ListProjectTeamsParams
        {
            ProjectId = request.ProjectId
        });

        var response = new ListProjectTeamsRes();
        response.Data.AddRange(_mapper.Map<List<ProjectTeamRes>>(projectTeams));
        return response;
        
    }

    public override async Task<GetProjectPermissionRes> GetProjectPermissions(GetProjectPermissionReq request, ServerCallContext context)
    {
        var permissions = await _projectTeamUseCase.GetAllPermissions();
        var response = new GetProjectPermissionRes();
        response.Permissions.AddRange(_mapper.Map<List<Permission>>(permissions));
        return response;
    }

    public override async Task<ProjectTeamRes> GetProjectTeam(GetProjectTeamReq request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ProjectId) || string.IsNullOrEmpty(request.TeamId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId and TeamId are required"));
        }
        var team = await _projectTeamUseCase.GetProjectTeam(request.ProjectId, request.TeamId);
        return _mapper.Map<ProjectTeamRes>(team);
    }

    public override async Task<GetUserTeamsRes> GetUserTeams(GetUserTeamsReq request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ProjectId) || string.IsNullOrEmpty(request.UserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId and UserId are required"));
        }

        var allTeams = await _projectTeamUseCase.ListProjectTeams(new ListProjectTeamsParams
        {
            ProjectId = request.ProjectId
        });

        var userTeams = allTeams.Where(t => t.MemberIds != null && t.MemberIds.Contains(request.UserId)).ToList();

        var res = new GetUserTeamsRes();
        res.Data.AddRange(_mapper.Map<List<ProjectTeamRes>>(userTeams));
        return res;
    }

    public override async Task<UpdateProjectTeamRes> UpdateProjectTeam(UpdateProjectTeamReq request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ProjectId) || string.IsNullOrEmpty(request.TeamId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId and TeamId are required"));
        }

        var param = new UpdateTeamParams
        {
            ProjectId = request.ProjectId,
            TeamId = request.TeamId,
            Name = string.IsNullOrEmpty(request.Name) ? null : request.Name,
            Description = request.Description,
            Permissions = request.PermissionKeys?.ToList()
        };

        var updated = await _projectTeamUseCase.UpdateProjectTeam(param);
        return new UpdateProjectTeamRes
        {
            Data = _mapper.Map<ProjectTeamRes>(updated)
        };
    }

}