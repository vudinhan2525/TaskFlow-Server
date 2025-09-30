using AutoMapper;
using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Infras.Entities;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using MongoDB.Bson.Serialization;

namespace MainService.Infras.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly IMongoCollection<Project> _projects;
    private readonly IMongoCollection<ProjectColumn> _projectColumns;
    private readonly IMongoCollection<ProjectTeam> _projectTeams;
    private readonly IMongoCollection<Issue> _issues;
    private readonly IMongoCollection<ProjectMember> _teamMembers;  // Changed to match DB collection name
    private readonly IMongoCollection<User> _users;
    
    private readonly ILogger<ProjectRepository> _logger;
    private readonly IMapper _mapper;

    public ProjectRepository(MongoDbService mongoDbService, IMapper mapper, ILogger<ProjectRepository> logger)
    {
        var database = mongoDbService.Database;
        _projects = database.GetCollection<Project>("projects");
        _issues = database.GetCollection<Issue>("issues");
        _projectColumns = database.GetCollection<ProjectColumn>("project_column");
        _teamMembers = database.GetCollection<ProjectMember>("team_members");
        _users = database.GetCollection<User>("users");
        _mapper = mapper;
        _logger = logger;
        // Ensure index on Key
        var indexKeysDefinition = Builders<Project>.IndexKeys.Ascending(p => p.Key);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<Project>(indexKeysDefinition, indexOptions);
        _projects.Indexes.CreateOne(indexModel);
    }

    public async Task<ProjectDomain> CreateProject(ProjectDomain projectDomain)
    {
        var projectEntity = _mapper.Map<Project>(projectDomain);
        await _projects.InsertOneAsync(projectEntity);
        projectDomain.Id = projectEntity.Id;
        return projectDomain;
    }

    public async Task<ProjectDomain> GetProject(string id)
    {
        var project = await _projects.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (project == null)
            throw new Exception("Project not found");

        return _mapper.Map<ProjectDomain>(project);
    }

    public async Task<ProjectDomain> UpdateProject(ProjectDomain projectDomain)
    {
        var projectEntity = _mapper.Map<Project>(projectDomain);
        var result = await _projects.ReplaceOneAsync(p => p.Id == projectDomain.Id, projectEntity);
        if (result.MatchedCount == 0)
            throw new Exception("Project not found");
        return projectDomain;
    }

    public async Task DeleteProject(string id)
    {
        var result = await _projects.DeleteOneAsync(p => p.Id == id);
        if (result.DeletedCount == 0)
            throw new Exception("Project not found");
    }

    public async Task<(List<ProjectDomain> Projects, int TotalCount)> ListProjects(ListProjectParams query)
    {
        var filterBuilder = Builders<Project>.Filter;
        var filters = new List<FilterDefinition<Project>>();

        // Filter by project IDs (preserving order is not supported directly with .In)
        if (query.ProjectIds != null && query.ProjectIds.Any())
        {
            filters.Add(filterBuilder.In(x => x.Id, query.ProjectIds));
        }

        // Filter by owner ID
        if (!string.IsNullOrEmpty(query.UserId))
        {
            filters.Add(filterBuilder.Eq(x => x.OwnerId, query.UserId));
        }

        // Filter by keyword (case-insensitive search on name)
        if (!string.IsNullOrEmpty(query.Kw))
        {
            filters.Add(filterBuilder.Regex(x => x.Name, new BsonRegularExpression(query.Kw, "i")));
        }

        var matchFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

        // Sorting
        var sortField = "created_at";
        var sortDescending = true;

        if (!string.IsNullOrEmpty(query.Sort))
        {
            sortField = query.Sort.TrimStart('-');
            sortDescending = query.Sort.StartsWith("-");
            sortField = sortField.ToLower() switch
            {
                "name" => "name",
                "key" => "key",
                "created_at" => "created_at",
                "updated_at" => "updated_at",
                _ => "created_at"
            };
        }

        var sortDoc = new BsonDocument(sortField, sortDescending ? -1 : 1);

        // Query database
        var totalCount = await _projects.CountDocumentsAsync(matchFilter);
        var projects = await _projects.Find(matchFilter)
            .Sort(sortDoc)
            .Skip((query.Page - 1) * query.Limit)
            .Limit(query.Limit)
            .ToListAsync();

        return (_mapper.Map<List<ProjectDomain>>(projects), (int)totalCount);
    }


    public async Task<ProjectColumnDomain> CreateColumn(ProjectColumnDomain projectColumn)
    {
        var projectColumnEntity = _mapper.Map<ProjectColumn>(projectColumn);
        await _projectColumns.InsertOneAsync(projectColumnEntity);

        projectColumn.Id = projectColumnEntity.Id;
        return projectColumn;
    }

    public async Task<List<ProjectColumnDomain>> FindColumnsByProjectId(ListProjectColumnsParams param)
    {

        var issueQuery = MongoUtils.BuildExprMongo(param, new Dictionary<string, (string field, string op, string? extra)>
        {
            { "DueDateFrom", ("due_date_from", "$gte", null) },
            { "DueDateTo", ("due_date_to", "$lte", null) },
            { "CreatedAtFrom", ("created_at", "$gte", null) },
            { "CreatedAtTo", ("created_at", "$lte", null) },
            { "AssigneeIds", ("assignee_id", "$in", null) },
            { "SprintIds", ("sprint_id", "$in", null) },
            { "Types", ("type", "$in", null) },
            { "Priorities", ("priority", "$in", null) },
            { "Keyword", ("title", "$regex", null) }
        }, excludeProps: ["ProjectId", "ColumnIds"]);
        issueQuery.Insert(0, new BsonDocument("$in", new BsonArray { "$_id", "$$issueIds" }));
        var issueMatch = new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$and", issueQuery)));

        var columnQuery = MongoUtils.BuildExprMongo(param, new Dictionary<string, (string field, string op, string? extra)>
        {
            { "ColumnIds", ("_id", "$in", "is_object_id") },
            { "ProjectId", ("project_id", "$eq", "is_object_id") },
        }, excludeProps: ["Keyword", "DueDateFrom", "DueDateTo", "CreatedAtFrom", "CreatedAtTo", "AssigneeIds", "SprintIds", "Types", "Priorities"]);
        var columnMatch = new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$and", columnQuery)));

        var pipeline = new[]
        {
            columnMatch,
            new BsonDocument { { "$sort", new BsonDocument { { "order", 1 } } } },
            new BsonDocument {
                {
                    "$lookup", new BsonDocument {
                        { "from", "issues" },
                        { "let", new BsonDocument("issueIds", "$issue_ids") },
                        { "pipeline", new BsonArray {
                            issueMatch,
                            new BsonDocument {
                                { "$sort", new BsonDocument("created_at", 1) }
                            }
                        }},
                        { "as", "issues" }
                    }
                }
            },
        };

        var rawResult = await _projectColumns.Aggregate<BsonDocument>(pipeline).ToListAsync();
        var columnsEntity = new List<ProjectColumn>();

        foreach (var bsonDoc in rawResult)
        {
            var column = BsonSerializer.Deserialize<ProjectColumn>(bsonDoc);
            columnsEntity.Add(column);
        }

        var columnsDomain = _mapper.Map<List<ProjectColumnDomain>>(columnsEntity);
        return columnsDomain;
    }

    public async Task<ProjectColumnDomain> FindColumn(GetColumnParams param)
    {
        var filterBuilder = Builders<ProjectColumn>.Filter;
        FilterDefinition<ProjectColumn> filter = FilterDefinition<ProjectColumn>.Empty;

        if (!string.IsNullOrEmpty(param.ColumnId))
        {
            filter = filterBuilder.Eq(c => c.Id, param.ColumnId);
        }
        else if (!string.IsNullOrEmpty(param.Name))
        {
            filter = filterBuilder.Eq(c => c.Name, param.Name);
        }

        var column = await _projectColumns.Find(filter).FirstOrDefaultAsync();
        return _mapper.Map<ProjectColumnDomain>(column);
    }

    public async Task UpdateColumnOrder(string projectId, string columnId, int order)
    {
        var filter = Builders<ProjectColumn>.Filter
        .Where(c => c.ProjectId == projectId && c.Id == columnId);

        var update = Builders<ProjectColumn>.Update
        .Set(c => c.Order, order)
        .Set(c => c.UpdatedAt, DateTime.UtcNow);

        await _projectColumns.UpdateOneAsync(filter, update);
    }

    public async Task<ProjectColumnDomain> UpdateColumn(UpdateColumnParams param)
    {
        var filter = Builders<ProjectColumn>.Filter.Eq(c => c.Id, param.ColumnId);

        var updateDefs = new List<UpdateDefinition<ProjectColumn>>();

        if (!string.IsNullOrEmpty(param.Name))
            updateDefs.Add(Builders<ProjectColumn>.Update.Set(c => c.Name, param.Name));

        if (!string.IsNullOrEmpty(param.RemoveIssueId))
            updateDefs.Add(Builders<ProjectColumn>.Update.Pull(c => c.IssueIds, new ObjectId(param.RemoveIssueId)));

        if (!string.IsNullOrEmpty(param.AddIssueId))
            updateDefs.Add(Builders<ProjectColumn>.Update.AddToSet(c => c.IssueIds, new ObjectId(param.AddIssueId)));

        updateDefs.Add(Builders<ProjectColumn>.Update.Set(c => c.UpdatedAt, DateTime.UtcNow));

        var update = Builders<ProjectColumn>.Update.Combine(updateDefs);

        var updatedColumn = await _projectColumns.FindOneAndUpdateAsync(filter, update);

        return _mapper.Map<ProjectColumnDomain>(updatedColumn);
    }
    public async Task DeleteColumn(DeleteColumnParams param)
    {
        var filter = Builders<ProjectColumn>.Filter
            .Where(c => c.Id == param.ColumnId);

        await _projectColumns.FindOneAndDeleteAsync(filter);
    }
}
