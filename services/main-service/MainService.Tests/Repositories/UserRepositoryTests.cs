// using Xunit;
// using Moq;
// using FluentAssertions;
// using MainService.Infras.Repositories;
// using MainService.Domain.Entities;
// using MainService.Domain.Enums;
// using MainService.Infras.Entities;
// using MainService.Domain.Interfaces;
// using MongoDB.Driver;
// using AutoMapper;
// using Microsoft.Extensions.Logging;
// using Grpc.Core;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using MainService.Infras;

// namespace MainService.Tests.Repositories
// {
//     public class UserRepositoryTests
//     {
//         private readonly Mock<IMongoCollection<User>> _userCollectionMock;
//         private readonly Mock<IMapper> _mapperMock;
//         private readonly Mock<ILogger<UserRepository>> _loggerMock;
//         private readonly Mock<MongoDbService> _mongoDbServiceMock;

//         public UserRepositoryTests()
//         {
//             _userCollectionMock = new Mock<IMongoCollection<User>>();
//             _mapperMock = new Mock<IMapper>();
//             _loggerMock = new Mock<ILogger<UserRepository>>();
//             _mongoDbServiceMock = new Mock<MongoDbService>();
//         }

//         #region CreateUserAsync Tests

//         [Fact]
//         public async Task CreateUserAsync_WithValidData_ShouldReturnUserWithId()
//         {
//             // Arrange
//             var userDomain = new UserDomain
//             {
//                 FirstName = "John",
//                 LastName = "Doe",
//                 Email = "john@example.com",
//                 Password = "hashedPassword",
//                 Role = UserRole.User,
//                 IsVerified = false,
//                 CreatedAt = DateTime.UtcNow,
//                 UpdatedAt = DateTime.UtcNow
//             };

//             var userEntity = new User
//             {
//                 Id = "user123",
//                 FirstName = userDomain.FirstName,
//                 LastName = userDomain.LastName,
//                 Email = userDomain.Email,
//                 Password = userDomain.Password
//             };

//             _mapperMock
//                 .Setup(x => x.Map<User>(It.IsAny<UserDomain>()))
//                 .Returns(userEntity);

//             // Note: In real tests, you would need to properly mock MongoDB operations
//             // This is a simplified example

//             // Act & Assert
//             // This test demonstrates the structure
//             // Full implementation would require MongoDB test containers or in-memory database
//             userEntity.Id.Should().NotBeNullOrEmpty();
//             userEntity.Email.Should().Be(userDomain.Email);
//         }

//         [Fact]
//         public async Task CreateUserAsync_WithDuplicateEmail_ShouldThrowException()
//         {
//             // Arrange
//             var userDomain = new UserDomain
//             {
//                 Email = "existing@example.com",
//                 Password = "password123"
//             };

//             // MongoDB will throw duplicate key exception
//             // This test demonstrates expected behavior

//             // Act & Assert
//             // In real implementation, this would throw MongoWriteException
//             // which should be caught and converted to appropriate RpcException
//         }

//         #endregion

//         #region FindUserAsync Tests

//         [Fact]
//         public async Task FindUserAsync_WithValidEmail_ShouldReturnUser()
//         {
//             // Arrange
//             var email = "user@test.com";
//             var query = new UserQueryParams { Email = email };

//             var userEntity = new User
//             {
//                 Id = "user123",
//                 Email = email,
//                 FirstName = "John",
//                 LastName = "Doe"
//             };

//             var userDomain = new UserDomain
//             {
//                 Id = userEntity.Id,
//                 Email = userEntity.Email,
//                 FirstName = userEntity.FirstName,
//                 LastName = userEntity.LastName
//             };

//             _mapperMock
//                 .Setup(x => x.Map<UserDomain>(It.IsAny<User>()))
//                 .Returns(userDomain);

//             // Act & Assert
//             userDomain.Should().NotBeNull();
//             userDomain.Email.Should().Be(email);
//         }

//         [Fact]
//         public async Task FindUserAsync_WithNonExistentEmail_ShouldReturnNull()
//         {
//             // Arrange
//             var query = new UserQueryParams { Email = "nonexistent@test.com" };

//             // MongoDB will return null for not found
//             // This test demonstrates expected behavior

//             // Act & Assert
//             // Result should be null
//         }

//         [Fact]
//         public async Task FindUserAsync_WithUserId_ShouldReturnUser()
//         {
//             // Arrange
//             var userId = "user123";
//             var query = new UserQueryParams { UserId = userId };

//             var userEntity = new User
//             {
//                 Id = userId,
//                 Email = "user@test.com",
//                 FirstName = "John"
//             };

//             var userDomain = new UserDomain
//             {
//                 Id = userEntity.Id,
//                 Email = userEntity.Email,
//                 FirstName = userEntity.FirstName
//             };

//             _mapperMock
//                 .Setup(x => x.Map<UserDomain>(It.IsAny<User>()))
//                 .Returns(userDomain);

//             // Act & Assert
//             userDomain.Should().NotBeNull();
//             userDomain.Id.Should().Be(userId);
//         }

//         #endregion

//         #region UpdateUserAsync Tests

//         [Fact]
//         public async Task UpdateUserAsync_WithValidData_ShouldReturnUpdatedUser()
//         {
//             // Arrange
//             var updateParams = new UpdateUserParams
//             {
//                 Id = "user123",
//                 FirstName = "Jane",
//                 LastName = "Smith",
//                 Avatar = "new-avatar.jpg"
//             };

//             var updatedUserEntity = new User
//             {
//                 Id = updateParams.Id,
//                 FirstName = updateParams.FirstName,
//                 LastName = updateParams.LastName,
//                 Avatar = updateParams.Avatar
//             };

//             var updatedUserDomain = new UserDomain
//             {
//                 Id = updatedUserEntity.Id,
//                 FirstName = updatedUserEntity.FirstName,
//                 LastName = updatedUserEntity.LastName,
//                 Avatar = updatedUserEntity.Avatar
//             };

//             _mapperMock
//                 .Setup(x => x.Map<UserDomain>(It.IsAny<User>()))
//                 .Returns(updatedUserDomain);

//             // Act & Assert
//             updatedUserDomain.Should().NotBeNull();
//             updatedUserDomain.FirstName.Should().Be(updateParams.FirstName);
//             updatedUserDomain.LastName.Should().Be(updateParams.LastName);
//         }

//         [Fact]
//         public async Task UpdateUserAsync_WithNonExistentUser_ShouldThrowException()
//         {
//             // Arrange
//             var updateParams = new UpdateUserParams
//             {
//                 Id = "nonexistent123",
//                 FirstName = "Jane"
//             };

//             // MongoDB UpdateOneAsync will return MatchedCount = 0
//             // This should throw RpcException with NotFound status

//             // Act & Assert
//             // Should throw RpcException with StatusCode.NotFound
//         }

//         #endregion

//         #region SearchUsersAsync Tests

//         [Fact]
//         public async Task SearchUsersAsync_WithNameFilter_ShouldReturnMatchingUsers()
//         {
//             // Arrange
//             var searchParams = new SearchUserQueryParams
//             {
//                 Name = "John",
//                 Page = 1,
//                 Limit = 10
//             };

//             var users = new List<User>
//             {
//                 new User { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@test.com" },
//                 new User { Id = "2", FirstName = "Johnny", LastName = "Smith", Email = "johnny@test.com" }
//             };

//             var userDomains = users.Select(u => new UserDomain
//             {
//                 Id = u.Id,
//                 FirstName = u.FirstName,
//                 LastName = u.LastName,
//                 Email = u.Email
//             }).ToList();

//             // Act & Assert
//             userDomains.Should().HaveCount(2);
//             userDomains.Should().OnlyContain(u => 
//                 u.FirstName.Contains("John", StringComparison.OrdinalIgnoreCase));
//         }

//         [Fact]
//         public async Task SearchUsersAsync_WithEmailFilter_ShouldReturnMatchingUsers()
//         {
//             // Arrange
//             var searchParams = new SearchUserQueryParams
//             {
//                 Email = "test.com",
//                 Page = 1,
//                 Limit = 10
//             };

//             var users = new List<User>
//             {
//                 new User { Id = "1", Email = "user1@test.com" },
//                 new User { Id = "2", Email = "user2@test.com" }
//             };

//             // Act & Assert
//             users.Should().OnlyContain(u => u.Email.Contains("test.com"));
//         }

//         [Fact]
//         public async Task SearchUsersAsync_WithUserIdsFilter_ShouldReturnSpecificUsers()
//         {
//             // Arrange
//             var userIds = new List<string> { "user1", "user2", "user3" };
//             var searchParams = new SearchUserQueryParams
//             {
//                 UserIds = userIds,
//                 Page = 1,
//                 Limit = 10
//             };

//             var users = new List<User>
//             {
//                 new User { Id = "user1", Email = "user1@test.com" },
//                 new User { Id = "user2", Email = "user2@test.com" },
//                 new User { Id = "user3", Email = "user3@test.com" }
//             };

//             // Act & Assert
//             users.Should().HaveCount(3);
//             users.Should().OnlyContain(u => userIds.Contains(u.Id));
//         }

//         [Fact]
//         public async Task SearchUsersAsync_WithPagination_ShouldReturnCorrectPage()
//         {
//             // Arrange
//             var searchParams = new SearchUserQueryParams
//             {
//                 Page = 2,
//                 Limit = 10
//             };

//             // This tests that pagination correctly skips first 10 results
//             // and returns next 10

//             // Act & Assert
//             // Verify that Skip((page - 1) * limit) is called correctly
//         }

//         [Fact]
//         public async Task SearchUsersAsync_WithVietnameseCharacters_ShouldHandleAccents()
//         {
//             // Arrange
//             var searchParams = new SearchUserQueryParams
//             {
//                 Name = "Nguyen", // Should match "Nguyễn" 
//                 Page = 1,
//                 Limit = 10
//             };

//             // The NormalizeVietnamese extension method should handle this
//             // Test verifies accent-insensitive search works

//             // Act & Assert
//             // Should match users with names like "Nguyễn", "Nguyen", etc.
//         }

//         [Fact]
//         public async Task SearchUsersAsync_WithNoResults_ShouldReturnEmptyList()
//         {
//             // Arrange
//             var searchParams = new SearchUserQueryParams
//             {
//                 Email = "nonexistent@nowhere.com",
//                 Page = 1,
//                 Limit = 10
//             };

//             // Act & Assert
//             // Should return empty list with TotalCount = 0
//         }

//         #endregion

//         #region String Extension Tests

//         [Fact]
//         public void NormalizeVietnamese_WithAccentedCharacters_ShouldRemoveAccents()
//         {
//             // Arrange
//             var input = "Nguyễn Văn A";
//             var expected = "Nguyen Van A";

//             // Act
//             var result = input.NormalizeVietnamese();

//             // Assert
//             result.Should().Be(expected);
//         }

//         [Fact]
//         public void NormalizeVietnamese_WithNullOrEmpty_ShouldReturnSameValue()
//         {
//             // Arrange & Act & Assert
//             string.Empty.NormalizeVietnamese().Should().Be(string.Empty);
//             ((string)null).NormalizeVietnamese().Should().BeNull();
//         }

//         [Fact]
//         public void BuildAccentInsensitivePattern_WithVietnameseCharacters_ShouldBuildCorrectRegex()
//         {
//             // Arrange
//             var input = "nguyen";

//             // Act
//             var pattern = RegexHelper.BuildAccentInsensitivePattern(input);

//             // Assert
//             pattern.Should().NotBeNullOrEmpty();
//             pattern.Should().Contain("["); // Should contain character classes
//         }

//         #endregion
//     }
// }