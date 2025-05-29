using AutoMapper;
using MainService.Domain.Entities;
using MainService.Infras.Entities;
using TaskFlow.IssueService;
using TaskFlow.ProjectService;
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
        CreateMap<Activity, ActivityDomain>().ReverseMap();
        CreateMap<ActivityChangeEntity, ActivityChange>().ReverseMap();
        CreateMap<ProjectColumn, ProjectColumnDomain>().ReverseMap();
        CreateMap<User, UserDomain>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ReverseMap();
        CreateMap<Issue, IssueDomain>()
            .ForMember(dest => dest.SprintId, opt => opt.MapFrom(src => src.SprintId ?? string.Empty))
            .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom(src => src.AssigneeId ?? string.Empty))
            .ReverseMap();
        CreateMap<User, UserDomain>().ReverseMap();

        // Sprint mapping
        CreateMap<SprintProgress, SprintProgressDomain>().ReverseMap();
        CreateMap<SprintStatistics, SprintStatisticsDomain>().ReverseMap();
        CreateMap<Sprint, SprintDomain>().ReverseMap();
        CreateMap<ProjectMember, ProjectMemberDomain>().ReverseMap();

        // Request mappings
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
                string.IsNullOrEmpty(src.AssigneeId) ? null : src.AssigneeId));

        CreateMap<AddProjectMemberReq, ProjectMemberDomain>();
        CreateMap<UpdateProjectMemberRoleReq, ProjectMemberDomain>();

        // Response mappings
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
            .ForMember(dest => dest.SprintId, opt => opt.MapFrom(src => src.SprintId ?? string.Empty))
            .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom(src => src.AssigneeId ?? string.Empty))
            .ForMember(dest => dest.Column, opt => opt.MapFrom(src => src.Column));

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
    }
}