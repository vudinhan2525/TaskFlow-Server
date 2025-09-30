using AutoMapper;
using MainService.Domain.Entities;
using MainService.Domain.Common;
using MainService.Domain.Interfaces;
using MainService.Infras.Entities;
using TaskFlow.IssueService;
using TaskFlow.ProjectService;
using TaskFlow.ProjectTeamService;
using TaskFlow.SprintService;
using TaskFlow.UserService;
using BaseService;
using TaskFlow.ProjectMemberService;

namespace CamQuizzBE.Applications.Helpers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Project, ProjectDomain>().ReverseMap();
        CreateMap<Comment, CommentDomain>().ReverseMap();
        CreateMap<Activity, ActivityDomain>().ReverseMap();
        CreateMap<ActivityChangeEntity, ActivityChange>().ReverseMap();
        CreateMap<ProjectColumn, ProjectColumnDomain>().ReverseMap();
        CreateMap<User, UserDomain>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ReverseMap();
        CreateMap<Issue, IssueDomain>()
            .ForMember(dest => dest.SprintId, opt => opt.MapFrom(src => src.SprintId ?? string.Empty))
            .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom(src => src.AssigneeId ?? string.Empty))
            .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src => src.TeamId ?? string.Empty))
            .ReverseMap();
        CreateMap<User, UserDomain>().ReverseMap();
        CreateMap<Sprint, SprintDomain>().ReverseMap();
        CreateMap<ProjectMember, ProjectMemberDomain>().ReverseMap();
        CreateMap<OtpToken, OtpTokenDomain>().ReverseMap();
        CreateMap<ProjectTeam, ProjectTeamDomain>().ReverseMap();
        // Removed invalid mapping: Infras.Entities.Permission does not exist

        CreateMap<CreateSprintReq, SprintDomain>();
        CreateMap<UpdateSprintReq, SprintDomain>();
        CreateMap<CreateProjectReq, ProjectDomain>();
        CreateMap<UpdateProjectReq, ProjectDomain>();
        CreateMap<CreateIssueReq, IssueDomain>()
            .ForMember(dest => dest.SprintId, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.SprintId) ? null : src.SprintId))
            .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.AssigneeId) ? null : src.AssigneeId));

        CreateMap<UpdateIssueReq, IssueDomain>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SprintId, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.SprintId) ? null : src.SprintId))
            .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.AssigneeId) ? null : src.AssigneeId))
            .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.TeamId) ? null : src.TeamId))
                ;

        CreateMap<AddProjectMemberReq, ProjectMemberDomain>();
        CreateMap<UpdateProjectMemberRoleReq, ProjectMemberDomain>();
        CreateMap<CreateTeamParams, ProjectTeam>();
     

        CreateMap<ProjectDomain, ProjectRes>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")))
            .ForMember(dest => dest.ProjectMembers, opt => opt.MapFrom(src => src.ProjectMembers));

        CreateMap<ProjectColumnDomain, ColumnRes>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")));

        CreateMap<IssueDomain, IssueRes>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CompletedAt == DateTime.MinValue ? DateTime.MinValue.ToString("o") : src.CompletedAt.ToString("o")))
            .ForMember(dest => dest.DueDateFrom, opt => opt.MapFrom(src => src.DueDateFrom == DateTime.MinValue ? string.Empty : src.DueDateFrom.ToString("o")))
            .ForMember(dest => dest.DueDateTo, opt => opt.MapFrom(src => src.DueDateTo == DateTime.MinValue ? string.Empty : src.DueDateTo.ToString("o")))
            .ForMember(dest => dest.SprintId, opt => opt.MapFrom(src => src.SprintId ?? string.Empty))
            .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom(src => src.AssigneeId ?? string.Empty))
            .ForMember(dest => dest.Column, opt => opt.MapFrom(src => src.Column))
            .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src => src.TeamId ?? string.Empty));

        CreateMap<UserDomain, UserRes>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<SprintDomain, SprintRes>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")))
            .ForMember(dest => dest.DateStarted, opt => opt.MapFrom(src => src.DateStarted.ToString("o")))
            .ForMember(dest => dest.DateEnded, opt => opt.MapFrom(src => src.DateEnded.ToString("o")));

        CreateMap<UserDomain, UserInfo>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));

        CreateMap<ProjectMemberDomain, ProjectMemberRes>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

        CreateMap<ProjectMemberDomain, UserMembershipRes>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")));

        CreateMap<CommentDomain, CommentRes>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")));

        CreateMap<ActivityDomain, ActivityRes>()
           .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? ""))
           .ForMember(dest => dest.IssueId, opt => opt.MapFrom(src => src.IssueId))
           .ForMember(dest => dest.ActionType, opt => opt.MapFrom(src => src.ActionType ?? ""))
           .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
           .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")))
           .ForMember(dest => dest.Changes, opt => opt.MapFrom(src => src.Changes ?? new List<ActivityChange>()));

        CreateMap<ActivityChange, ActivityChangesRes>()
            .ForMember(dest => dest.Field, opt => opt.MapFrom(src => src.Field ?? ""))
            .ForMember(dest => dest.OldValue, opt => opt.MapFrom(src => src.OldValue ?? ""))
            .ForMember(dest => dest.NewValue, opt => opt.MapFrom(src => src.NewValue ?? ""));

        CreateMap<ProjectTeamDomain, ProjectTeamRes>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("o")))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("o")))
            .ForMember(dest => dest.PermissionKeys, opt => opt.MapFrom(src => src.PermissionKeys))
            .ForMember(dest => dest.MemberIds, opt => opt.MapFrom(src => src.MemberIds));
        CreateMap<ProjectPermission, PermissionDomain>().ReverseMap();
        CreateMap<PermissionDomain, Permission>().ReverseMap();



    }
}