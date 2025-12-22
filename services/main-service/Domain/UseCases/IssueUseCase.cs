using Grpc.Core;
using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Domain.Enums;
using TaskFlow.IssueService;

namespace MainService.Domain.UseCases;

public class IssueUseCase
{
    private readonly ITransactionRepo _transactionRepo;
    private readonly IProjectRepository _projectRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivitiesRepository _activitiesRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly ILogger<IssueUseCase> _logger;
    private readonly IPublisherService _publisher;

    public IssueUseCase(
        IIssueRepository issueRepository,
        IUserRepository userRepository,
        ISprintRepository sprintRepository,
        IActivitiesRepository activitiesRepository,
        IProjectRepository projectRepository,
        ITransactionRepo transactionRepo,
        ILogger<IssueUseCase> logger,
        IPublisherService publisher)
    {
        _issueRepository = issueRepository;
        _transactionRepo = transactionRepo;
        _activitiesRepository = activitiesRepository;
        _projectRepository = projectRepository;
        _logger = logger;
        _userRepository = userRepository;
        _sprintRepository = sprintRepository;
        _publisher = publisher;
    }

    public async Task<IssueDomain> CreateIssue(CreateIssueReq param)
    {
        var column = await _projectRepository.FindColumn(new GetColumnParams
        {
            ColumnId = param.ColumnId
        });

        if (column == null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ColumnId is invalid or does not exist."));

        var issue = new IssueDomain()
        {
            ProjectId = param.ProjectId,
            ReporterId = param.ReporterId ?? string.Empty,
            ColumnId = param.ColumnId,
            Title = param.Title,
            Summary = param.Summary ?? string.Empty,
            Description = param.Description ?? string.Empty,
            Type = Enum.TryParse<IssueType>(param.Type, true, out var type) ? type : IssueType.Task,
            Priority = Enum.TryParse<IssuePriority>(param.Priority, true, out var priority) ? priority : IssuePriority.Medium,
            StoryPoint = param.StoryPoint,
            ParentId = param.ParentId ?? string.Empty,
            Attachments = param.Attachments.ToList()
        };

        if (param.SprintId != null)
        {
            issue.AssignToSprint(param.SprintId);
        }
        if (param.AssigneeId != null)
        {
            issue.AssignToUser(param.AssigneeId);
        }

        var result = await _transactionRepo.ExecuteAsync(async session =>
        {
            var newIssue = await _issueRepository.CreateIssue(issue);

            await _projectRepository.UpdateColumn(new UpdateColumnParams
            {
                AddIssueId = newIssue.Id,
                ColumnId = column.Id,
            });

            return newIssue;
        });

        await _publisher.EmitKafka(TopicName.ACTIVITIES, KafkaMessageAction.ACTIVITIES_ISSUE_CREATED, new IActivitiesMessage
        {
            NewIssue = result,
            OldIssue = null,
            UserId = result.ReporterId,
        });

        return result;
    }

    public async Task<IssueDomain> GetIssue(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Issue ID cannot be empty"));

        var issue = await _issueRepository.GetIssue(id) ?? throw new RpcException(new Status(StatusCode.NotFound, $"Issue with ID {id} not found"));

        return issue;
    }

    public async Task<IssueDomain> UpdateIssue(UpdateIssueParams updateData)
    {
        if (string.IsNullOrEmpty(updateData.Id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Issue ID cannot be empty"));

        IssueDomain existingIssue = null!;
        IssueDomain oldIssue = null!;

        var updatedIssue = await _transactionRepo.ExecuteAsync(async session =>
        {
            existingIssue = await _issueRepository.GetIssue(updateData.Id);
            oldIssue = existingIssue;
            if (!string.IsNullOrEmpty(updateData.ColumnId) && updateData.ColumnId != existingIssue.ColumnId)
            {
                var newColumn = await _projectRepository.FindColumn(new GetColumnParams
                {
                    ColumnId = updateData.ColumnId
                });

                if (newColumn == null)
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Column with id '{updateData.ColumnId}' not found"));

                var oldColumn = await _projectRepository.FindColumn(new GetColumnParams
                {
                    ColumnId = existingIssue.ColumnId
                });

                if (oldColumn == null)
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Original column with id '{existingIssue.ColumnId}' not found"));

                // Check if moving to DONE column and set completed_at
                // This logic automatically sets the completed_at timestamp when an issue is moved to a column named "DONE"
                if (newColumn.Name.ToUpper() == "DONE" && oldColumn.Name.ToUpper() != "DONE")
                {
                    updateData.CompletedAt = DateTime.UtcNow;
                }

                await _projectRepository.UpdateColumn(new UpdateColumnParams
                {
                    AddIssueId = updateData.Id,
                    ColumnId = newColumn.Id,
                });
                await _projectRepository.UpdateColumn(new UpdateColumnParams
                {
                    RemoveIssueId = existingIssue.Id,
                    ColumnId = oldColumn.Id,
                });
            }
            return await _issueRepository.UpdateIssue(updateData);
        });

        var notifyTask = Task.CompletedTask;
        // Check asignee change and actor_id different with recipient_id
        if (oldIssue.AssigneeId != updatedIssue.AssigneeId && !string.IsNullOrEmpty(updatedIssue.AssigneeId) && updateData.CreatorId != updatedIssue.AssigneeId)
        {
            notifyTask = _publisher.EmitKafka(TopicName.NOTIFICATIONS, KafkaMessageAction.NOTIFICATIONS_CREATE_NEW_NOTIFICATION, new INotificationMessage
            {
                Type = NotificationType.ASSIGNMENT.ToString(),
                ActorId = updateData.CreatorId,
                IssueId = updatedIssue.Id,
                RecipientId = updatedIssue.AssigneeId
            });
        }

        var activityTask = _publisher.EmitKafka(TopicName.ACTIVITIES, KafkaMessageAction.ACTIVITIES_ISSUE_CHANGED, new IActivitiesMessage
        {
            OldIssue = oldIssue,
            NewIssue = updatedIssue,
            UserId = updateData.CreatorId
        });

        await Task.WhenAll(notifyTask, activityTask);
        return updatedIssue;
    }

    public async Task DeleteIssue(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Issue ID cannot be empty"));

        var issue = await _issueRepository.GetIssue(id);

        if (issue == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Issue with ID {id} not found"));

        var column = await _projectRepository.FindColumn(new GetColumnParams
        {
            ColumnId = issue.ColumnId
        });
        await _projectRepository.UpdateColumn(new UpdateColumnParams
        {
            RemoveIssueId = issue.Id,
            ColumnId = column.Id,
        });
        await _issueRepository.DeleteIssue(id);
    }

    public async Task OnIssueChanged(KafkaMessage<IActivitiesMessage> message)
    {
        var data = message.Data;

        var user = await _userRepository.FindUserAsync(new UserQueryParams
        {
            UserId = data.UserId
        });

        var oldIssue = data.OldIssue;
        var newIssue = data.NewIssue;
        ActivityDomain activity;
        if (message.EventType == KafkaMessageAction.ACTIVITIES_ISSUE_CREATED.ToString() || oldIssue == null)
        {
            activity = new ActivityDomain
            {
                IssueId = newIssue.Id!,
                UserId = data.UserId,
                UserName = user?.FullName ?? "Unknown",
                ProjectId = newIssue.ProjectId,
                ActionType = ActivityAction.ISSUE_CREATED,
                Changes = []
            };
            await _activitiesRepository.CreateActivity(activity);
            return;
        }
        var changes = await getDifferentChange(oldIssue, newIssue);
        if (changes.Count == 0)
            return;

        activity = new ActivityDomain
        {
            IssueId = newIssue.Id!,
            UserId = data.UserId,
            UserName = user?.FullName ?? "Unknown",
            ProjectId = newIssue.ProjectId,
            ActionType = ActivityAction.ISSUE_UPDATED,
            Changes = changes
        };
        await _activitiesRepository.CreateActivity(activity);
    }

    private async Task<List<ActivityChange>> getDifferentChange(IssueDomain oldIssue, IssueDomain newIssue)
    {
        var changes = new List<ActivityChange>();

        void Compare<T>(string field, T? oldValue, T? newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                changes.Add(new ActivityChange
                {
                    Field = field,
                    OldValue = oldValue?.ToString(),
                    NewValue = newValue?.ToString()
                });
            }
        }

        if (oldIssue.ColumnId != newIssue.ColumnId && oldIssue.ColumnId != null && newIssue.ColumnId != null)
        {
            var oldColumnTask = _projectRepository.FindColumn(new GetColumnParams
            {
                ColumnId = oldIssue.ColumnId
            });
            var newColumnTask = _projectRepository.FindColumn(new GetColumnParams
            {
                ColumnId = newIssue.ColumnId
            });

            await Task.WhenAll(oldColumnTask, newColumnTask);

            var oldStatus = oldColumnTask.Result;
            var newStatus = newColumnTask.Result;

            Compare("Status", oldStatus.Name, newStatus.Name);
        }
        if (oldIssue.AssigneeId != newIssue.AssigneeId && oldIssue.AssigneeId != null && newIssue.AssigneeId != null)
        {
            var oldAsigneeTask = _userRepository.FindUserAsync(new UserQueryParams
            {
                UserId = oldIssue.AssigneeId
            });
            var newAsigneeTask = _userRepository.FindUserAsync(new UserQueryParams
            {
                UserId = newIssue.AssigneeId
            });

            await Task.WhenAll(oldAsigneeTask, newAsigneeTask);

            var oldAsignee = oldAsigneeTask.Result;
            var newAsignee = newAsigneeTask.Result;

            Compare("Assignee", oldAsignee.FullName, newAsignee.FullName);
        }
        if (oldIssue.ReporterId != newIssue.ReporterId && oldIssue.ReporterId != null && newIssue.ReporterId != null)
        {
            var oldReporterTask = _userRepository.FindUserAsync(new UserQueryParams
            {
                UserId = oldIssue.ReporterId
            });
            var newReporterTask = _userRepository.FindUserAsync(new UserQueryParams
            {
                UserId = newIssue.ReporterId
            });

            await Task.WhenAll(oldReporterTask, newReporterTask);

            var oldReporter = oldReporterTask.Result;
            var newReporter = newReporterTask.Result;

            Compare("Reporter", oldReporter.FullName, newReporter.FullName);
        }
        if (oldIssue.SprintId != newIssue.SprintId && oldIssue.SprintId != null && newIssue.SprintId != null)
        {
            var oldSprintTask = _sprintRepository.GetSprint(oldIssue.SprintId);
            var newSprintTask = _sprintRepository.GetSprint(newIssue.SprintId);

            await Task.WhenAll(oldSprintTask, newSprintTask);

            var oldSprint = oldSprintTask.Result;
            var newSprint = newSprintTask.Result;

            Compare("Sprint", oldSprint.Name, newSprint.Name);
        }

        Compare("Title", oldIssue.Title, newIssue.Title);
        Compare("Description", oldIssue.Description, newIssue.Description);
        Compare("Priority", oldIssue.Priority, newIssue.Priority);
        Compare("Type", oldIssue.Type, newIssue.Type);
        Compare("Summary", oldIssue.Summary, newIssue.Summary);
        Compare<int?>("StoryPoint", oldIssue.StoryPoint, newIssue.StoryPoint);

        return changes;
    }

    public async Task<(List<IssueDomain> Issues, int TotalCount)> ListIssues(GetIssuesParams param)
    {
        if (param.Page <= 0) param.Page = 1;
        if (param.Limit <= 0) param.Limit = 10;

        return await _issueRepository.ListIssues(param);
    }

    public async Task<(List<ActivityDomain>, int totalCount)> ListActivities(GetActivityParams param)
    {
        return await _activitiesRepository.ListActivities(param);
    }
}