using System.Text.Json;
using AutoMapper;
using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Infras.Entities;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MainService.Infras.Repositories;

public class IssueRepository : IIssueRepository
{
    private readonly IMongoCollection<Issue> _issues;
    private readonly ILogger<IssueRepository> _logger;
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;

    public IssueRepository(MongoDbService mongoDbService, IMapper mapper, IProjectRepository projectRepository, ILogger<IssueRepository> logger)
    {
        var database = mongoDbService.Database;
        _issues = database.GetCollection<Issue>("issues");
        _mapper = mapper;
        _logger = logger;
        _projectRepository = projectRepository;
    }

    public async Task<IssueDomain> CreateIssue(IssueDomain issueDomain)
    {
        var issueEntity = _mapper.Map<Issue>(issueDomain);
        await _issues.InsertOneAsync(issueEntity);
        issueDomain.Id = issueEntity.Id;
        return issueDomain;
    }

    public async Task<IssueDomain> GetIssue(string id)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("_id", ObjectId.Parse(id))),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "project_column" },
                { "localField", "column_id" },
                { "foreignField", "_id" },
                { "as", "column" }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$column" },
                { "preserveNullAndEmptyArrays", true }
            })
        };

        var result = await _issues.Aggregate<Issue>(pipeline).FirstOrDefaultAsync();
        if (result == null)
            throw new Exception("Issue not found");

        return _mapper.Map<IssueDomain>(result);
    }

    public async Task<List<IssueDomain>> GetIssuesBySprintId(string sprintId)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("sprint_id", sprintId)),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "project_column" },
                { "localField", "column_id" },
                { "foreignField", "_id" },
                { "as", "column" }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$column" },
                { "preserveNullAndEmptyArrays", true }
            })
        };

        var results = await _issues.Aggregate<Issue>(pipeline).ToListAsync();
        return _mapper.Map<List<IssueDomain>>(results);
    }

    public async Task<UserStats> GetStats(string projectId)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("project_id", projectId)),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "project_column" },
                { "let", new BsonDocument("colId", "$column_id") },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "$expr", new BsonDocument("$eq", new BsonArray
                                {
                                    "$_id",
                                    new BsonDocument("$toObjectId", "$$colId")
                                })
                            }
                        })
                    }
                },
                { "as", "column" }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$column" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$facet", new BsonDocument
            {
                {
                    "byStatus", new BsonArray
                    {
                        new BsonDocument("$group", new BsonDocument
                        {
                            { "_id", new BsonDocument {
                                { "name", "$column.name" },
                                { "order", "$column.order" }
                            }},
                            { "count", new BsonDocument("$sum", 1) }
                        }),
                        new BsonDocument("$sort", new BsonDocument("_id.order", 1)),
                        new BsonDocument("$project", new BsonDocument
                        {
                            { "_id", 0 },
                            { "name", "$_id.name" },
                            { "count", 1 }
                        })
                    }
                },
                {
                    "byType", new BsonArray
                    {
                        new BsonDocument("$group", new BsonDocument
                        {
                            { "_id", "$type" },
                            { "count", new BsonDocument("$sum", 1) }
                        })
                    }
                },
                {
                    "byPriority", new BsonArray
                    {
                        new BsonDocument("$group", new BsonDocument
                        {
                            { "_id", "$priority" },
                            { "count", new BsonDocument("$sum", 1) }
                        })
                    }
                }
            })
        };

        var results = await _issues.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();

        var response = new UserStats
        {
            IssuesByType = results["byType"].AsBsonArray
                .ToDictionary(
                    x => x["_id"].AsString,
                    x => x["count"].AsInt32
                ),
            IssuesByPriority = results["byPriority"].AsBsonArray
                .ToDictionary(
                    x => x["_id"].AsString,
                    x => x["count"].AsInt32
                ),
            TotalIssues = results["byStatus"].AsBsonArray.Sum(x => x["count"].AsInt32),
            CompletedIssues = results["byStatus"].AsBsonArray
                .Where(x => x["name"].AsString.ToLower() == "done")
                .Sum(x => x["count"].AsInt32),
            InProgressIssues = results["byStatus"].AsBsonArray
                .Where(x => x["name"].AsString.ToLower() == "in progress")
                .Sum(x => x["count"].AsInt32),
            TodoIssues = results["byStatus"].AsBsonArray
                .Where(x => x["name"].AsString.ToLower() == "to do")
                .Sum(x => x["count"].AsInt32)
        };

        return response;
    }

    public async Task<IssueDomain> UpdateIssue(UpdateIssueParams body)
    {
        var existingIssue = await _issues.Find(i => i.Id == body.IssueId).FirstOrDefaultAsync();
        if (existingIssue == null)
            throw new Exception("Issue not found");

        if (!string.IsNullOrEmpty(body.Title)) existingIssue.Title = body.Title;
        if (!string.IsNullOrEmpty(body.ProjectId)) existingIssue.ProjectId = body.ProjectId;
        if (!string.IsNullOrEmpty(body.SprintId)) existingIssue.SprintId = body.SprintId;
        if(body.SprintId == "null") existingIssue.SprintId = "";
        if (!string.IsNullOrEmpty(body.AssigneeId)) existingIssue.AssigneeId = body.AssigneeId;
        if (!string.IsNullOrEmpty(body.Description)) existingIssue.Description = body.Description;
        if (!string.IsNullOrEmpty(body.Summary)) existingIssue.Summary = body.Summary;
        if (body.StoryPoint.HasValue) existingIssue.StoryPoint = body.StoryPoint.Value;
        if (!string.IsNullOrEmpty(body.ReporterId)) existingIssue.ReporterId = body.ReporterId;
        if (!string.IsNullOrEmpty(body.ColumnId)) existingIssue.ColumnId = body.ColumnId;
        if (!string.IsNullOrEmpty(body.ParentId)) existingIssue.ParentId = body.ParentId;
        if (body.Type.HasValue) existingIssue.Type = body.Type;
        if (body.Priority.HasValue) existingIssue.Priority = body.Priority;
        if (body.Attachments != null && body.Attachments.Count > 0) existingIssue.Attachments = body.Attachments;

        existingIssue.UpdatedAt = DateTime.UtcNow;
        await _issues.ReplaceOneAsync(i => i.Id == body.IssueId, existingIssue);

        // Fetch the updated issue with column information
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("_id", ObjectId.Parse(body.IssueId))),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "project_column" },
                { "let", new BsonDocument { { "colId", "$column_id" } } },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "$expr", new BsonDocument
                                {
                                    { "$eq", new BsonArray { "$_id", new BsonDocument("$toObjectId", "$$colId") } }
                                }
                            }
                        })
                    }
                },
                { "as", "column" }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$column" },
                { "preserveNullAndEmptyArrays", true }
            })
        };

        var rawResult = await _issues.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        var updatedIssue = BsonSerializer.Deserialize<Issue>(rawResult);
        return _mapper.Map<IssueDomain>(updatedIssue);
    }

    public async Task DeleteIssue(string id)
    {
        var result = await _issues.DeleteOneAsync(i => i.Id == id);
        if (result.DeletedCount == 0)
            throw new Exception("Issue not found");
    }

    public async Task<(List<IssueDomain> Issues, int TotalCount)> ListIssues(GetIssuesParams param)
    {
        var filterBuilder = Builders<Issue>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(param.ProjectId))
        {
            filter &= filterBuilder.Eq(i => i.ProjectId, param.ProjectId);
        }

        if (param.ColumnIds != null && param.ColumnIds.Any())
        {
            filter &= filterBuilder.In(i => i.ColumnId, param.ColumnIds);
        }

        if (param.AssigneeIds != null && param.AssigneeIds.Any())
        {
            filter &= filterBuilder.In(i => i.AssigneeId, param.AssigneeIds);
        }

        if (param.SprintIds != null && param.SprintIds.Any())
        {
            filter &= filterBuilder.In(i => i.SprintId, param.SprintIds);
        }

        if (!string.IsNullOrEmpty(param.Keyword))
        {
            var decodedKeyword = Uri.UnescapeDataString(param.Keyword.Replace("+", " "));

            var keywordFilter = filterBuilder.Or(
                filterBuilder.Regex(i => i.Title, new BsonRegularExpression(decodedKeyword, "i")),
                filterBuilder.Regex(i => i.Description, new BsonRegularExpression(decodedKeyword, "i"))
            );
            filter &= keywordFilter;
        }

        var totalCount = await _issues.CountDocumentsAsync(filter);
        var renderedFilter = filter.Render(new RenderArgs<Issue>(
            BsonSerializer.SerializerRegistry.GetSerializer<Issue>(),
            BsonSerializer.SerializerRegistry
        ));

        var pipeline = new[]
        {
            new BsonDocument("$match", renderedFilter),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "project_column" },
                { "let", new BsonDocument { { "colId", "$column_id" } } },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "$expr", new BsonDocument
                                {
                                    { "$eq", new BsonArray { "$_id", new BsonDocument("$toObjectId", "$$colId") } }
                                }
                            }
                        })
                    }
                },
                { "as", "column" }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$column" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$skip", (param.Page - 1) * param.Limit),
            new BsonDocument("$limit", param.Limit)
        };

        var rawResults = await _issues.Aggregate<BsonDocument>(pipeline).ToListAsync();
        var issues = rawResults.Select(bson => BsonSerializer.Deserialize<Issue>(bson)).ToList();

        return (_mapper.Map<List<IssueDomain>>(issues), (int)totalCount);
    }
}
