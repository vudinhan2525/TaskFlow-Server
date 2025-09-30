using AutoMapper;
using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Infras.Entities;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using MongoDB.Bson.Serialization;
using TaskFlow.ProjectTeamService;

namespace MainService.Infras.Repositories;

public class ProjectTeamRepository : IProjectTeamRepository
{
    private readonly IMongoCollection<ProjectTeam> _projectTeams;
    private readonly IMongoCollection<ProjectPermission> _permissions;
    private readonly IMongoCollection<ProjectMember> _projectMembers;

    private readonly IMapper _mapper;
    private readonly ILogger<ProjectTeamRepository> _logger;

    public ProjectTeamRepository(MongoDbService mongoDbService, IMapper mapper, ILogger<ProjectTeamRepository> logger)
    {
        var database = mongoDbService.Database;
        _projectTeams = database.GetCollection<ProjectTeam>("project_teams");
        _permissions = database.GetCollection<ProjectPermission>("permissions");
        _projectMembers = database.GetCollection<ProjectMember>("project_members");
        _mapper = mapper;   
        _logger = logger;
    }

    public async Task<ProjectTeamDomain> CreateProjectTeam(CreateTeamParams param)
    {
        using var session = await _projectTeams.Database.Client.StartSessionAsync();
        session.StartTransaction();

        try
        {
            // 1. Create the team
            var projectTeamEntity = _mapper.Map<ProjectTeam>(param);
            
            if (string.IsNullOrEmpty(projectTeamEntity.Id))
            {
                projectTeamEntity.Id = ObjectId.GenerateNewId().ToString();
            }
            
            await _projectTeams.InsertOneAsync(session, projectTeamEntity);
            
            if (param.MemberIds != null && param.MemberIds.Any())
            {

                var memberFilter = Builders<ProjectMember>.Filter.And(
                    Builders<ProjectMember>.Filter.Eq(x => x.ProjectId, param.ProjectId),
                    Builders<ProjectMember>.Filter.In(x => x.UserId, param.MemberIds)
                );

                var memberUpdate = Builders<ProjectMember>.Update
                    .AddToSet(x => x.TeamIds, projectTeamEntity.Id)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                await _projectMembers.UpdateManyAsync(session, memberFilter, memberUpdate);
            }

            // Commit transaction
            await session.CommitTransactionAsync();

            var projectTeamDomain = _mapper.Map<ProjectTeamDomain>(projectTeamEntity);
            projectTeamDomain.Id = projectTeamEntity.Id ?? string.Empty;
            projectTeamDomain.MemberIds = param.MemberIds?.ToList() ?? new List<string>();
            return projectTeamDomain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project team with members. Rolling back transaction.");
            await session.AbortTransactionAsync();
            throw;
        }
    }

    public async Task<List<ProjectTeamDomain>> ListProjectTeams(ListProjectTeamsParams param)
    {
        var filter = Builders<ProjectTeam>.Filter.Eq(t => t.ProjectId, param.ProjectId);
        var projectTeams = await _projectTeams.Find(filter).ToListAsync();

        var result = new List<ProjectTeamDomain>();
        foreach (var team in projectTeams)
        {
            var teamDomain = new ProjectTeamDomain
            {
                Id = team.Id.ToString(),
                ProjectId = team.ProjectId,
                Name = team.Name,
                Description = team.Description ?? string.Empty,
                CreatedAt = team.CreatedAt,
                UpdatedAt = team.UpdatedAt,
                PermissionKeys = team.PermissionKeys ?? new List<string>()
            };
            // populate member ids
            if (!string.IsNullOrEmpty(team.Id))
            {
                var memberFilter = Builders<ProjectMember>.Filter.And(
                    Builders<ProjectMember>.Filter.Eq(m => m.ProjectId, team.ProjectId),
                    Builders<ProjectMember>.Filter.AnyEq(m => m.TeamIds, team.Id)
                );
                var members = await _projectMembers.Find(memberFilter).Project(m => m.UserId).ToListAsync();
                teamDomain.MemberIds = members ?? new List<string>();
            }
            result.Add(teamDomain);
        }

        return result;
    }

    public async Task<List<PermissionDomain>> GetAllPermissions()
    {
        var allPermissions = await _permissions.Find(_ => true).ToListAsync();
        return _mapper.Map<List<PermissionDomain>>(allPermissions);
    }

    public async Task<ProjectTeamDomain?> GetProjectTeamById(string projectId, string teamId)
    {
        var filter = Builders<ProjectTeam>.Filter.And(
            Builders<ProjectTeam>.Filter.Eq(t => t.ProjectId, projectId),
            Builders<ProjectTeam>.Filter.Eq(t => t.Id, teamId)
        );
        var team = await _projectTeams.Find(filter).FirstOrDefaultAsync();
        if (team == null) return null;
        var domain = new ProjectTeamDomain
        {
            Id = team.Id?.ToString() ?? string.Empty,
            ProjectId = team.ProjectId,
            Name = team.Name,
            Description = team.Description ?? string.Empty,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt,
            PermissionKeys = team.PermissionKeys ?? new List<string>()
        };
        // populate member ids
        if (!string.IsNullOrEmpty(team.Id))
        {
            var memberFilter = Builders<ProjectMember>.Filter.And(
                Builders<ProjectMember>.Filter.Eq(m => m.ProjectId, team.ProjectId),
                Builders<ProjectMember>.Filter.AnyEq(m => m.TeamIds, team.Id)
            );
            var members = await _projectMembers.Find(memberFilter).Project(m => m.UserId).ToListAsync();
            domain.MemberIds = members ?? new List<string>();
        }
        return domain;
    }

    public async Task<ProjectTeamDomain?> UpdateProjectTeam(UpdateTeamParams param)
    {
        var filter = Builders<ProjectTeam>.Filter.And(
            Builders<ProjectTeam>.Filter.Eq(t => t.ProjectId, param.ProjectId),
            Builders<ProjectTeam>.Filter.Eq(t => t.Id, param.TeamId)
        );

        var updateDefs = new List<UpdateDefinition<ProjectTeam>>();
        if (!string.IsNullOrEmpty(param.Name))
        {
            updateDefs.Add(Builders<ProjectTeam>.Update.Set(t => t.Name, param.Name));
        }
        if (param.Description != null)
        {
            updateDefs.Add(Builders<ProjectTeam>.Update.Set(t => t.Description, param.Description));
        }
        if (param.Permissions != null)
        {
            updateDefs.Add(Builders<ProjectTeam>.Update.Set(t => t.PermissionKeys, param.Permissions));
        }
        updateDefs.Add(Builders<ProjectTeam>.Update.Set(t => t.UpdatedAt, DateTime.UtcNow));

        if (updateDefs.Count == 1) // only UpdatedAt
        {
            // still perform update to touch updated_at
        }

        var update = Builders<ProjectTeam>.Update.Combine(updateDefs);
        var options = new FindOneAndUpdateOptions<ProjectTeam> { ReturnDocument = ReturnDocument.After };
        var updated = await _projectTeams.FindOneAndUpdateAsync(filter, update, options);
        if (updated == null) return null;

        var updatedDomain = new ProjectTeamDomain
        {
            Id = updated.Id?.ToString() ?? string.Empty,
            ProjectId = updated.ProjectId,
            Name = updated.Name,
            Description = updated.Description ?? string.Empty,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt,
            PermissionKeys = updated.PermissionKeys ?? new List<string>()
        };
        // populate member ids
        if (!string.IsNullOrEmpty(updated.Id))
        {
            var memberFilter = Builders<ProjectMember>.Filter.And(
                Builders<ProjectMember>.Filter.Eq(m => m.ProjectId, updated.ProjectId),
                Builders<ProjectMember>.Filter.AnyEq(m => m.TeamIds, updated.Id)
            );
            var members = await _projectMembers.Find(memberFilter).Project(m => m.UserId).ToListAsync();
            updatedDomain.MemberIds = members ?? new List<string>();
        }
        return updatedDomain;
    }

}
