using FluentValidation;
using MainService.Domain.Interfaces;
using MainService.Domain.UseCases;
using MainService.Infras;
using MainService.Infras.Repositories;
using MainService.Presentation.Validator.Users;


public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        // Database Service
        services.AddSingleton<MongoDbService>();
        // Use Cases
        services.AddScoped<UserUseCase>();
        services.AddScoped<ProjectUseCase>();
        services.AddScoped<SprintUseCase>();
        services.AddScoped<IssueUseCase>();
        services.AddScoped<ProjectMemberUseCase>();
        services.AddScoped<CommentUseCase>();
        services.AddScoped<OtpTokenUseCase>();
        services.AddScoped<ProjectTeamUseCase>();

        // Repositories
        services.AddSingleton<ITransactionRepo, MongoTransactionRepo>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IProjectRepository, ProjectRepository>();
        services.AddSingleton<ISprintRepository, SprintRepository>();
        services.AddSingleton<IIssueRepository, IssueRepository>();
        services.AddSingleton<IActivitiesRepository, ActivitiesRepository>();
        services.AddSingleton<IProjectMemberRepository, ProjectMemberRepository>();
        services.AddSingleton<ICommentsRepository, CommentsRepository>();
        services.AddSingleton<IOtpTokenRepository, OtpTokenRepository>();
        services.AddSingleton<IPublisherService, KafkaPublisher>();
        services.AddSingleton<IProjectTeamRepository, ProjectTeamRepository>();

        // Workers
        services.AddHostedService<ActivitiesConsumer>();
        return services;
    }

    public static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<AuthenticationInterceptor>();
            options.Interceptors.Add<GrpcExceptionInterceptor>();
        });
        services.AddGrpcReflection();

        return services;
    }

    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateProjectValidator>();
        services.AddValidatorsFromAssemblyContaining<UpdateProjectValidator>();

        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
        services.AddValidatorsFromAssemblyContaining<LoginUserValidator>();
        services.AddValidatorsFromAssemblyContaining<UpdateUserValidator>();
        services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();
        services.AddValidatorsFromAssemblyContaining<ChangePasswordValidator>();

        services.AddValidatorsFromAssemblyContaining<CreateIssueValidator>();
        services.AddValidatorsFromAssemblyContaining<UpdateIssueValidator>();

        return services;
    }
}
