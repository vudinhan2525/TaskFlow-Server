using Xunit;
using Moq;
using Grpc.Core;
using MainService.Domain.UseCases;
using MainService.Domain.Interfaces;
using MainService.Domain.Entities;
using MainService.Domain.Enums;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFlow.IssueService;
using MongoDB.Driver; // ← BẮT BUỘC ĐỂ DÙNG IClientSessionHandle

namespace MainService.Tests.UseCases
{
    public class IssueUseCaseTests
    {
        private readonly Mock<ITransactionRepo> _transactionRepoMock;
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<ISprintRepository> _sprintRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IActivitiesRepository> _activitiesRepositoryMock;
        private readonly Mock<IIssueRepository> _issueRepositoryMock;
        private readonly Mock<ILogger<IssueUseCase>> _loggerMock;
        private readonly Mock<IPublisherService> _publisherMock;
        private readonly IssueUseCase _issueUseCase;

        public IssueUseCaseTests()
        {
            _transactionRepoMock = new Mock<ITransactionRepo>();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _sprintRepositoryMock = new Mock<ISprintRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _activitiesRepositoryMock = new Mock<IActivitiesRepository>();
            _issueRepositoryMock = new Mock<IIssueRepository>();
            _loggerMock = new Mock<ILogger<IssueUseCase>>();
            _publisherMock = new Mock<IPublisherService>();

            _issueUseCase = new IssueUseCase(
                _issueRepositoryMock.Object,
                _userRepositoryMock.Object,
                _sprintRepositoryMock.Object,
                _activitiesRepositoryMock.Object,
                _projectRepositoryMock.Object,
                _transactionRepoMock.Object,
                _loggerMock.Object,
                _publisherMock.Object
            );
        }

        #region CreateIssue Tests (ISS-01 to ISS-06)

        [Fact]
        public async Task ISS01_CreateIssueWithValidData_ShouldCreateSuccessfully()
        {
            var projectId = "proj123";
            var createReq = new CreateIssueReq
            {
                ProjectId = projectId,
                ReporterId = "user123",
                ColumnId = "col123",
                Title = "Test Issue",
                Summary = "Test summary",
                Description = "Test description",
                Type = "Task",
                Priority = "Medium",
                StoryPoint = 5,
                Attachments = { }
            };

            var column = new ProjectColumnDomain { Id = createReq.ColumnId, Name = "To Do", ProjectId = projectId };
            var createdIssue = new IssueDomain
            {
                Id = "issue123",
                ProjectId = projectId,
                ReporterId = createReq.ReporterId,
                ColumnId = createReq.ColumnId,
                Title = createReq.Title,
                Type = IssueType.Task,
                Priority = IssuePriority.Medium
            };

            _projectRepositoryMock.Setup(r => r.FindColumn(It.IsAny<GetColumnParams>())).ReturnsAsync(column);

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(createdIssue);

            _issueRepositoryMock.Setup(r => r.CreateIssue(It.IsAny<IssueDomain>())).ReturnsAsync(createdIssue);
            _projectRepositoryMock.Setup(r => r.UpdateColumn(It.IsAny<UpdateColumnParams>())).ReturnsAsync(column);

            var result = await _issueUseCase.CreateIssue(createReq);

            result.Should().NotBeNull();
            result.Id.Should().Be("issue123");
        }

        [Fact]
        public async Task ISS02_CreateIssue_WithInvalidColumnId_ShouldThrowException()
        {
            var createReq = new CreateIssueReq
            {
                ProjectId = "proj123",
                ReporterId = "user123",
                ColumnId = "invalid123",
                Title = "Test Issue",
                Type = "Task",
                Priority = "Medium",
                Attachments = { }
            };

            _projectRepositoryMock.Setup(r => r.FindColumn(It.IsAny<GetColumnParams>())).ReturnsAsync((ProjectColumnDomain?)null);

            var exception = await Assert.ThrowsAsync<RpcException>(() => _issueUseCase.CreateIssue(createReq));
            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("ColumnId is invalid or does not exist");
        }

        [Fact]
        public async Task ISS03_CreateIssue_WithSprintId_ShouldAssignToSprint()
        {
            var createReq = new CreateIssueReq
            {
                ProjectId = "proj123",
                ReporterId = "user123",
                ColumnId = "col123",
                Title = "Test Issue",
                SprintId = "sprint123",
                Type = "Task",
                Priority = "Medium",
                Attachments = { }
            };

            var column = new ProjectColumnDomain { Id = createReq.ColumnId, Name = "To Do", ProjectId = createReq.ProjectId };
            var createdIssue = new IssueDomain { Id = "issue123", ProjectId = createReq.ProjectId, SprintId = createReq.SprintId };

            _projectRepositoryMock.Setup(r => r.FindColumn(It.IsAny<GetColumnParams>())).ReturnsAsync(column);

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(createdIssue);

            _issueRepositoryMock.Setup(r => r.CreateIssue(It.IsAny<IssueDomain>())).ReturnsAsync(createdIssue);
            _projectRepositoryMock.Setup(r => r.UpdateColumn(It.IsAny<UpdateColumnParams>())).ReturnsAsync(column);
            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<IActivitiesMessage>())).Returns(Task.CompletedTask);

            var result = await _issueUseCase.CreateIssue(createReq);
            result.SprintId.Should().Be(createReq.SprintId);
        }

        [Fact]
        public async Task ISS04_CreateIssue_WithAssigneeId_ShouldAssignToUser()
        {
            var createReq = new CreateIssueReq
            {
                ProjectId = "proj123",
                ReporterId = "user123",
                ColumnId = "col123",
                Title = "Test Issue",
                AssigneeId = "user456",
                Type = "Task",
                Priority = "Medium",
                Attachments = { }
            };

            var column = new ProjectColumnDomain { Id = createReq.ColumnId, Name = "To Do", ProjectId = createReq.ProjectId };
            var createdIssue = new IssueDomain { Id = "issue123", ProjectId = createReq.ProjectId, AssigneeId = createReq.AssigneeId };

            _projectRepositoryMock.Setup(r => r.FindColumn(It.IsAny<GetColumnParams>())).ReturnsAsync(column);

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(createdIssue);

            _issueRepositoryMock.Setup(r => r.CreateIssue(It.IsAny<IssueDomain>())).ReturnsAsync(createdIssue);
            _projectRepositoryMock.Setup(r => r.UpdateColumn(It.IsAny<UpdateColumnParams>())).ReturnsAsync(column);
            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<IActivitiesMessage>())).Returns(Task.CompletedTask);

            var result = await _issueUseCase.CreateIssue(createReq);
            result.AssigneeId.Should().Be(createReq.AssigneeId);
        }

        [Fact]
        public async Task ISS05_CreateIssue_ShouldAddToColumn()
        {
            var projectId = "proj123";
            var createReq = new CreateIssueReq
            {
                ProjectId = projectId,
                ReporterId = "user123",
                ColumnId = "col123",
                Title = "Test Issue",
                Type = "Task",
                Priority = "Medium",
                Attachments = { }
            };

            var column = new ProjectColumnDomain { Id = createReq.ColumnId, Name = "To Do", ProjectId = projectId };
            var createdIssue = new IssueDomain { Id = "issue123", ProjectId = projectId, ColumnId = createReq.ColumnId };

            _projectRepositoryMock.Setup(r => r.FindColumn(It.IsAny<GetColumnParams>())).ReturnsAsync(column);

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(createdIssue);

            _issueRepositoryMock.Setup(r => r.CreateIssue(It.IsAny<IssueDomain>())).ReturnsAsync(createdIssue);
            _projectRepositoryMock.Setup(r => r.UpdateColumn(It.IsAny<UpdateColumnParams>())).ReturnsAsync(column);
            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<IActivitiesMessage>())).Returns(Task.CompletedTask);

            await _issueUseCase.CreateIssue(createReq);

            _projectRepositoryMock.Verify(r => r.UpdateColumn(
                It.Is<UpdateColumnParams>(p => p.AddIssueId == createdIssue.Id && p.ColumnId == column.Id)), Times.Once);
        }

        [Fact]
        public async Task ISS06_CreateIssue_ShouldPublishActivityEvent()
        {
            var projectId = "proj123";
            var createReq = new CreateIssueReq
            {
                ProjectId = projectId,
                ReporterId = "user123",
                ColumnId = "col123",
                Title = "Test Issue",
                Type = "Task",
                Priority = "Medium",
                Attachments = { }
            };

            var column = new ProjectColumnDomain { Id = createReq.ColumnId, Name = "To Do", ProjectId = projectId };
            var createdIssue = new IssueDomain { Id = "issue123", ProjectId = projectId, ReporterId = "user123" };

            _projectRepositoryMock.Setup(r => r.FindColumn(It.IsAny<GetColumnParams>())).ReturnsAsync(column);

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(createdIssue);

            _issueRepositoryMock.Setup(r => r.CreateIssue(It.IsAny<IssueDomain>())).ReturnsAsync(createdIssue);
            _projectRepositoryMock.Setup(r => r.UpdateColumn(It.IsAny<UpdateColumnParams>())).ReturnsAsync(column);
            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<IActivitiesMessage>())).Returns(Task.CompletedTask);

            await _issueUseCase.CreateIssue(createReq);

            _publisherMock.Verify(p => p.EmitKafka(
                TopicName.ACTIVITIES,
                KafkaMessageAction.ACTIVITIES_ISSUE_CREATED,
                It.Is<IActivitiesMessage>(m => m.NewIssue.Id == createdIssue.Id && m.UserId == createReq.ReporterId)), Times.Once);
        }

        #endregion

        #region GetIssue Tests (ISS-07 to ISS-09)

        [Fact]
        public async Task ISS07_GetIssueById_WithValidId_ShouldReturnIssue()
        {
            var issueId = "issue123";
            var issue = new IssueDomain { Id = issueId, Title = "Test Issue", ProjectId = "proj123" };

            _issueRepositoryMock.Setup(repo => repo.GetIssue(issueId)).ReturnsAsync(issue);

            var result = await _issueUseCase.GetIssue(issueId);

            result.Should().NotBeNull();
            result.Id.Should().Be(issueId);
            result.Title.Should().Be(issue.Title);
        }

        [Fact]
        public async Task ISS08_GetIssueById_WithInvalidId_ShouldThrowException()
        {
            var issueId = "invalid123";
            _issueRepositoryMock.Setup(repo => repo.GetIssue(issueId)).ReturnsAsync((IssueDomain?)null);

            var exception = await Assert.ThrowsAsync<RpcException>(() => _issueUseCase.GetIssue(issueId));

            exception.StatusCode.Should().Be(StatusCode.NotFound);
            exception.Status.Detail.Should().Contain($"Issue with ID {issueId} not found");
        }

        [Fact]
        public async Task ISS09_GetIssueById_WithEmptyId_ShouldThrowException()
        {
            var exception = await Assert.ThrowsAsync<RpcException>(() => _issueUseCase.GetIssue(""));

            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Issue ID cannot be empty");
        }

        #endregion

        #region ListIssues Tests (ISS-19 to ISS-20)

        [Fact]
        public async Task ISS19_ListIssues_ShouldReturnIssuesAndTotalCount()
        {
            var param = new GetIssuesParams { ProjectId = "proj123", Page = 1, Limit = 10 };
            var issues = new List<IssueDomain>
            {
                new IssueDomain { Id = "issue1", Title = "Issue 1" },
                new IssueDomain { Id = "issue2", Title = "Issue 2" }
            };

            _issueRepositoryMock.Setup(repo => repo.ListIssues(It.IsAny<GetIssuesParams>())).ReturnsAsync((issues, issues.Count));

            var result = await _issueUseCase.ListIssues(param);

            result.Issues.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task ISS20_ListIssues_WithInvalidPagination_ShouldUseDefaults()
        {
            var param = new GetIssuesParams { ProjectId = "proj123", Page = 0, Limit = -5 };
            _issueRepositoryMock.Setup(repo => repo.ListIssues(It.IsAny<GetIssuesParams>())).ReturnsAsync((new List<IssueDomain>(), 0));

            await _issueUseCase.ListIssues(param);

            _issueRepositoryMock.Verify(repo => repo.ListIssues(It.Is<GetIssuesParams>(p => p.Page == 1 && p.Limit == 10)), Times.Once);
        }

        #endregion

        #region ListActivities Tests (ISS-21 to ISS-22)

        [Fact]
        public async Task ISS21_ListActivities_ShouldReturnActivitiesAndTotalCount()
        {
            var param = new GetActivityParams { IssueId = "issue123", Page = 1, Limit = 10 };
            var activities = new List<ActivityDomain>
            {
                new ActivityDomain { Id = "act1", IssueId = "issue123", ProjectId = "proj123", ActionType = ActivityAction.ISSUE_CREATED },
                new ActivityDomain { Id = "act2", IssueId = "issue123", ProjectId = "proj123", ActionType = ActivityAction.ISSUE_UPDATED }
            };

            _activitiesRepositoryMock.Setup(repo => repo.ListActivities(It.IsAny<GetActivityParams>())).ReturnsAsync((activities, activities.Count));

            var (resultActivities, totalCount) = await _issueUseCase.ListActivities(param);

            resultActivities.Should().HaveCount(2);
            totalCount.Should().Be(2);
        }

        [Fact]
        public async Task ISS22_ListActivities_EmptyResult_ShouldReturnEmptyList()
        {
            var param = new GetActivityParams { IssueId = "issue123", Page = 1, Limit = 10 };
            _activitiesRepositoryMock.Setup(repo => repo.ListActivities(It.IsAny<GetActivityParams>())).ReturnsAsync((new List<ActivityDomain>(), 0));

            var (resultActivities, totalCount) = await _issueUseCase.ListActivities(param);

            resultActivities.Should().BeEmpty();
            totalCount.Should().Be(0);
        }

        #endregion

        #region UpdateIssue Tests (ISS-10 to ISS-15)

        [Fact]
        public async Task ISS10_UpdateIssue_WithValidData_ShouldSucceed()
        {
            var updateParams = new UpdateIssueParams { Id = "issue123", Title = "Updated Title", CreatorId = "user123" };
            var existingIssue = new IssueDomain { Id = "issue123", Title = "Old Title", ColumnId = "col123", ProjectId = "proj123" };
            var updatedIssue = new IssueDomain { Id = "issue123", Title = "Updated Title", ColumnId = "col123", ProjectId = "proj123" };

            _issueRepositoryMock.Setup(r => r.GetIssue(updateParams.Id)).ReturnsAsync(existingIssue);
            _issueRepositoryMock.Setup(r => r.UpdateIssue(It.IsAny<UpdateIssueParams>())).ReturnsAsync(updatedIssue);

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(updatedIssue);

            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<object>())).Returns(Task.CompletedTask);

            var result = await _issueUseCase.UpdateIssue(updateParams);
            result.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task ISS11_UpdateIssue_MovingToNewColumn_ShouldUpdateBothColumns()
        {
            var updateParams = new UpdateIssueParams { Id = "issue123", ColumnId = "newCol456", CreatorId = "user123" };
            var existingIssue = new IssueDomain { Id = "issue123", ColumnId = "oldCol123", ProjectId = "proj123" };
            var oldColumn = new ProjectColumnDomain { Id = "oldCol123", Name = "To Do", ProjectId = "proj123" };
            var newColumn = new ProjectColumnDomain { Id = "newCol456", Name = "In Progress", ProjectId = "proj123" };
            var updatedIssue = new IssueDomain { Id = "issue123", ColumnId = "newCol456", ProjectId = "proj123" };

            _issueRepositoryMock.Setup(r => r.GetIssue(updateParams.Id)).ReturnsAsync(existingIssue);
            _projectRepositoryMock.Setup(r => r.FindColumn(It.Is<GetColumnParams>(p => p.ColumnId == "newCol456"))).ReturnsAsync(newColumn);
            _projectRepositoryMock.Setup(r => r.FindColumn(It.Is<GetColumnParams>(p => p.ColumnId == "oldCol123"))).ReturnsAsync(oldColumn);
            _issueRepositoryMock.Setup(r => r.UpdateIssue(It.IsAny<UpdateIssueParams>())).ReturnsAsync(updatedIssue);
            _projectRepositoryMock.Setup(r => r.UpdateColumn(It.IsAny<UpdateColumnParams>())).ReturnsAsync(new ProjectColumnDomain());

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(updatedIssue);

            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<object>())).Returns(Task.CompletedTask);

            await _issueUseCase.UpdateIssue(updateParams);

            _projectRepositoryMock.Verify(r => r.UpdateColumn(It.Is<UpdateColumnParams>(p => p.AddIssueId == updateParams.Id)), Times.Once);
            _projectRepositoryMock.Verify(r => r.UpdateColumn(It.Is<UpdateColumnParams>(p => p.RemoveIssueId == updateParams.Id)), Times.Once);
        }

        [Fact]
        public async Task ISS12_UpdateIssue_MovingToDoneColumn_ShouldSetCompletedAt()
        {
            var updateParams = new UpdateIssueParams { Id = "issue123", ColumnId = "doneCol", CreatorId = "user123" };
            var existingIssue = new IssueDomain { Id = "issue123", ColumnId = "inProgressCol", ProjectId = "proj123" };
            var oldColumn = new ProjectColumnDomain { Id = "inProgressCol", Name = "In Progress", ProjectId = "proj123" };
            var doneColumn = new ProjectColumnDomain { Id = "doneCol", Name = "DONE", ProjectId = "proj123" };
            var updatedIssue = new IssueDomain { Id = "issue123", ColumnId = "doneCol", ProjectId = "proj123", CompletedAt = DateTime.UtcNow };

            _issueRepositoryMock.Setup(r => r.GetIssue(updateParams.Id)).ReturnsAsync(existingIssue);
            _projectRepositoryMock.Setup(r => r.FindColumn(It.Is<GetColumnParams>(p => p.ColumnId == "doneCol"))).ReturnsAsync(doneColumn);
            _projectRepositoryMock.Setup(r => r.FindColumn(It.Is<GetColumnParams>(p => p.ColumnId == "inProgressCol"))).ReturnsAsync(oldColumn);
            _issueRepositoryMock.Setup(r => r.UpdateIssue(It.IsAny<UpdateIssueParams>())).ReturnsAsync(updatedIssue);
            _projectRepositoryMock.Setup(r => r.UpdateColumn(It.IsAny<UpdateColumnParams>())).ReturnsAsync(new ProjectColumnDomain());

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(updatedIssue);

            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<object>())).Returns(Task.CompletedTask);

            var result = await _issueUseCase.UpdateIssue(updateParams);
            result.CompletedAt.Should().NotBe(default(DateTime));
        }

        [Fact]
        public async Task ISS13_UpdateIssue_ChangingAssignee_ShouldSendNotification()
        {
            var updateParams = new UpdateIssueParams { Id = "issue123", AssigneeId = "newUser456", CreatorId = "user123" };
            var oldIssue = new IssueDomain { Id = "issue123", AssigneeId = "oldUser", ColumnId = "col123", ProjectId = "proj123" };
            var updatedIssue = new IssueDomain { Id = "issue123", AssigneeId = "newUser456", ColumnId = "col123", ProjectId = "proj123" };

            _issueRepositoryMock.Setup(r => r.GetIssue(updateParams.Id)).ReturnsAsync(oldIssue);
            _issueRepositoryMock.Setup(r => r.UpdateIssue(It.IsAny<UpdateIssueParams>())).ReturnsAsync(updatedIssue);

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(updatedIssue);

            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<object>())).Returns(Task.CompletedTask);

            await _issueUseCase.UpdateIssue(updateParams);

            _publisherMock.Verify(p => p.EmitKafka(
                TopicName.NOTIFICATIONS,
                KafkaMessageAction.NOTIFICATIONS_CREATE_NEW_NOTIFICATION,
                It.Is<INotificationMessage>(m => m.Type == NotificationType.ASSIGNMENT.ToString() && m.RecipientId == updatedIssue.AssigneeId)), Times.Once);
        }

        [Fact]
        public async Task ISS14_UpdateIssue_ShouldPublishActivityEvent()
        {
            var updateParams = new UpdateIssueParams { Id = "issue123", Title = "Updated Title", CreatorId = "user123" };
            var existingIssue = new IssueDomain { Id = "issue123", Title = "Old Title", ColumnId = "col123", ProjectId = "proj123" };
            var updatedIssue = new IssueDomain { Id = "issue123", Title = "Updated Title", ColumnId = "col123", ProjectId = "proj123" };

            _issueRepositoryMock.Setup(r => r.GetIssue(updateParams.Id)).ReturnsAsync(existingIssue);
            _issueRepositoryMock.Setup(r => r.UpdateIssue(It.IsAny<UpdateIssueParams>())).ReturnsAsync(updatedIssue);

            _transactionRepoMock
                .Setup(r => r.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task<IssueDomain>>>()))
                .Callback<Func<IClientSessionHandle, Task<IssueDomain>>>(async func => await func(null!))
                .ReturnsAsync(updatedIssue);

            _publisherMock.Setup(p => p.EmitKafka(It.IsAny<TopicName>(), It.IsAny<KafkaMessageAction>(), It.IsAny<object>())).Returns(Task.CompletedTask);

            await _issueUseCase.UpdateIssue(updateParams);

            _publisherMock.Verify(p => p.EmitKafka(
                TopicName.ACTIVITIES,
                KafkaMessageAction.ACTIVITIES_ISSUE_CHANGED,
                It.Is<IActivitiesMessage>(m => m.OldIssue.Id == existingIssue.Id && m.NewIssue.Id == updatedIssue.Id)), Times.Once);
        }

        [Fact]
        public async Task ISS15_UpdateIssue_WithEmptyId_ShouldThrowException()
        {
            var updateParams = new UpdateIssueParams { Id = "", Title = "Updated Title" };
            var exception = await Assert.ThrowsAsync<RpcException>(() => _issueUseCase.UpdateIssue(updateParams));
            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Issue ID cannot be empty");
        }

        #endregion

        #region DeleteIssue Tests (ISS-16 to ISS-17)

        [Fact]
        public async Task ISS16_DeleteIssue_WithValidId_ShouldSucceed()
        {
            var issueId = "issue123";
            var issue = new IssueDomain { Id = issueId, Title = "Test Issue", ColumnId = "col123", ProjectId = "proj123" };
            var column = new ProjectColumnDomain { Id = issue.ColumnId, Name = "To Do", ProjectId = "proj123" };

            _issueRepositoryMock.Setup(repo => repo.GetIssue(issueId)).ReturnsAsync(issue);
            _projectRepositoryMock.Setup(repo => repo.FindColumn(It.IsAny<GetColumnParams>())).ReturnsAsync(column);
            _projectRepositoryMock.Setup(repo => repo.UpdateColumn(It.IsAny<UpdateColumnParams>())).ReturnsAsync(column);
            _issueRepositoryMock.Setup(repo => repo.DeleteIssue(issueId)).Returns(Task.CompletedTask);

            await _issueUseCase.DeleteIssue(issueId);

            _issueRepositoryMock.Verify(repo => repo.DeleteIssue(issueId), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.UpdateColumn(It.Is<UpdateColumnParams>(p => p.RemoveIssueId == issueId)), Times.Once);
        }

        [Fact]
        public async Task ISS17_DeleteIssue_WithNonExistentId_ShouldThrowException()
        {
            var issueId = "nonexistent123";
            _issueRepositoryMock.Setup(repo => repo.GetIssue(issueId)).ReturnsAsync((IssueDomain?)null);

            var exception = await Assert.ThrowsAsync<RpcException>(() => _issueUseCase.DeleteIssue(issueId));

            exception.StatusCode.Should().Be(StatusCode.NotFound);
            exception.Status.Detail.Should().Contain($"Issue with ID {issueId} not found");
        }

        #endregion
    }
}