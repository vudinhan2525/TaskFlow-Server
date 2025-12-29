using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MainService.Domain.UseCases;
using MainService.Domain.Interfaces;
using MainService.Domain.Entities;
using FluentAssertions;
using TaskFlow.SprintService;

namespace MainService.Tests.UseCases
{
    public class SprintUseCaseTests
    {
        private readonly Mock<ISprintRepository> _sprintRepositoryMock;
        private readonly Mock<IProjectMemberRepository> _projectMemberRepositoryMock;
        private readonly Mock<IPublisherService> _publisherMock;
        private readonly SprintUseCase _sprintUseCase;

        public SprintUseCaseTests()
        {
            _sprintRepositoryMock = new Mock<ISprintRepository>();
            _projectMemberRepositoryMock = new Mock<IProjectMemberRepository>();
            _publisherMock = new Mock<IPublisherService>();

            _sprintUseCase = new SprintUseCase(
                _sprintRepositoryMock.Object,
                _projectMemberRepositoryMock.Object,
                _publisherMock.Object
            );
        }

        #region CreateSprint Tests (SPRINT-01 to SPRINT-04)

        [Fact]
        public async Task SPRINT01_CreateSprintWithValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var sprint = new SprintDomain
            {
                Name = "Sprint 1",
                ProjectId = "proj123",
                DateStarted = DateTime.UtcNow,
                DateEnded = DateTime.UtcNow.AddDays(14),
                Duration = 14,
                Goal = "Deliver login feature"
            };

            var createdSprint = new SprintDomain
            {
                Id = "sprint123",
                Name = sprint.Name,
                ProjectId = sprint.ProjectId,
                DateStarted = sprint.DateStarted,
                DateEnded = sprint.DateEnded,
                Duration = sprint.Duration,
                Goal = sprint.Goal,
                CreatedAt = DateTime.UtcNow
            };

            _sprintRepositoryMock
                .Setup(repo => repo.CreateSprint(It.IsAny<SprintDomain>()))
                .ReturnsAsync(createdSprint);

            _projectMemberRepositoryMock
                .Setup(repo => repo.GetProjectMembersAsync(sprint.ProjectId, 1, int.MaxValue))
                .ReturnsAsync(new List<ProjectMemberDomain>());

            // Act
            var result = await _sprintUseCase.CreateSprint(sprint);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdSprint.Id);
            result.Name.Should().Be(sprint.Name);
            _sprintRepositoryMock.Verify(repo => repo.CreateSprint(It.IsAny<SprintDomain>()), Times.Once);
        }

        [Fact]
        public async Task SPRINT02_CreateSprintWithEmptyName_ShouldThrowException()
        {
            // Arrange
            var sprint = new SprintDomain
            {
                Name = "",
                ProjectId = "proj123",
                DateStarted = DateTime.UtcNow,
                DateEnded = DateTime.UtcNow.AddDays(1)
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _sprintUseCase.CreateSprint(sprint));

            exception.Message.Should().Contain("Sprint name cannot be empty");
        }

        [Fact]
        public async Task SPRINT03_CreateSprintWithInvalidDates_ShouldThrowException()
        {
            // Arrange
            var sprint = new SprintDomain
            {
                Name = "Sprint 1",
                ProjectId = "proj123",
                DateStarted = DateTime.UtcNow.AddDays(1),
                DateEnded = DateTime.UtcNow // End before start
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _sprintUseCase.CreateSprint(sprint));

            exception.Message.Should().Contain("Start date must be before end date");
        }

        [Fact]
        public async Task SPRINT04_CreateSprint_ShouldNotifyMembersIfFutureSprint()
        {
            // Arrange
            var sprint = new SprintDomain
            {
                Name = "Future Sprint",
                ProjectId = "proj123",
                DateStarted = DateTime.UtcNow.AddDays(1),
                DateEnded = DateTime.UtcNow.AddDays(15)
            };

            var createdSprint = new SprintDomain
            {
                Id = "sprint456",
                Name = sprint.Name,
                ProjectId = sprint.ProjectId,
                DateStarted = sprint.DateStarted,
                DateEnded = sprint.DateEnded
            };

            var members = new List<ProjectMemberDomain>
            {
                new() { Id = "member1", UserId = "user1" },
                new() { Id = "member2", UserId = "user2" }
            };

            _sprintRepositoryMock
                .Setup(repo => repo.CreateSprint(It.IsAny<SprintDomain>()))
                .ReturnsAsync(createdSprint);

            _projectMemberRepositoryMock
                .Setup(repo => repo.GetProjectMembersAsync(sprint.ProjectId, 1, int.MaxValue))
                .ReturnsAsync(members);

            _publisherMock
                .Setup(p => p.EmitKafka(
                    TopicName.NOTIFICATIONS,
                    KafkaMessageAction.NOTIFICATIONS_CREATE_NEW_NOTIFICATION,
                    It.IsAny<INotificationMessage>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sprintUseCase.CreateSprint(sprint);

            // Assert
            _publisherMock.Verify(p => p.EmitKafka(
                TopicName.NOTIFICATIONS,
                KafkaMessageAction.NOTIFICATIONS_CREATE_NEW_NOTIFICATION,
                It.Is<INotificationMessage>(m => m.Type == NotificationType.SPRINT_STARTED.ToString())),
                Times.Exactly(2));
        }

        #endregion

        #region GetSprint Tests (SPRINT-05 to SPRINT-07)

        [Fact]
        public async Task SPRINT05_GetSprintById_WithValidId_ShouldReturnSprint()
        {
            // Arrange
            var sprintId = "sprint123";
            var sprint = new SprintDomain
            {
                Id = sprintId,
                Name = "Sprint 1",
                ProjectId = "proj123"
            };

            _sprintRepositoryMock
                .Setup(repo => repo.GetSprint(sprintId))
                .ReturnsAsync(sprint);

            // Act
            var result = await _sprintUseCase.GetSprint(sprintId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(sprintId);
        }

        [Fact]
        public async Task SPRINT06_GetSprintById_WithInvalidId_ShouldThrowException()
        {
            // Arrange
            var sprintId = "invalid123";

            _sprintRepositoryMock
                .Setup(repo => repo.GetSprint(sprintId))
                .ReturnsAsync((SprintDomain?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sprintUseCase.GetSprint(sprintId));

            exception.Message.Should().Contain($"Sprint with ID {sprintId} not found");
        }

        [Fact]
        public async Task SPRINT07_GetSprintById_WithEmptyId_ShouldThrowException()
        {
            // Arrange
            var sprintId = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _sprintUseCase.GetSprint(sprintId));

            exception.Message.Should().Contain("Sprint ID cannot be empty");
        }

        #endregion

        #region UpdateSprint Tests (SPRINT-08 to SPRINT-11)

        [Fact]
        public async Task SPRINT08_UpdateSprint_WithValidData_ShouldSucceed()
        {
            // Arrange
            var existingSprint = new SprintDomain
            {
                Id = "sprint123",
                Name = "Old Sprint",
                ProjectId = "proj123",
                DateStarted = DateTime.UtcNow,
                DateEnded = DateTime.UtcNow.AddDays(10)
            };

            var updatedSprint = new SprintDomain
            {
                Id = "sprint123",
                Name = "Updated Sprint",
                ProjectId = "proj123",
                DateStarted = DateTime.UtcNow,
                DateEnded = DateTime.UtcNow.AddDays(14),
                Goal = "New goal"
            };

            _sprintRepositoryMock
                .Setup(repo => repo.GetSprint("sprint123"))
                .ReturnsAsync(existingSprint);

            _sprintRepositoryMock
                .Setup(repo => repo.UpdateSprint(It.IsAny<SprintDomain>()))
                .ReturnsAsync(updatedSprint);

            // Act
            var result = await _sprintUseCase.UpdateSprint(updatedSprint);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Sprint");
            result.Goal.Should().Be("New goal");
        }

        [Fact]
        public async Task SPRINT09_UpdateSprint_WithEmptyId_ShouldThrowException()
        {
            // Arrange
            var sprint = new SprintDomain
            {
                Id = "",
                Name = "Sprint"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _sprintUseCase.UpdateSprint(sprint));

            exception.Message.Should().Contain("Sprint ID cannot be empty");
        }

        [Fact]
        public async Task SPRINT10_UpdateSprint_WithEmptyName_ShouldThrowException()
        {
            // Arrange
            var sprint = new SprintDomain
            {
                Id = "sprint123",
                Name = ""
            };

            _sprintRepositoryMock
                .Setup(repo => repo.GetSprint("sprint123"))
                .ReturnsAsync(new SprintDomain { Id = "sprint123", Name = "Old" });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _sprintUseCase.UpdateSprint(sprint));

            exception.Message.Should().Contain("Sprint name cannot be empty");
        }

        [Fact]
        public async Task SPRINT11_UpdateSprint_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var sprint = new SprintDomain
            {
                Id = "nonexistent123",
                Name = "Sprint"
            };

            _sprintRepositoryMock
                .Setup(repo => repo.GetSprint("nonexistent123"))
                .ReturnsAsync((SprintDomain?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sprintUseCase.UpdateSprint(sprint));

            exception.Message.Should().Contain("Sprint with ID nonexistent123 not found");
        }

        #endregion

        #region DeleteSprint Tests (SPRINT-12 to SPRINT-13)

        [Fact]
        public async Task SPRINT12_DeleteSprint_WithValidId_ShouldSucceed()
        {
            // Arrange
            var sprintId = "sprint123";
            var sprint = new SprintDomain { Id = sprintId, Name = "Sprint" };

            _sprintRepositoryMock
                .Setup(repo => repo.GetSprint(sprintId))
                .ReturnsAsync(sprint);

            _sprintRepositoryMock
                .Setup(repo => repo.DeleteSprint(sprintId))
                .Returns(Task.CompletedTask);

            // Act
            await _sprintUseCase.DeleteSprint(sprintId);

            // Assert
            _sprintRepositoryMock.Verify(repo => repo.DeleteSprint(sprintId), Times.Once);
        }

        [Fact]
        public async Task SPRINT13_DeleteSprint_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var sprintId = "nonexistent123";

            _sprintRepositoryMock
                .Setup(repo => repo.GetSprint(sprintId))
                .ReturnsAsync((SprintDomain?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sprintUseCase.DeleteSprint(sprintId));

            exception.Message.Should().Contain($"Sprint with ID {sprintId} not found");
        }

        #endregion
    }
}