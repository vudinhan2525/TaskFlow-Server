using MainService.Domain.Entities;
using TaskFlow.SprintService;
namespace MainService.Domain.Interfaces;

public interface ISprintRepository
{
    Task<SprintDomain> CreateSprint(SprintDomain sprint);
    Task<SprintDomain> GetSprint(string id);
    Task<SprintDomain> UpdateSprint(SprintDomain sprint);
    Task DeleteSprint(string id);
    Task<SprintStats> GetSprintStats(string sprint_id, string project_id);
    Task<List<SprintDailyStats>> GetSprintDailyStats(string sprint_id);
    Task<(List<SprintDomain> Sprints, int TotalCount)> ListSprints(ListSprintParams param);
    // Task<List<SprintDomain>> GetAllSprintsByProjectId(string projectId);
}

public class ListSprintParams
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? ProjectId { get; set; }
    public List<string>? SprintIds { get; set; }
}