using Grpc.Core;
using MainService.Domain.UseCases;
using MainService.Domain.Entities;
using TaskFlow.ProjectMemberService;
using Google.Protobuf.WellKnownTypes;
using BaseService;
using DomainProjectMemberRole = MainService.Domain.Enums.TeamMemberRole;
using GrpcProjectMemberRole = TaskFlow.ProjectMemberService.ProjectMemberRole;
using AutoMapper;
using MainService.Domain.Interfaces;

public class ProjectMemberController : ProjectMemberService.ProjectMemberServiceBase
{
    private readonly ProjectMemberUseCase _projectMemberUseCase;
    private readonly ProjectUseCase _projectUseCase;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectMemberController> _logger;

    public ProjectMemberController(
        ProjectMemberUseCase projectMemberUseCase,
        ProjectUseCase projectUseCase,
        IMapper mapper,
        ILogger<ProjectMemberController> logger)
    {
        _projectMemberUseCase = projectMemberUseCase ?? throw new ArgumentNullException(nameof(projectMemberUseCase));
        _projectUseCase = projectUseCase ?? throw new ArgumentNullException(nameof(projectUseCase));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<ProjectMemberRes> AddProjectMember(AddProjectMemberReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
        }

        try
        {
            var member = await _projectMemberUseCase.AddProjectMemberAsync(
                request.ProjectId,
                userId,
                request.UserId,
                (DomainProjectMemberRole)request.Role
            );

            return MapToProjectMemberResponse(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to project {ProjectId}", request.ProjectId);
            throw new RpcException(new Status(StatusCode.Internal, "Error adding member to project"));
        }
    }

    public override async Task<ProjectMemberRes> UpdateProjectMemberRole(UpdateProjectMemberRoleReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
        }

        try
        {
            var member = await _projectMemberUseCase.UpdateProjectMemberRoleAsync(
                request.ProjectId,
                request.UserId,
                (DomainProjectMemberRole)request.Role
            );

            return MapToProjectMemberResponse(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member role for project {ProjectId}", request.ProjectId);
            throw new RpcException(new Status(StatusCode.Internal, "Error updating member role"));
        }
    }

    public override async Task<Empty> RemoveProjectMember(RemoveProjectMemberReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
        }

        try
        {
            await _projectMemberUseCase.RemoveProjectMemberAsync(request.ProjectId, request.UserId);
            return new Empty();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from project {ProjectId}", request.ProjectId);
            throw new RpcException(new Status(StatusCode.Internal, "Error removing member"));
        }
    }

    public override async Task<ListProjectMembersRes> ListProjectMembers(ListProjectMembersReq request, ServerCallContext context)
    {
        try
        {
            var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
            if (string.IsNullOrEmpty(userId))
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
            }

            var userMember = await _projectMemberUseCase.GetByProjectAndUserAsync(request.ProjectId, userId);
            if (userMember == null || userMember.IsPending)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Access denied. User must be an approved member of the project."));
            }

            if (request.Page <= 0) request.Page = 1;
            if (request.Limit <= 0) request.Limit = 10;

            // Use search functionality if name or email filters are provided
            var (members, totalCount) = await _projectMemberUseCase.SearchProjectMembersAsync(new SearchProjectMemberQueryParams
            {
                ProjectId = request.ProjectId,
                Name = request.Name,
                Email = request.Email,
                Page = (int)request.Page,
                Limit = (int)request.Limit
            });
           

            var response = new ListProjectMembersRes
            {
                Pagination = new PaginationRes
                {
                    TotalItems = totalCount,
                    CurrentPage = request.Page,
                    Limit = request.Limit,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.Limit)
                }
            };

            response.Data.AddRange(members.Select(MapToProjectMemberResponse));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing project members for project {ProjectId}", request.ProjectId);
            throw new RpcException(new Status(StatusCode.Internal, "Error listing project members"));
        }
    }

    public override async Task<GetUserMembershipsRes> GetUserMemberships(UserMembershipsReq request, ServerCallContext context)
    {
        try
        {
            if (request.Page <= 0) request.Page = 1;
            if (request.Limit <= 0) request.Limit = 10;
            if (string.IsNullOrEmpty(request.UserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required"));
            }

            var (project_members, totalCount) = await _projectMemberUseCase.GetUserProjectsAsync(
                request.UserId,
                (int)request.Page,
                (int)request.Limit
            );

            var response = new GetUserMembershipsRes
            {
                Pagination = new PaginationRes
                {
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.Limit),
                    CurrentPage = request.Page,
                    Limit = request.Limit
                }
            };

            var projectIds = project_members.Select(pm => pm.ProjectId).Distinct().ToList();
            var projects = await _projectUseCase.ListProjects(new ListProjectParams
            {
                ProjectIds = projectIds
            });

            foreach (var member in project_members)
            {
                var project = projects.Projects.FirstOrDefault(p => p.Id == member.ProjectId);
                _logger.LogInformation("Mapping project member {MemberId} to project {ProjectId}", member.Id, member.ProjectId);
                var userMembership = new UserMembershipRes
                {
                    Id = member.Id ?? string.Empty,
                    ProjectId = member.ProjectId,
                    UserId = member.UserId,
                    Role = (GrpcProjectMemberRole)member.Role,
                    IsPending = member.IsPending,
                    CreatedAt = member.CreatedAt.ToString("O"),
                    UpdatedAt = member.UpdatedAt.ToString("O"),
                    Project = new ProjectInfo
                    {
                        Id = project.Id ?? string.Empty,
                        Name = project.Name,
                        Description = project.Key,
                        Status = project.Access.ToString(),
                        CreatedAt = project.CreatedAt.ToString("O"),
                        UpdatedAt = project.UpdatedAt.ToString("O"),
                    }
                };
                response.Data.Add(userMembership);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memberships for user {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, "Error getting user memberships"));
        }
    }

    public override async Task<ProjectMemberRes> ApproveMember(ApproveMemberReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
        }

        try
        {
            var member = await _projectMemberUseCase.ApproveProjectMemberAsync(
                request.ProjectId,
                userId,
                request.UserId
            );

            return MapToProjectMemberResponse(member);
        }
        catch (UnauthorizedAccessException)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only project owner can approve members"));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving member for project {ProjectId}", request.ProjectId);
            throw new RpcException(new Status(StatusCode.Internal, "Error approving member"));
        }
    }

    public override async Task<ProjectMemberRes> AcceptInvitation(AcceptInvitationReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
        }

        try
        {
            var member = await _projectMemberUseCase.AcceptInvitationAsync(request.ProjectId, userId);
            return MapToProjectMemberResponse(member);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation for project {ProjectId}", request.ProjectId);
            throw new RpcException(new Status(StatusCode.Internal, "Error accepting invitation"));
        }
    }

    public override async Task<Empty> RejectInvitation(RejectInvitationReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
        }

        try
        {
            await _projectMemberUseCase.RejectInvitationAsync(request.ProjectId, userId);
            return new Empty();
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting invitation for project {ProjectId}", request.ProjectId);
            throw new RpcException(new Status(StatusCode.Internal, "Error rejecting invitation"));
        }
    }

    public override async Task<Empty> RejectMember(RejectMemberReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
        }

        try
        {
            await _projectMemberUseCase.RejectProjectMemberAsync(
                request.ProjectId,
                userId,
                request.UserId
            );

            return new Empty();
        }
        catch (UnauthorizedAccessException)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only project owner can reject members"));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting member for project {ProjectId}", request.ProjectId);
            throw new RpcException(new Status(StatusCode.Internal, "Error rejecting member"));
        }
    }

    private static ProjectMemberRes MapToProjectMemberResponse(ProjectMemberDomain member)
    {
        return new ProjectMemberRes
        {
            Id = member.Id ?? string.Empty,
            ProjectId = member.ProjectId,
            UserId = member.UserId,
            Role = (GrpcProjectMemberRole)member.Role,
            IsPending = member.IsPending,
            CreatedAt = member.CreatedAt.ToString("O"),
            UpdatedAt = member.UpdatedAt.ToString("O")
        };
    }
}