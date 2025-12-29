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
using MongoDB.Driver;

namespace MainService.Tests.UseCases
{
    public class ProjectUseCaseTests
    {
        private readonly Mock<ITransactionRepo> _transactionRepoMock;
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IProjectMemberRepository> _projectMemberRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IssueUseCase> _issueUseCaseMock;
        private readonly ProjectUseCase _projectUseCase;

        public ProjectUseCaseTests()
        {
            _transactionRepoMock = new Mock<ITransactionRepo>();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _projectMemberRepositoryMock = new Mock<IProjectMemberRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _issueUseCaseMock = new Mock<IssueUseCase>(
                Mock.Of<IIssueRepository>(),
                Mock.Of<IUserRepository>(),
                Mock.Of<ISprintRepository>(),
                Mock.Of<IActivitiesRepository>(),
                Mock.Of<IProjectRepository>(),
                Mock.Of<ITransactionRepo>(),
                Mock.Of<ILogger<IssueUseCase>>(),
                Mock.Of<IPublisherService>()
            );

            _projectUseCase = new ProjectUseCase(
                _projectRepositoryMock.Object,
                _transactionRepoMock.Object,
                _projectMemberRepositoryMock.Object,
                _issueUseCaseMock.Object,
                _userRepositoryMock.Object
            );
        }

        #region CreateProject Tests (PROJ-01 to PROJ-05)

        [Fact]
        public async Task PROJ01_CreateProjectWithValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var project = new ProjectDomain
            {
                Name = "Test Project",
                Key = "TEST",
                OwnerId = "owner123",
                Access = ProjectAccess.Private,
                Type = ProjectType.Scrum
            };

            var createdProject = new ProjectDomain
            {
                Id = "proj123",
                Name = project.Name,
                Key = project.Key,
                OwnerId = project.OwnerId,
                Access = project.Access,
                Type = project.Type,
                CreatedAt = DateTime.UtcNow
            };

            var ownerMember = new ProjectMemberDomain
            {
                Id = "member123",
                ProjectId = createdProject.Id!,
                UserId = project.OwnerId,
                Role = TeamMemberRole.Owner,
                IsPending = false
            };

            // FIX: Setup ExecuteAsync with correct signature (IClientSessionHandle, not object)
            _transactionRepoMock
                .Setup(repo => repo.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task>>()))
                .Returns((Func<IClientSessionHandle, Task> func) => func(null!));

            _projectRepositoryMock
                .Setup(repo => repo.CreateProject(It.IsAny<ProjectDomain>()))
                .ReturnsAsync(createdProject);

            _projectMemberRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<ProjectMemberDomain>()))
                .ReturnsAsync(ownerMember);

            _projectRepositoryMock
                .Setup(repo => repo.UpdateProject(It.IsAny<ProjectDomain>()))
                .ReturnsAsync(createdProject);

            // Act
            var result = await _projectUseCase.CreateProject(project);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(createdProject.Id);
            result.Name.Should().Be(project.Name);
            _projectRepositoryMock.Verify(repo => repo.CreateProject(It.IsAny<ProjectDomain>()), Times.Once);
            _projectMemberRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ProjectMemberDomain>()), Times.Once);
        }

        [Fact]
        public async Task PROJ02_CreateProjectWithEmptyName_ShouldThrowException()
        {
            // Arrange
            var project = new ProjectDomain
            {
                Name = "",
                Key = "TEST",
                OwnerId = "owner123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _projectUseCase.CreateProject(project));
            
            exception.Message.Should().Contain("Project name cannot be empty");
        }

        [Fact]
        public async Task PROJ03_CreateProjectWithEmptyKey_ShouldThrowException()
        {
            // Arrange
            var project = new ProjectDomain
            {
                Name = "Test Project",
                Key = "",
                OwnerId = "owner123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _projectUseCase.CreateProject(project));
            
            exception.Message.Should().Contain("Project key cannot be empty");
        }

        [Fact]
        public async Task PROJ04_CreateProjectWithEmptyOwnerId_ShouldThrowException()
        {
            // Arrange
            var project = new ProjectDomain
            {
                Name = "Test Project",
                Key = "TEST",
                OwnerId = ""
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _projectUseCase.CreateProject(project));
            
            exception.Message.Should().Contain("Project owner ID cannot be empty");
        }

        [Fact]
        public async Task PROJ05_CreateProject_ShouldCreateOwnerMemberAutomatically()
        {
            // Arrange
            var project = new ProjectDomain
            {
                Name = "Test Project",
                Key = "TEST",
                OwnerId = "owner123"
            };

            var createdProject = new ProjectDomain
            {
                Id = "proj123",
                Name = project.Name,
                Key = project.Key,
                OwnerId = project.OwnerId
            };

            // FIX: Setup ExecuteAsync with correct signature
            _transactionRepoMock
                .Setup(repo => repo.ExecuteAsync(It.IsAny<Func<IClientSessionHandle, Task>>()))
                .Returns((Func<IClientSessionHandle, Task> func) => func(null!));

            _projectRepositoryMock
                .Setup(repo => repo.CreateProject(It.IsAny<ProjectDomain>()))
                .ReturnsAsync(createdProject);

            _projectMemberRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<ProjectMemberDomain>()))
                .ReturnsAsync(new ProjectMemberDomain
                {
                    ProjectId = createdProject.Id!,
                    UserId = project.OwnerId,
                    Role = TeamMemberRole.Owner,
                    IsPending = false
                });

            _projectRepositoryMock
                .Setup(repo => repo.UpdateProject(It.IsAny<ProjectDomain>()))
                .ReturnsAsync(createdProject);

            // Act
            await _projectUseCase.CreateProject(project);

            // Assert
            _projectMemberRepositoryMock.Verify(repo => repo.AddAsync(
                It.Is<ProjectMemberDomain>(m => 
                    m.UserId == project.OwnerId && 
                    m.Role == TeamMemberRole.Owner && 
                    m.IsPending == false)), Times.Once);
        }

        #endregion

        #region GetProject Tests (PROJ-06 to PROJ-08)

        [Fact]
        public async Task PROJ06_GetProjectById_WithValidId_ShouldReturnProject()
        {
            // Arrange
            var projectId = "proj123";
            var project = new ProjectDomain
            {
                Id = projectId,
                Name = "Test Project",
                Key = "TEST"
            };

            var members = new List<ProjectMemberDomain>
            {
                new ProjectMemberDomain 
                { 
                    Id = "member1", 
                    UserId = "user1", 
                    ProjectId = projectId,
                    Role = TeamMemberRole.Owner
                }
            };

            var user = new UserDomain
            {
                Id = "user1",
                FirstName = "John",
                LastName = "Doe"
            };

            _projectRepositoryMock
                .Setup(repo => repo.GetProject(projectId))
                .ReturnsAsync(project);

            _projectMemberRepositoryMock
                .Setup(repo => repo.GetProjectMembersAsync(projectId, 1, 100))
                .ReturnsAsync(members);

            _userRepositoryMock
                .Setup(repo => repo.FindUserAsync(It.IsAny<UserQueryParams>()))
                .ReturnsAsync(user);

            // Act
            var result = await _projectUseCase.GetProject(projectId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(projectId);
            result.ProjectMembers.Should().HaveCount(1);
        }

        [Fact]
        public async Task PROJ07_GetProjectById_WithInvalidId_ShouldThrowException()
        {
            // Arrange
            var projectId = "invalid123";

            _projectRepositoryMock
                .Setup(repo => repo.GetProject(projectId))
                .ReturnsAsync((ProjectDomain?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _projectUseCase.GetProject(projectId));
            
            exception.Message.Should().Contain($"Project with ID {projectId} not found");
        }

        [Fact]
        public async Task PROJ08_GetProjectById_WithEmptyId_ShouldThrowException()
        {
            // Arrange
            var projectId = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _projectUseCase.GetProject(projectId));
            
            exception.Message.Should().Contain("Project ID cannot be empty");
        }

        #endregion

        #region UpdateProject Tests (PROJ-09 to PROJ-12)

        [Fact]
        public async Task PROJ09_UpdateProject_WithValidData_ShouldSucceed()
        {
            // Arrange
            var existingProject = new ProjectDomain
            {
                Id = "proj123",
                Name = "Old Name",
                Key = "OLD",
                OwnerId = "owner123",
                Access = ProjectAccess.Private
            };

            var updateProject = new ProjectDomain
            {
                Id = "proj123",
                Name = "New Name",
                Key = "NEW",
                OwnerId = "owner123",
                Access = ProjectAccess.Public,
                Type = ProjectType.Scrum
            };

            _projectRepositoryMock
                .Setup(repo => repo.GetProject(updateProject.Id))
                .ReturnsAsync(existingProject);

            _projectRepositoryMock
                .Setup(repo => repo.UpdateProject(It.IsAny<ProjectDomain>()))
                .ReturnsAsync(updateProject);

            // Act
            var result = await _projectUseCase.UpdateProject(updateProject);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(updateProject.Name);
            result.Key.Should().Be(updateProject.Key);
        }

        [Fact]
        public async Task PROJ10_UpdateProject_ChangingOwner_ShouldThrowException()
        {
            // Arrange
            var existingProject = new ProjectDomain
            {
                Id = "proj123",
                Name = "Test Project",
                OwnerId = "owner123"
            };

            var updateProject = new ProjectDomain
            {
                Id = "proj123",
                Name = "Test Project",
                OwnerId = "newowner456" // Trying to change owner
            };

            _projectRepositoryMock
                .Setup(repo => repo.GetProject(updateProject.Id))
                .ReturnsAsync(existingProject);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _projectUseCase.UpdateProject(updateProject));
            
            exception.Message.Should().Contain("Cannot change project owner through update");
        }

        [Fact]
        public async Task PROJ11_UpdateProject_WithEmptyId_ShouldThrowException()
        {
            // Arrange
            var project = new ProjectDomain
            {
                Id = "",
                Name = "Test Project"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _projectUseCase.UpdateProject(project));
            
            exception.Message.Should().Contain("Project ID cannot be empty");
        }

        [Fact]
        public async Task PROJ12_UpdateProject_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var project = new ProjectDomain
            {
                Id = "nonexistent123",
                Name = "Test Project"
            };

            _projectRepositoryMock
                .Setup(repo => repo.GetProject(project.Id))
                .ReturnsAsync((ProjectDomain?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _projectUseCase.UpdateProject(project));
            
            exception.Message.Should().Contain($"Project with ID {project.Id} not found");
        }

        #endregion

        #region DeleteProject Tests (PROJ-13 to PROJ-14)

        [Fact]
        public async Task PROJ13_DeleteProject_WithValidId_ShouldSucceed()
        {
            // Arrange
            var projectId = "proj123";
            var project = new ProjectDomain
            {
                Id = projectId,
                Name = "Test Project"
            };

            _projectRepositoryMock
                .Setup(repo => repo.GetProject(projectId))
                .ReturnsAsync(project);

            _projectRepositoryMock
                .Setup(repo => repo.DeleteProject(projectId))
                .Returns(Task.CompletedTask);

            // Act
            await _projectUseCase.DeleteProject(projectId);

            // Assert
            _projectRepositoryMock.Verify(repo => repo.DeleteProject(projectId), Times.Once);
        }

        [Fact]
        public async Task PROJ14_DeleteProject_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var projectId = "nonexistent123";

            _projectRepositoryMock
                .Setup(repo => repo.GetProject(projectId))
                .ReturnsAsync((ProjectDomain?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _projectUseCase.DeleteProject(projectId));
            
            exception.Message.Should().Contain($"Project with ID {projectId} not found");
        }

        #endregion

        #region Column Tests (COL-01 to COL-05)

        [Fact]
        public async Task COL01_CreateColumn_ShouldAssignCorrectOrder()
        {
            // Arrange
            var param = new CreateColumnParams
            {
                Name = "New Column",
                ProjectId = "proj123"
            };

            var existingColumns = new List<ProjectColumnDomain>
            {
                new ProjectColumnDomain { Id = "col1", Order = 1 },
                new ProjectColumnDomain { Id = "col2", Order = 2 }
            };

            var newColumn = new ProjectColumnDomain
            {
                Id = "col3",
                Name = param.Name,
                ProjectId = param.ProjectId,
                Order = 3
            };

            _projectRepositoryMock
                .Setup(repo => repo.FindColumnsByProjectId(It.IsAny<ListProjectColumnsParams>()))
                .ReturnsAsync(existingColumns);

            _projectRepositoryMock
                .Setup(repo => repo.CreateColumn(It.IsAny<ProjectColumnDomain>()))
                .ReturnsAsync(newColumn);

            // Act
            var result = await _projectUseCase.CreateColumn(param);

            // Assert
            result.Should().NotBeNull();
            result.Order.Should().Be(3);
            result.Name.Should().Be(param.Name);
        }

        [Fact]
        public async Task COL02_CreateColumn_InEmptyProject_ShouldHaveOrderOne()
        {
            // Arrange
            var param = new CreateColumnParams
            {
                Name = "First Column",
                ProjectId = "proj123"
            };

            _projectRepositoryMock
                .Setup(repo => repo.FindColumnsByProjectId(It.IsAny<ListProjectColumnsParams>()))
                .ReturnsAsync(new List<ProjectColumnDomain>());

            _projectRepositoryMock
                .Setup(repo => repo.CreateColumn(It.IsAny<ProjectColumnDomain>()))
                .ReturnsAsync(new ProjectColumnDomain
                {
                    Id = "col1",
                    Name = param.Name,
                    ProjectId = param.ProjectId,
                    Order = 1
                });

            // Act
            var result = await _projectUseCase.CreateColumn(param);

            // Assert
            result.Order.Should().Be(1);
        }

        [Fact]
        public async Task COL03_UpdateColumn_ShouldUpdateSuccessfully()
        {
            // Arrange
            var param = new UpdateColumnParams
            {
                ColumnId = "col123",
                Name = "Updated Column"
            };

            var updatedColumn = new ProjectColumnDomain
            {
                Id = param.ColumnId,
                Name = param.Name
            };

            _projectRepositoryMock
                .Setup(repo => repo.UpdateColumn(It.IsAny<UpdateColumnParams>()))
                .ReturnsAsync(updatedColumn);

            // Act
            var result = await _projectUseCase.UpdateColumn(param);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(param.Name);
        }

        [Fact]
        public async Task COL04_DeleteColumn_WithIssues_ShouldThrowException()
        {
            // Arrange
            var param = new DeleteColumnParams
            {
                ColumnId = "col123"
            };

            var column = new ProjectColumnDomain
            {
                Id = param.ColumnId,
                Name = "Column With Issues",
                IssueIds = new List<string> { "issue1", "issue2" }
            };

            _projectRepositoryMock
                .Setup(repo => repo.FindColumn(It.IsAny<GetColumnParams>()))
                .ReturnsAsync(column);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() => 
                _projectUseCase.DeleteColumn(param));
            
            exception.StatusCode.Should().Be(StatusCode.FailedPrecondition);
            exception.Status.Detail.Should().Contain("Cannot delete column that contains issues");
        }

        [Fact]
        public async Task COL05_DeleteColumn_WithoutIssues_ShouldSucceed()
        {
            // Arrange
            var param = new DeleteColumnParams
            {
                ColumnId = "col123"
            };

            var column = new ProjectColumnDomain
            {
                Id = param.ColumnId,
                Name = "Empty Column",
                IssueIds = new List<string>()
            };

            _projectRepositoryMock
                .Setup(repo => repo.FindColumn(It.IsAny<GetColumnParams>()))
                .ReturnsAsync(column);

            _projectRepositoryMock
                .Setup(repo => repo.DeleteColumn(It.IsAny<DeleteColumnParams>()))
                .Returns(Task.CompletedTask);

            // Act
            await _projectUseCase.DeleteColumn(param);

            // Assert
            _projectRepositoryMock.Verify(repo => repo.DeleteColumn(param), Times.Once);
        }

        #endregion
    }
}