using AutoMapper;
using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Domain.Enums;
using MainService.Infras.Entities;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.RegularExpressions;

namespace MainService.Infras.Repositories;

public class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly IMongoCollection<ProjectMember> _collection;
    private readonly IMongoCollection<User> _users;
    private readonly IMapper _mapper;

    public ProjectMemberRepository(MongoDbService mongoDbService, IMapper mapper)
    {
        var database = mongoDbService.Database;
        _collection = database.GetCollection<ProjectMember>("project_members");
        _users = database.GetCollection<User>("users");
        _mapper = mapper;
    }

    public async Task<ProjectMemberDomain?> GetAsync(string id)
    {
        var entity = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        return entity == null ? null : _mapper.Map<ProjectMemberDomain>(entity);
    }

    public async Task<ProjectMemberDomain?> GetByProjectAndUserAsync(string projectId, string userId)
    {
        var entity = await _collection.Find(x => x.ProjectId == projectId && x.UserId == userId)
            .FirstOrDefaultAsync();
        return entity == null ? null : _mapper.Map<ProjectMemberDomain>(entity);
    }

    public async Task<IEnumerable<ProjectMemberDomain>> GetProjectMembersAsync(string projectId, int page, int limit)
    {
        var entities = await _collection.Find(x => x.ProjectId == projectId)
            .Skip((page - 1) * limit)
            .Limit(limit)
            .ToListAsync();
        return _mapper.Map<IEnumerable<ProjectMemberDomain>>(entities);
    }
    public async Task<int> GetProjectMembersCountAsync(string projectId)
    {
        return (int)await _collection.CountDocumentsAsync(x => x.ProjectId == projectId);
    }

    public async Task<(IEnumerable<ProjectMemberDomain> Members, int TotalCount)> SearchProjectMembersAsync(SearchProjectMemberQueryParams param)
    {
        var name = param.Name;
        var email = param.Email;
        var page = param.Page;
        var limit = param.Limit;

        // First, find user IDs that match the name and email filters
        var userFilterBuilder = Builders<User>.Filter;
        var userFilters = new List<FilterDefinition<User>>();

        if (!string.IsNullOrEmpty(name))
        {
            var decodedName = Uri.UnescapeDataString(name.Replace("+", " "));
            var normalizedName = decodedName.NormalizeVietnamese();
            var searchTerms = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var nameFilters = new List<FilterDefinition<User>>();
            foreach (var term in searchTerms)
            {
                var pattern = RegexHelper.BuildAccentInsensitivePattern(term);
                nameFilters.Add(userFilterBuilder.Or(
                    userFilterBuilder.Regex(x => x.FirstName, new BsonRegularExpression(pattern, "i")),
                    userFilterBuilder.Regex(x => x.LastName, new BsonRegularExpression(pattern, "i")),
                    userFilterBuilder.Regex(x => x.FullName, new BsonRegularExpression(pattern, "i"))
                ));
            }
            userFilters.Add(userFilterBuilder.And(nameFilters));
        }

        if (!string.IsNullOrEmpty(email))
        {
            var decodedEmail = Uri.UnescapeDataString(email.Replace("+", " ")).Trim();
            userFilters.Add(userFilterBuilder.Regex(x => x.Email, new BsonRegularExpression(decodedEmail, "i")));
        }

        var userFilter = userFilters.Any() ? userFilterBuilder.And(userFilters) : userFilterBuilder.Empty;
        var matchingUserIds = new List<string>();

        if (userFilter != userFilterBuilder.Empty)
        {
            var matchingUsers = await _users.Find(userFilter).Project(x => x.Id).ToListAsync();
            matchingUserIds = matchingUsers.Where(x => !string.IsNullOrEmpty(x)).ToList();
            
            // If no users match the filters, return empty result
            if (!matchingUserIds.Any())
            {
                return (new List<ProjectMemberDomain>(), 0);
            }
        }

        // Now filter project members by project ID and matching user IDs
        var memberFilterBuilder = Builders<ProjectMember>.Filter;
        var memberFilters = new List<FilterDefinition<ProjectMember>>();

        memberFilters.Add(memberFilterBuilder.Eq(x => x.ProjectId, param.ProjectId));

        if (matchingUserIds.Any())
        {
            memberFilters.Add(memberFilterBuilder.In(x => x.UserId, matchingUserIds));
        }

        var memberFilter = memberFilterBuilder.And(memberFilters);

        var options = new FindOptions<ProjectMember, ProjectMember>
        {
            Collation = new Collation("en", strength: CollationStrength.Secondary)
        };

        var totalCount = await _collection.CountDocumentsAsync(memberFilter, new CountOptions { Collation = options.Collation });
        var members = await _collection.Find(memberFilter)
            .Skip((page - 1) * limit)
            .Limit(limit)
            .ToListAsync();

        return (members.Select(m => _mapper.Map<ProjectMemberDomain>(m)), (int)totalCount);
    }

    public async Task<ProjectMemberDomain> AddAsync(ProjectMemberDomain member)
    {
        var entity = _mapper.Map<ProjectMember>(member);
        await _collection.InsertOneAsync(entity);
        member.Id = entity.Id;
        return member;
    }

    public async Task<ProjectMemberDomain> UpdateAsync(ProjectMemberDomain member)
    {
        var entity = _mapper.Map<ProjectMember>(member);
        var result = await _collection.ReplaceOneAsync(x => x.Id == member.Id, entity);
        if (result.MatchedCount == 0)
            throw new Exception("Project member not found");
        return member;
    }

    public async Task DeleteAsync(string id)
    {
        var result = await _collection.DeleteOneAsync(x => x.Id == id);
        if (result.DeletedCount == 0)
            throw new Exception("Project member not found");
    }

    public async Task<bool> IsUserProjectMemberAsync(string projectId, string userId)
    {
        var member = await GetByProjectAndUserAsync(projectId, userId);
        return member != null;
    }

    public async Task<bool> HasProjectRole(string projectId, string userId, TeamMemberRole role)
    {
        var member = await GetByProjectAndUserAsync(projectId, userId);
        return member?.Role == role;
    }

    public async Task<IEnumerable<ProjectMemberDomain>> GetUserProjectsAsync(string userId, int page, int limit)
    {
        var entities = await _collection.Find(x => x.UserId == userId)
            .Skip((page - 1) * limit)
            .Limit(limit)
            .ToListAsync();
        return _mapper.Map<IEnumerable<ProjectMemberDomain>>(entities);
    }

    public async Task<int> GetUserProjectsCountAsync(string userId)
    {
        return (int)await _collection.CountDocumentsAsync(x => x.UserId == userId);
    }

    public async Task<ProjectMemberDomain> ApproveMemberAsync(string projectId, string userId)
    {
        var filter = Builders<ProjectMember>.Filter.And(
            Builders<ProjectMember>.Filter.Eq(x => x.ProjectId, projectId),
            Builders<ProjectMember>.Filter.Eq(x => x.UserId, userId)
        );

        var update = Builders<ProjectMember>.Update
            .Set(x => x.IsPending, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<ProjectMember>
        {
            ReturnDocument = ReturnDocument.After
        };

        var entity = await _collection.FindOneAndUpdateAsync(filter, update, options);
        if (entity == null)
            throw new Exception("Project member not found");

        return _mapper.Map<ProjectMemberDomain>(entity);
    }

    public async Task<bool> RejectMemberAsync(string projectId, string userId)
    {
        var filter = Builders<ProjectMember>.Filter.And(
            Builders<ProjectMember>.Filter.Eq(x => x.ProjectId, projectId),
            Builders<ProjectMember>.Filter.Eq(x => x.UserId, userId),
            Builders<ProjectMember>.Filter.Eq(x => x.IsPending, true)
        );

        var result = await _collection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    public async Task<bool> AddMembersToTeamAsync(string projectId, string teamId, List<string> userIds)
    {
        var filter = Builders<ProjectMember>.Filter.And(
            Builders<ProjectMember>.Filter.Eq(x => x.ProjectId, projectId),
            Builders<ProjectMember>.Filter.In(x => x.UserId, userIds)
        );

        var update = Builders<ProjectMember>.Update
            .AddToSet(x => x.TeamIds, teamId)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateManyAsync(filter, update);
        return result.ModifiedCount > 0;
    }

}   