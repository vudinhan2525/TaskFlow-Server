using Grpc.Core;
using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Domain.Packages;
using Microsoft.Extensions.Logging;
using TaskFlow.UserService;

namespace MainService.Domain.UseCases;

public class UserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly ILogger<UserUseCase> _logger;

    public UserUseCase(IUserRepository userRepository, IIssueRepository issueRepository, ILogger<UserUseCase> logger)
    {
        _userRepository = userRepository;
        _issueRepository = issueRepository;
        _logger = logger;
    }

    public async Task<UserDomain> CreateUser(UserDomain userBody)
    {
        var user = await _userRepository.FindUserAsync(new UserQueryParams
        {
            Email = userBody.Email
        });

        if (user != null)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Email has been used!!"));
        }

        userBody.Password = PasswordHasher.HashPassword(userBody.Password);
        return await _userRepository.CreateUserAsync(userBody);
    }

    public async Task<UserDomain?> FindUserAsync(UserQueryParams queryParams)
    {
        var user = await _userRepository.FindUserAsync(queryParams);
        return user;
    }

    public async Task<UserDomain> LoginUser(LoginReqParams param)
    {
        var user = await _userRepository.FindUserAsync(new UserQueryParams
        {
            Email = param.Email
        });

        if (user == null)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found!"));


        if (!PasswordHasher.ValidatePassword(param.Password, user.Password))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid password!"));


        return user;
    }
    public async Task<UserDomain> GetById(string userId)
    {
        var user = await _userRepository.FindUserAsync(new UserQueryParams
        {
            UserId = userId
        });

        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found!"));
        }

        return user;
    }
    public async Task<UserDomain> UpdateUser(UserDomain user)
    {
        if (string.IsNullOrEmpty(user.Id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "userId not found!!"));
        }

        var existingUser = await _userRepository.FindUserAsync(new UserQueryParams
        {
            UserId = user.Id
        });

        if (existingUser == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User with ID {user.Id} not found"));


        if (!string.IsNullOrEmpty(user.FirstName))
            existingUser.FirstName = user.FirstName;

        if (!string.IsNullOrEmpty(user.LastName))
            existingUser.LastName = user.LastName;

        if (!string.IsNullOrEmpty(user.Email))
            existingUser.Email = user.Email;

        if (!string.IsNullOrEmpty(user.Password))
            existingUser.Password = PasswordHasher.HashPassword(user.Password);

        existingUser.UpdatedAt = DateTime.UtcNow;

        return await _userRepository.UpdateUser(existingUser);
    }
    public async Task<MainService.Domain.Interfaces.UserStats> GetStats(string projectId)
    {
        return await _issueRepository.GetStats(projectId);
    }
    public async Task<(IEnumerable<UserDomain> Users, int TotalCount)> SearchUsersAsync(string? name, string? email, int page, int limit)
    {
        try
        {
            // Normalize inputs with defaults
            page = Math.Max(1, page);
            limit = limit <= 0 ? 10 : limit;

            return await _userRepository.SearchUsersAsync(name, email, page, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with name: {Name}, email: {Email}", name, email);
            throw;
        }
    }

    public async Task ChangePassword(string userId, string oldPassword, string newPassword)
    {
        var user = await _userRepository.FindUserAsync(new UserQueryParams
        {
            UserId = userId
        });

        if (user == null)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        if (!PasswordHasher.ValidatePassword(oldPassword, user.Password))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Current password is incorrect"));

        user.Password = PasswordHasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateUser(user);
    }
}
