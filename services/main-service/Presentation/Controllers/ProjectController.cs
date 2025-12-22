using AutoMapper;
using FluentValidation;
using Grpc.Core;
using MainService.Domain.UseCases;
using MainService.Domain.Entities;
using TaskFlow.ProjectService;
using MainService.Domain.Interfaces;
using BaseService;


public class ProjectController : ProjectService.ProjectServiceBase
{
    private readonly ProjectUseCase _projectUseCase;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateProjectReq> _createProjectValidator;
    private readonly IValidator<UpdateProjectReq> _updateProjectValidator;
    private readonly IValidator<ListProjectsReq> _listProjectsValidator;
    private readonly ILogger<ProjectController> _logger;
    public ProjectController(
        ProjectUseCase projectUseCase,
        IMapper mapper,
        ILogger<ProjectController> logger,
        IValidator<CreateProjectReq> createProjectValidator,
        IValidator<UpdateProjectReq> updateProjectValidator,
        IValidator<ListProjectsReq> listProjectsValidator)
    {
        _projectUseCase = projectUseCase;
        _mapper = mapper;
        _logger = logger;
        _createProjectValidator = createProjectValidator;
        _updateProjectValidator = updateProjectValidator;
        _listProjectsValidator = listProjectsValidator;
    }

    public override async Task<CreateProjectRes> CreateProject(CreateProjectReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User must be authenticated"));
        }

        var validationResult = await _createProjectValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        try
        {
            var projectDomain = _mapper.Map<ProjectDomain>(request);
            projectDomain.OwnerId = userId; // Set the authenticated user as owner

            var result = await _projectUseCase.CreateProject(projectDomain);
            return new CreateProjectRes
            {
                Data = _mapper.Map<ProjectRes>(result),
                Message = "Create project success.",
                Status = "success"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            throw new RpcException(new Status(StatusCode.Internal, "Error creating project"));
        }
    }
    public override async Task<ProjectRes> GetProject(GetProjectReq request, ServerCallContext context)
    {
        var result = await _projectUseCase.GetProject(request.Id);
        return _mapper.Map<ProjectRes>(result);
    }
    public override async Task<ProjectRes> UpdateProject(UpdateProjectReq request, ServerCallContext context)
    {
        var validationResult = await _updateProjectValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var projectDomain = _mapper.Map<ProjectDomain>(request);
        var result = await _projectUseCase.UpdateProject(projectDomain);
        return _mapper.Map<ProjectRes>(result);
    }
    public override async Task<Google.Protobuf.WellKnownTypes.Empty> DeleteProject(DeleteProjectReq request, ServerCallContext context)
    {
        try
        {
            await _projectUseCase.DeleteProject(request.Id);
            return new Google.Protobuf.WellKnownTypes.Empty();
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }
    public override async Task<ListProjectsRes> ListProjects(ListProjectsReq request, ServerCallContext context)
    {
        if (request.Page <= 0) request.Page = 1;
        if (request.Limit <= 0) request.Limit = 10;

        var validationResult = await _listProjectsValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var (projects, totalCount) = await _projectUseCase.ListProjects(new ListProjectParams
        {
            Limit = request.Limit,
            Page = request.Page,
            Kw = request.Kw,
            Sort = request.Sort,
            ProjectIds = request.ProjectIds.ToList()
        });
        var totalPages = (int)Math.Ceiling((double)totalCount / request.Limit);

        var response = new ListProjectsRes();
        response.Data.AddRange(_mapper.Map<List<ProjectRes>>(projects));
        response.Pagination = new BaseService.PaginationRes
        {
            TotalItems = totalCount,
            TotalPages = totalPages,
            CurrentPage = request.Page,
            Limit = request.Limit
        };
        return response;
    }
    public override async Task<ListProjectsRes> GetUserProjects(UserProjectsReq request, ServerCallContext context)
    {
        if (request.Page <= 0) request.Page = 1;
        if (request.Limit <= 0) request.Limit = 10;
        if (string.IsNullOrEmpty(request.UserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required"));
        }

        var (projects, totalCount) = await _projectUseCase.ListProjects(new ListProjectParams
        {
            Limit = request.Limit,
            Page = request.Page,
            UserId = request.UserId,
            Kw = request.Kw,
            Sort = request.Sort
        });
        var totalPages = (int)Math.Ceiling((double)totalCount / request.Limit);
        var response = new ListProjectsRes();
        response.Data.AddRange(_mapper.Map<List<ProjectRes>>(projects));
        response.Pagination = new BaseService.PaginationRes
        {
            TotalItems = totalCount,
            TotalPages = totalPages,
            CurrentPage = request.Page,
            Limit = request.Limit
        };
        return response;
    }
    public override async Task<CreateColumnRes> CreateProjectColumn(CreateColumnReq request, ServerCallContext context)
    {
        var projectColumn = await _projectUseCase.CreateColumn(new CreateColumnParams
        {
            Name = request.Name,
            ProjectId = request.ProjectId
        });
        return new CreateColumnRes { Data = _mapper.Map<ColumnRes>(projectColumn) };
    }
    public override async Task<GetColumnsRes> GetProjectColumns(GetColumnsReq request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ProjectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId is required"));
        }
     
        var projectColumns = await _projectUseCase.GetAllColumns(new ListProjectColumnsParams
        {
            ProjectId = request.ProjectId,
            Keyword = request.Keyword,
            CreatedAtFrom = ConverterUtils.ParseIsoDateTime(request.CreatedAtFrom),
            CreatedAtTo = ConverterUtils.ParseIsoDateTime(request.CreatedAtTo),
            DueDateFrom = ConverterUtils.ParseIsoDateTime(request.DueDateFrom),
            DueDateTo = ConverterUtils.ParseIsoDateTime(request.DueDateTo),
            AssigneeIds = request.AssigneeIds.ToList(),
            SprintIds = request.SprintIds.ToList(),
            Types = request.Types_.ToList(),
            Priorities = request.Priorities.ToList(),
            ColumnIds = request.ColumnIds.ToList(),
            ActiveSprintOnly = request.ActiveSprintOnly, 
        });



        var response = new GetColumnsRes();
        response.Data.AddRange(_mapper.Map<List<ColumnRes>>(projectColumns));
        return response;
    }
    public override async Task<UpdateColumnsOrderRes> UpdateOrderProjectColumns(UpdateColumnsOrderReq request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ProjectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProjectId is required"));
        }
        if (request.Columns == null || request.Columns.Count == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Columns list cannot be empty"));
        }
        if (request.Columns.Any(c => c.Order < 1))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Column order must be positive"));
        }

        var columnOrders = request.Columns.Select(c => new ColumnOrders
        {
            Id = c.Id,
            Order = c.Order
        }).ToList();

        var projectColumns = await _projectUseCase.UpdateColumnsOrder(new UpdateColumnOrdersParams
        {
            Columns = columnOrders,
            ProjectId = request.ProjectId
        });

        var response = new UpdateColumnsOrderRes();
        response.Data.AddRange(_mapper.Map<List<ColumnRes>>(projectColumns));

        return response;
    }
    public override async Task<UpdateColumnRes> UpdateColumnProject(UpdateColumnReq request, ServerCallContext context)
    {
        var columnUpdated = await _projectUseCase.UpdateColumn(new UpdateColumnParams
        {
            ColumnId = request.ColumnId,
            Name = request.Name
        });

        return new UpdateColumnRes { Data = _mapper.Map<ColumnRes>(columnUpdated) };
    }
    public override async Task<DeleteColumnRes> DeleteColumn(DeleteColumnReq request, ServerCallContext context)
    {
        await _projectUseCase.DeleteColumn(new DeleteColumnParams
        {
            ColumnId = request.ColumnId
        });

        return new DeleteColumnRes();
    }
}