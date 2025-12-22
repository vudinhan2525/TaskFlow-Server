using AutoMapper;
using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Infras.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using TaskFlow.SprintService;

namespace MainService.Infras.Repositories;

public class SprintRepository : ISprintRepository
{
    private readonly IMongoCollection<Sprint> _sprints;
    private readonly IMapper _mapper;
    private readonly ILogger<SprintRepository> _logger;
    private readonly IProjectRepository _projectRepository;
    private readonly IIssueRepository _issueRepository;


    public SprintRepository(MongoDbService mongoDbService, IIssueRepository issueRepository, IMapper mapper, ILogger<SprintRepository> logger, IProjectRepository projectRepository)
    {
        var database = mongoDbService.Database;
        _sprints = database.GetCollection<Sprint>("sprints");
        _mapper = mapper;
        _logger = logger;
        _projectRepository = projectRepository;
        _issueRepository = issueRepository;

        var projectIdKeys = Builders<Sprint>.IndexKeys.Ascending(s => s.ProjectId);
        var projectIdModel = new CreateIndexModel<Sprint>(projectIdKeys);
        _sprints.Indexes.CreateOne(projectIdModel);

    }

    public async Task<SprintDomain> CreateSprint(SprintDomain sprintDomain)
    {
        var sprintEntity = _mapper.Map<Sprint>(sprintDomain);
        await _sprints.InsertOneAsync(sprintEntity);
        sprintDomain.Id = sprintEntity.Id;
        return sprintDomain;
    }

    public async Task<SprintDomain> GetSprint(string id)
    {
        var sprintEntity = await _sprints.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (sprintEntity == null)
            throw new Exception("Sprint not found");
        return _mapper.Map<SprintDomain>(sprintEntity);
    }

    public async Task<SprintDomain> UpdateSprint(SprintDomain sprintDomain)
    {
        var sprintEntity = _mapper.Map<Sprint>(sprintDomain);
        var result = await _sprints.ReplaceOneAsync(s => s.Id == sprintDomain.Id, sprintEntity);
        if (result.MatchedCount == 0)
            throw new Exception("Sprint not found");
        return sprintDomain;
    }

    public async Task DeleteSprint(string id)
    {
        var result = await _sprints.DeleteOneAsync(s => s.Id == id);
        if (result.DeletedCount == 0)
            throw new Exception("Sprint not found");
    }

    public async Task<(List<SprintDomain> Sprints, int TotalCount)> ListSprints(ListSprintParams param)
    {
        var filterBuilder = Builders<Sprint>.Filter;
        var filters = new List<FilterDefinition<Sprint>>();

        // Filter by ProjectId
        if (!string.IsNullOrEmpty(param.ProjectId))
        {
            filters.Add(filterBuilder.Eq(s => s.ProjectId, param.ProjectId));
        }

        // Filter by SprintIds
        if (param.SprintIds != null && param.SprintIds.Any())
        {
            filters.Add(filterBuilder.In(s => s.Id, param.SprintIds));
        }

        var filter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

        var totalCount = await _sprints.CountDocumentsAsync(filter);
        var sprints = await _sprints.Find(filter)
            .Skip((param.Page - 1) * param.Limit)
            .Limit(param.Limit)
            .ToListAsync();

        return (_mapper.Map<List<SprintDomain>>(sprints), (int)totalCount);
    }


    public async Task<SprintStats> GetSprintStats(string sprint_id, string project_id)
    {
        var columns = await _projectRepository.FindColumnsByProjectId(new ListProjectColumnsParams { ProjectId = project_id });
        string? lastColId = columns.Count > 0 ? columns[^1].Id : null;
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("_id", ObjectId.Parse(sprint_id))),
            new BsonDocument("$addFields", new BsonDocument("sprint_id_str", new BsonDocument("$toString", "$_id"))),

            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "issues" },
                { "localField", "sprint_id_str" },
                { "foreignField", "sprint_id" },
                { "as", "issues" }
            }),

            new BsonDocument("$project", new BsonDocument
            {
                { "total_issues", new BsonDocument("$size", "$issues") },
                { "completed_issues", new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
                    {
                        { "input", "$issues" },
                        { "as", "issue" },
                        { "cond", new BsonDocument("$eq", new BsonArray { "$$issue.column_id", lastColId }) }
                    }))
                },
                { "total_story_point", new BsonDocument("$sum", "$issues.story_point") },
                { "completed_story_point", new BsonDocument("$sum", new BsonDocument("$map", new BsonDocument
                    {
                        { "input", "$issues" },
                        { "as", "issue" },
                        { "in", new BsonDocument("$cond", new BsonArray
                            {
                                new BsonDocument("$eq", new BsonArray { "$$issue.column_id", lastColId }),
                                "$$issue.story_point",
                                0
                            })
                        }
                    }))
                }
            })
        };
        var rawResult = await _sprints.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();

        return new SprintStats
        {
            TotalIssues = rawResult.GetValue("total_issues", 0).ToInt32(),
            CompletedIssues = rawResult.GetValue("completed_issues", 0).ToInt32(),
            TotalStoryPoint = rawResult.GetValue("total_story_point", 0).ToInt32(),
            CompletedStoryPoint = rawResult.GetValue("completed_story_point", 0).ToInt32()
        };
    }
    public async Task<List<SprintDailyStats>> GetSprintDailyStats(string sprint_id)
    {
        var sprint = await _sprints.Find(x => x.Id == sprint_id).FirstOrDefaultAsync();
        if (sprint == null)
            return [];

        var start = sprint.DateStarted.ToUniversalTime().Date;
        var end = sprint.DateEnded.ToUniversalTime().Date;

        var (issues, totalCount) = await _issueRepository.ListIssues(new GetIssuesParams
        {
            SprintIds = [sprint_id]
        });

        var dailyStats = new List<SprintDailyStats>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var completedCount = issues.Count(i =>
                i.CompletedAt.ToUniversalTime().Date <= date);

            dailyStats.Add(new SprintDailyStats
            {
                Date = date.ToString(),
                CompletedIssues = completedCount,
                RemainingIssues = totalCount - completedCount
            });
        }
        return dailyStats;
    }

}