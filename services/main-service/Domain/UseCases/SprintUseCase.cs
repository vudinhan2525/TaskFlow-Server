using MainService.Domain.Entities;
using MainService.Domain.Interfaces;

namespace MainService.Domain.UseCases;

public class SprintUseCase
{
    private readonly ISprintRepository _sprintRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IPublisherService _publisherService;

    public SprintUseCase(
        ISprintRepository sprintRepository,
        IIssueRepository issueRepository,
        IPublisherService publisherService)
    {
        _sprintRepository = sprintRepository;
        _issueRepository = issueRepository;
        _publisherService = publisherService;
    }

    public async Task<SprintDomain> CreateSprint(SprintDomain sprint)
    {
        if (string.IsNullOrEmpty(sprint.Name))
            throw new ArgumentException("Sprint name cannot be empty");

        if (string.IsNullOrEmpty(sprint.ProjectId))
            throw new ArgumentException("Project ID cannot be empty");

        if (sprint.DateStarted >= sprint.DateEnded)
            throw new ArgumentException("Start date must be before end date");

        // Initialize statistics
        sprint.Statistics = new SprintStatisticsDomain
        {
            TotalIssues = 0,
            CompletedIssues = 0,
            TotalStoryPoints = 0,
            CompletedStoryPoints = 0,
            IssuesByType = new Dictionary<string, int>(),
            IssuesByStatus = new Dictionary<string, int>(),
            DailyProgress = new List<SprintProgressDomain>
            {
                new SprintProgressDomain
                {
                    Day = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Planned = 0,
                    Completed = 0,
                    Remaining = 0
                }
            }
        };

        return await _sprintRepository.CreateSprint(sprint);
    }

    public async Task<SprintDomain> UpdateSprintStatistics(string sprintId)
    {
        var sprint = await GetSprint(sprintId);
        var issues = await _issueRepository.GetIssuesBySprintId(sprintId);

        // Update statistics
        sprint.Statistics.TotalIssues = issues.Count;
        sprint.Statistics.CompletedIssues = issues.Count(i => i.ColumnId == "done"); // Assuming "done" is the column ID
        sprint.Statistics.TotalStoryPoints = issues.Sum(i => i.StoryPoint);
        sprint.Statistics.CompletedStoryPoints = issues.Where(i => i.ColumnId == "done").Sum(i => i.StoryPoint);

        // Update issue type distribution
        sprint.Statistics.IssuesByType = issues
            .GroupBy(i => i.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Update issue status distribution
        sprint.Statistics.IssuesByStatus = issues
            .GroupBy(i => i.ColumnId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate daily progress
        var totalDays = (sprint.DateEnded - sprint.DateStarted).Days;
        var progressData = new List<SprintProgressDomain>();

        for (int day = 0; day <= totalDays; day += 2) // Add data every 2 days
        {
            var currentDate = sprint.DateStarted.AddDays(day);
            if (currentDate > DateTime.UtcNow) break; // Don't add future dates

            // Get completed issues up to this date
            var completedIssues = issues.Count(i =>
                i.ColumnId == "done" &&
                i.UpdatedAt <= currentDate);

            var progress = new SprintProgressDomain
            {
                Day = currentDate.ToString("yyyy-MM-dd"),
                Planned = issues.Count,
                Completed = completedIssues,
                Remaining = issues.Count - completedIssues
            };
            progressData.Add(progress);
        }

        // Ensure at least one progress entry
        if (!progressData.Any())
        {
            progressData.Add(new SprintProgressDomain
            {
                Day = sprint.DateStarted.ToString("yyyy-MM-dd"),
                Planned = issues.Count,
                Completed = 0,
                Remaining = issues.Count
            });
        }

        sprint.Statistics.DailyProgress = progressData;

        // Publish activity
        await _publisherService.Emit(new ActivityDomain
        {
            IssueId = string.Empty, // This is a sprint-level activity
            SprintId = sprintId,
            ActionType = ActivityAction.SPRINT_PROGRESS_UPDATED,
            Changes = new List<ActivityChange>
            {
                new ActivityChange
                {
                    Field = ActivityField.COMPLETED_COUNT,
                    NewValue = sprint.Statistics.CompletedIssues.ToString()
                },
                new ActivityChange
                {
                    Field = ActivityField.REMAINING_COUNT,
                    NewValue = (sprint.Statistics.TotalIssues - sprint.Statistics.CompletedIssues).ToString()
                }
            }
        });

        return await _sprintRepository.UpdateSprint(sprint);
    }

    public async Task<SprintDomain> GetSprint(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Sprint ID cannot be empty");

        var sprint = await _sprintRepository.GetSprint(id);
        if (sprint == null)
            throw new KeyNotFoundException($"Sprint with ID {id} not found");

        return sprint;
    }

    public async Task<SprintDomain> UpdateSprint(SprintDomain sprint)
    {
        if (string.IsNullOrEmpty(sprint.Id))
            throw new ArgumentException("Sprint ID cannot be empty");

        if (string.IsNullOrEmpty(sprint.Name))
            throw new ArgumentException("Sprint name cannot be empty");

        var existingSprint = await _sprintRepository.GetSprint(sprint.Id);
        if (existingSprint == null)
            throw new KeyNotFoundException($"Sprint with ID {sprint.Id} not found");

        // Update only provided fields
        existingSprint.Name = sprint.Name;
        existingSprint.DateStarted = sprint.DateStarted;
        existingSprint.DateEnded = sprint.DateEnded;
        existingSprint.Duration = sprint.Duration;
        existingSprint.Goal = sprint.Goal;
        existingSprint.ProjectId = sprint.ProjectId;
        existingSprint.UpdatedAt = DateTime.UtcNow;

        return await _sprintRepository.UpdateSprint(existingSprint);
    }

    public async Task DeleteSprint(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Sprint ID cannot be empty");

        var sprint = await _sprintRepository.GetSprint(id);
        if (sprint == null)
            throw new KeyNotFoundException($"Sprint with ID {id} not found");

        await _sprintRepository.DeleteSprint(id);
    }

    public async Task<(List<SprintDomain> Sprints, int TotalCount)> ListSprints(string projectId, int page, int pageSize)
    {
        if (string.IsNullOrEmpty(projectId))
            throw new ArgumentException("Project ID cannot be empty");

        if (page < 1)
            throw new ArgumentException("Page number must be greater than 0");
        
        if (pageSize < 1)
            throw new ArgumentException("Page size must be greater than 0");

        return await _sprintRepository.ListSprints(projectId, page, pageSize);
    }
}