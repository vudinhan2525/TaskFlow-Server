// using Xunit;
// using Moq;
// using Grpc.Core;
// using FluentAssertions;
// using MainService.Domain.UseCases;
// using MainService.Domain.Entities;
// using MainService.Domain.Enums;
// using MainService.Domain.Interfaces; 
// using TaskFlow.UserService;
// using FluentValidation;
// using FluentValidation.Results;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
// using AutoMapper;
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;

// namespace MainService.Tests.Controllers
// {
//     public class UserControllerTests
//     {
//         private readonly Mock<IValidator<CreateUserReq>> _createUserValidatorMock;
//         private readonly Mock<IValidator<LoginUserReq>> _loginUserValidatorMock;
//         private readonly Mock<IValidator<UpdateUserReq>> _updateUserValidatorMock;
//         private readonly Mock<IValidator<RegisterUserReq>> _registerUserValidatorMock;
//         private readonly Mock<IValidator<ChangePasswordReq>> _changePasswordValidatorMock;
//         private readonly Mock<UserUseCase> _userUseCaseMock;
//         private readonly Mock<ILogger<UserController>> _loggerMock;
//         private readonly Mock<IConfiguration> _configurationMock;
//         private readonly Mock<IMapper> _mapperMock;
//         private readonly UserController _controller;

//         public UserControllerTests()
//         {
//             _createUserValidatorMock = new Mock<IValidator<CreateUserReq>>();
//             _loginUserValidatorMock = new Mock<IValidator<LoginUserReq>>();
//             _updateUserValidatorMock = new Mock<IValidator<UpdateUserReq>>();
//             _registerUserValidatorMock = new Mock<IValidator<RegisterUserReq>>();
//             _changePasswordValidatorMock = new Mock<IValidator<ChangePasswordReq>>();
//             _userUseCaseMock = new Mock<UserUseCase>();
//             _loggerMock = new Mock<ILogger<UserController>>();
//             _configurationMock = new Mock<IConfiguration>();
//             _mapperMock = new Mock<IMapper>();

//             // Setup JWT secret
//             _configurationMock.Setup(x => x["JWT_SECRET"])
//                 .Returns("-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC...\n-----END PRIVATE KEY-----");

//             _controller = new UserController(
//                 _createUserValidatorMock.Object,
//                 _loginUserValidatorMock.Object,
//                 _updateUserValidatorMock.Object,
//                 _registerUserValidatorMock.Object,
//                 _changePasswordValidatorMock.Object,
//                 _userUseCaseMock.Object,
//                 _loggerMock.Object,
//                 _configurationMock.Object,
//                 _mapperMock.Object
//             );
//         }

//         #region Register Tests

//         [Fact]
//         public async Task Register_WithValidData_ShouldReturnSuccess()
//         {
//             // Arrange
//             var request = new RegisterUserReq
//             {
//                 FirstName = "John",
//                 LastName = "Doe",
//                 Email = "john@example.com",
//                 Password = "ValidPass123"
//             };

//             var user = new UserDomain
//             {
//                 Id = "user123",
//                 FirstName = request.FirstName,
//                 LastName = request.LastName,
//                 Email = request.Email,
//                 IsVerified = false
//             };

//           var userRes = new TaskFlow.UserService.UserRes  
//             {
//                 Id = user.Id,
//                 FirstName = user.FirstName,
//                 LastName = user.LastName,
//                 Email = user.Email
//             };

//             _registerUserValidatorMock
//                 .Setup(x => x.ValidateAsync(request, default))
//                 .ReturnsAsync(new ValidationResult());

//             _userUseCaseMock
//                 .Setup(x => x.CreateUser(It.IsAny<UserDomain>()))
//                 .ReturnsAsync(user);

//             _mapperMock
//                 .Setup(x => x.Map<UserRes>(It.IsAny<UserDomain>()))
//                 .Returns(userRes);

//             var context = TestServerCallContext.Create();

//             // Act
//             var result = await _controller.Register(request, context);

//             // Assert
//             result.Should().NotBeNull();
//             result.Status.Should().Be("success");
//             result.Message.Should().Contain("Send mail verify successfully");
//         }

//         [Fact]
//         public async Task Register_WithInvalidData_ShouldThrowValidationException()
//         {
//             // Arrange
//             var request = new RegisterUserReq
//             {
//                 FirstName = "",
//                 LastName = "",
//                 Email = "invalid-email",
//                 Password = "123",
//                 ConfirmPassword = "456"
//             };

//             var validationFailures = new List<ValidationFailure>
//             {
//                 new ValidationFailure("Email", "Invalid email format"),
//                 new ValidationFailure("Password", "Password must be at least 8 characters")
//             };

//             _registerUserValidatorMock
//                 .Setup(x => x.ValidateAsync(request, default))
//                 .ReturnsAsync(new ValidationResult(validationFailures));

//             var context = TestServerCallContext.Create();

//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<RpcException>(() =>
//                 _controller.Register(request, context));

//             exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
//             exception.Status.Detail.Should().Contain("Invalid email format");
//         }

//         #endregion

//         #region Login Tests

//         [Fact]
//         public async Task Login_WithValidCredentials_ShouldReturnSuccessAndSetCookie()
//         {
//             // Arrange
//             var request = new LoginUserReq
//             {
//                 Email = "user@test.com",
//                 Password = "ValidPass123"
//             };

//             var user = new UserDomain
//             {
//                 Id = "user123",
//                 Email = request.Email,
//                 IsVerified = true,
//                 Role = UserRole.User
//             };

//                var userRes = new TaskFlow.UserService.UserRes  
//             {
//                 Id = user.Id,
//                 Email = user.Email
//             };

//             _loginUserValidatorMock
//                 .Setup(x => x.ValidateAsync(request, default))
//                 .ReturnsAsync(new ValidationResult());

//             _userUseCaseMock
//                 .Setup(x => x.LoginUser(It.IsAny<LoginReqParams>()))
//                 .ReturnsAsync(user);

//             _mapperMock
//                 .Setup(x => x.Map<UserRes>(It.IsAny<UserDomain>()))
//                 .Returns(userRes);

//             var context = TestServerCallContext.Create();

//             // Act
//             var result = await _controller.Login(request, context);

//             // Assert
//             result.Should().NotBeNull();
//             result.Status.Should().Be("success");
//             result.Message.Should().Contain("Login successfully");
//             result.Data.Should().NotBeNull();
//             result.Data.Id.Should().Be(user.Id);
//         }

//         [Fact]
//         public async Task Login_WithUnverifiedAccount_ShouldThrowException()
//         {
//             // Arrange
//             var request = new LoginUserReq
//             {
//                 Email = "unverified@test.com",
//                 Password = "ValidPass123"
//             };

//             _loginUserValidatorMock
//                 .Setup(x => x.ValidateAsync(request, default))
//                 .ReturnsAsync(new ValidationResult());

//             _userUseCaseMock
//                 .Setup(x => x.LoginUser(It.IsAny<LoginReqParams>()))
//                 .ThrowsAsync(new RpcException(new Status(StatusCode.Unauthenticated, 
//                     "Account not verified. Please verify OTP!")));

//             var context = TestServerCallContext.Create();

//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<RpcException>(() =>
//                 _controller.Login(request, context));

//             exception.StatusCode.Should().Be(StatusCode.Unauthenticated);
//             exception.Status.Detail.Should().Contain("Account not verified");
//         }

//         #endregion

//         #region VerifyOTP Tests

//         [Fact]
//         public async Task VerifyOTP_WithValidOTP_ShouldReturnSuccess()
//         {
//             // Arrange
//             var request = new VerifyOTPReq
//             {
//                 Email = "user@test.com",
//                 Otp = "123456"
//             };

//             var user = new UserDomain
//             {
//                 Id = "user123",
//                 Email = request.Email,
//                 IsVerified = true
//             };

//                   var userRes = new TaskFlow.UserService.UserRes  
//             {
//                 Id = user.Id,
//                 Email = user.Email
//             };

//             _userUseCaseMock
//                 .Setup(x => x.VerifyUser(request.Otp, request.Email))
//                 .ReturnsAsync(user);

//             _mapperMock
//                 .Setup(x => x.Map<UserRes>(It.IsAny<UserDomain>()))
//                 .Returns(userRes);

//             var context = TestServerCallContext.Create();

//             // Act
//             var result = await _controller.VerifyOTP(request, context);

//             // Assert
//             result.Should().NotBeNull();
//             result.Status.Should().Be("success");
//             result.Data.Should().NotBeNull();
//             result.Data.Id.Should().Be(user.Id);
//         }

//         [Fact]
//         public async Task VerifyOTP_WithInvalidOTP_ShouldThrowException()
//         {
//             // Arrange
//             var request = new VerifyOTPReq
//             {
//                 Email = "user@test.com",
//                 Otp = "999999"
//             };

//             _userUseCaseMock
//                 .Setup(x => x.VerifyUser(request.Otp, request.Email))
//                 .ThrowsAsync(new RpcException(new Status(StatusCode.InvalidArgument, 
//                     "Invalid OTP code")));

//             var context = TestServerCallContext.Create();

//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<RpcException>(() =>
//                 _controller.VerifyOTP(request, context));

//             exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
//             exception.Status.Detail.Should().Contain("Invalid OTP code");
//         }

//         #endregion

//         #region ResendOTP Tests

//         [Fact]
//         public async Task ResendOTP_WithValidEmail_ShouldReturnSuccess()
//         {
//             // Arrange
//             var request = new ResendOTPReq
//             {
//                 Email = "user@test.com"
//             };

//             _userUseCaseMock
//                 .Setup(x => x.ResendOTP(request.Email))
//                 .ReturnsAsync(true);

//             var context = TestServerCallContext.Create();

//             // Act
//             var result = await _controller.ResendOTP(request, context);

//             // Assert
//             result.Should().NotBeNull();
//             result.Status.Should().Be("success");
//             result.Message.Should().Contain("Resend mail verify successfully");
//         }

//         #endregion

//         #region GetMe Tests

//         [Fact]
//         public async Task GetMe_WithAuthenticatedUser_ShouldReturnUserData()
//         {
//             // Arrange
//             var userId = "user123";
//             var user = new UserDomain
//             {
//                 Id = userId,
//                 Email = "user@test.com",
//                 FirstName = "John",
//                 LastName = "Doe"
//             };

//                   var userRes = new TaskFlow.UserService.UserRes  
//             {
//                 Id = user.Id,
//                 Email = user.Email,
//                 FirstName = user.FirstName,
//                 LastName = user.LastName
//             };

//             _userUseCaseMock
//                 .Setup(x => x.GetById(userId))
//                 .ReturnsAsync(user);

//             _mapperMock
//                 .Setup(x => x.Map<UserRes>(It.IsAny<UserDomain>()))
//                 .Returns(userRes);

//             var context = TestServerCallContext.Create();
//             context.UserState["UserId"] = userId;

//             // Act
//             var result = await _controller.GetMe(new GetUserReq(), context);

//             // Assert
//             result.Should().NotBeNull();
//             result.Status.Should().Be("User fetched successfully");
//             result.Data.Should().NotBeNull();
//             result.Data.Id.Should().Be(userId);
//         }

//         [Fact]
//         public async Task GetMe_WithoutAuthentication_ShouldThrowException()
//         {
//             // Arrange
//             var context = TestServerCallContext.Create();
//             // No UserId in UserState

//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<RpcException>(() =>
//                 _controller.GetMe(new GetUserReq(), context));

//             exception.StatusCode.Should().Be(StatusCode.Unauthenticated);
//             exception.Status.Detail.Should().Contain("User is not authenticated");
//         }

//         #endregion

//         #region UpdateUser Tests

//         [Fact]
//         public async Task UpdateUser_WithValidData_ShouldReturnSuccess()
//         {
//             // Arrange
//             var userId = "user123";
//             var request = new UpdateUserReq
//             {
//                 FirstName = "Jane",
//                 LastName = "Smith",
//                 Avatar = "avatar.jpg"
//             };

//             var updatedUser = new UserDomain
//             {
//                 Id = userId,
//                 FirstName = request.FirstName,
//                 LastName = request.LastName,
//                 Avatar = request.Avatar
//             };

//                   var userRes = new TaskFlow.UserService.UserRes  
//             {
//                 Id = updatedUser.Id,
//                 FirstName = updatedUser.FirstName,
//                 LastName = updatedUser.LastName
//             };

//             _updateUserValidatorMock
//                 .Setup(x => x.ValidateAsync(request, default))
//                 .ReturnsAsync(new ValidationResult());

//             _userUseCaseMock
//                 .Setup(x => x.UpdateUserAsync(It.IsAny<UpdateUserParams>()))
//                 .ReturnsAsync(updatedUser);

//             _mapperMock
//                 .Setup(x => x.Map<UserRes>(It.IsAny<UserDomain>()))
//                 .Returns(userRes);

//             var context = TestServerCallContext.Create();
//             context.UserState["UserId"] = userId;

//             // Act
//             var result = await _controller.UpdateUser(request, context);

//             // Assert
//             result.Should().NotBeNull();
//             result.Status.Should().Be("Updated successfully");
//             result.Data.Should().NotBeNull();
//             result.Data.FirstName.Should().Be(request.FirstName);
//         }

//         #endregion

//         #region ChangePassword Tests

//         [Fact]
//         public async Task ChangePassword_WithValidData_ShouldReturnSuccess()
//         {
//             // Arrange
//             var userId = "user123";
//             var request = new ChangePasswordReq
//             {
//                 UserId = userId,
//                 OldPassword = "OldPass123",
//                 NewPassword = "NewPass123",
//                 ConfirmNewPassword = "NewPass123"
//             };

//             _changePasswordValidatorMock
//                 .Setup(x => x.ValidateAsync(request, default))
//                 .ReturnsAsync(new ValidationResult());

//             _userUseCaseMock
//                 .Setup(x => x.ChangePassword(request.UserId, request.OldPassword, request.NewPassword))
//                 .Returns(Task.CompletedTask);

//             var context = TestServerCallContext.Create();
//             context.UserState["UserId"] = userId;

//             // Act
//             var result = await _controller.ChangePassword(request, context);

//             // Assert
//             result.Should().NotBeNull();
//             result.Status.Should().Be("success");
//             result.Message.Should().Contain("Password changed successfully");
//         }

//         [Fact]
//         public async Task ChangePassword_WithMismatchedUserId_ShouldThrowException()
//         {
//             // Arrange
//             var authenticatedUserId = "user123";
//             var request = new ChangePasswordReq
//             {
//                 UserId = "otherUser456", // Different user
//                 OldPassword = "OldPass123",
//                 NewPassword = "NewPass123"
//             };

//             var context = TestServerCallContext.Create();
//             context.UserState["UserId"] = authenticatedUserId;

//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<RpcException>(() =>
//                 _controller.ChangePassword(request, context));

//             exception.StatusCode.Should().Be(StatusCode.PermissionDenied);
//             exception.Status.Detail.Should().Contain("Cannot change another user's password");
//         }

//         #endregion

//         #region Logout Tests

//         [Fact]
//         public async Task Logout_ShouldClearCookieAndReturnSuccess()
//         {
//             // Arrange
//             var userId = "user123";
//             var context = TestServerCallContext.Create();
//             context.UserState["UserId"] = userId;

//             // Act
//             var result = await _controller.Logout(new LogoutUserReq(), context);

//             // Assert
//             result.Should().NotBeNull();
//             result.Status.Should().Be("success");
//             result.Message.Should().Contain("Logged out successfully");
//         }

//         #endregion
//     }

//     // Helper class to create test ServerCallContext
//     public static class TestServerCallContext
//     {
//         public static ServerCallContext Create()
//         {
//             var context = new Mock<ServerCallContext>();
//             var userState = new Dictionary<object, object>();
            
//             context.Setup(x => x.UserState).Returns(userState);
//             context.Setup(x => x.WriteResponseHeadersAsync(It.IsAny<Metadata>()))
//                 .Returns(Task.CompletedTask);
            
//             return context.Object;
//         }
//     }
// }