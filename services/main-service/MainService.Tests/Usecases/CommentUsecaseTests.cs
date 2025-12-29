using Xunit;
using Moq;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MainService.Domain.UseCases;
using MainService.Domain.Interfaces;
using MainService.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace MainService.Tests.UseCases
{
    public class CommentUseCaseTests
    {
        private readonly Mock<ICommentsRepository> _commentsRepositoryMock;
        private readonly Mock<ILogger<CommentUseCase>> _loggerMock;
        private readonly CommentUseCase _commentUseCase;

        public CommentUseCaseTests()
        {
            _commentsRepositoryMock = new Mock<ICommentsRepository>();
            _loggerMock = new Mock<ILogger<CommentUseCase>>();

            _commentUseCase = new CommentUseCase(
                _commentsRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        #region CreateComment Tests (COMMENT-01 to COMMENT-04)

        [Fact]
        public async Task COMMENT01_CreateCommentWithValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var comment = new CommentDomain
            {
                IssueId = "issue123",
                UserId = "user123",
                Content = "Great work!"
            };

            var createdComment = new CommentDomain
            {
                Id = "comment123",
                IssueId = comment.IssueId,
                UserId = comment.UserId,
                Content = comment.Content,
                CreatedAt = DateTime.UtcNow
            };

            _commentsRepositoryMock
                .Setup(repo => repo.CreateComment(It.IsAny<CommentDomain>()))
                .ReturnsAsync(createdComment);

            // Act
            var result = await _commentUseCase.CreateComment(comment);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("comment123");
            result.Content.Should().Be("Great work!");
        }

        [Fact]
        public async Task COMMENT02_CreateCommentWithEmptyUserId_ShouldThrowException()
        {
            // Arrange
            var comment = new CommentDomain
            {
                IssueId = "issue123",
                UserId = "",
                Content = "Test"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _commentUseCase.CreateComment(comment));

            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Missing required fields");
        }

        [Fact]
        public async Task COMMENT03_CreateCommentWithEmptyContent_ShouldThrowException()
        {
            // Arrange
            var comment = new CommentDomain
            {
                IssueId = "issue123",
                UserId = "user123",
                Content = ""
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _commentUseCase.CreateComment(comment));

            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Missing required fields");
        }

        [Fact]
        public async Task COMMENT04_CreateCommentWithNullContent_ShouldThrowException()
        {
            // Arrange
            var comment = new CommentDomain
            {
                IssueId = "issue123",
                UserId = "user123",
                Content = null
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _commentUseCase.CreateComment(comment));

            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Missing required fields");
        }

        #endregion

        #region GetComment Tests (COMMENT-05 to COMMENT-07)

        [Fact]
        public async Task COMMENT05_GetCommentById_WithValidId_ShouldReturnComment()
        {
            // Arrange
            var commentId = "comment123";
            var comment = new CommentDomain
            {
                Id = commentId,
                IssueId = "issue123",
                UserId = "user123",
                Content = "Test comment"
            };

            _commentsRepositoryMock
                .Setup(repo => repo.GetComment(commentId))
                .ReturnsAsync(comment);

            // Act
            var result = await _commentUseCase.GetComment(commentId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(commentId);
        }

        [Fact]
        public async Task COMMENT06_GetCommentById_WithEmptyId_ShouldThrowException()
        {
            // Arrange
            var commentId = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _commentUseCase.GetComment(commentId));

            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Comment ID is required");
        }

        [Fact]
        public async Task COMMENT07_GetCommentById_WithNullId_ShouldThrowException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _commentUseCase.GetComment(null));

            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Comment ID is required");
        }

        #endregion

        #region ListComments Tests (COMMENT-08 to COMMENT-09)

        [Fact]
        public async Task COMMENT08_ListComments_WithValidParams_ShouldReturnComments()
        {
            // Arrange
            var param = new GetCommentParams
            {
                IssueId = "issue123",
                Page = 1,
                Limit = 10
            };

            var comments = new List<CommentDomain>
            {
                new() { Id = "c1", IssueId = "issue123", UserId = "u1", Content = "C1" },
                new() { Id = "c2", IssueId = "issue123", UserId = "u2", Content = "C2" }
            };

            _commentsRepositoryMock
                .Setup(repo => repo.ListComments(param))
                .ReturnsAsync((comments, comments.Count));

            // Act
            var (resultComments, totalCount) = await _commentUseCase.ListComments(param);

            // Assert
            resultComments.Should().HaveCount(2);
            totalCount.Should().Be(2);
        }

        [Fact]
        public async Task COMMENT09_ListComments_WithInvalidPagination_ShouldThrowException()
        {
            // Arrange
            var param = new GetCommentParams
            {
                IssueId = "issue123",
                Page = 0,
                Limit = -5
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _commentUseCase.ListComments(param));

            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Invalid pagination parameters");
        }

        #endregion

        #region DeleteComment Tests (COMMENT-10 to COMMENT-11)

        [Fact]
        public async Task COMMENT10_DeleteComment_WithValidId_ShouldSucceed()
        {
            // Arrange
            var commentId = "comment123";

            _commentsRepositoryMock
                .Setup(repo => repo.DeleteComment(commentId))
                .Returns(Task.CompletedTask);

            // Act
            await _commentUseCase.DeleteComment(commentId);

            // Assert
            _commentsRepositoryMock.Verify(repo => repo.DeleteComment(commentId), Times.Once);
        }

        [Fact]
        public async Task COMMENT11_DeleteComment_WithEmptyId_ShouldThrowException()
        {
            // Arrange
            var commentId = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _commentUseCase.DeleteComment(commentId));

            exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
            exception.Status.Detail.Should().Contain("Comment ID is required");
        }

        #endregion
    }
}