using Grpc.Core;
using MainService.Domain.Entities;
using MainService.Domain.Interfaces;
using MainService.Domain.Packages;
using TaskFlow.UserService;

namespace MainService.Domain.UseCases;

public class UserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly OtpTokenUseCase _otpTokenUseCase;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ILogger<UserUseCase> _logger;
    private readonly IPublisherService _publisher;

    public UserUseCase(OtpTokenUseCase otpTokenUseCase, IUserRepository userRepository, IIssueRepository issueRepository, ILogger<UserUseCase> logger, IPublisherService publisher)
    {
        _userRepository = userRepository;
        _issueRepository = issueRepository;
        _otpTokenUseCase = otpTokenUseCase;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<UserDomain> CreateUser(UserDomain userBody)
    {
        userBody.Password = PasswordHasher.HashPassword(userBody.Password);
        var user = await _userRepository.CreateUserAsync(userBody);

        if (user.Id == null) throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        await _otpTokenUseCase.GenerateOtpAsync(user);

        return user;
    }

    public async Task<UserDomain> VerifyUser(string otp, string email)
    {
        var user = await _userRepository.FindUserAsync(new UserQueryParams
        {
            Email = email,
        });

        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
        }

        var result = await _otpTokenUseCase.VerifyOtpAsync(user, otp);

        if (result != OtpVerifyResult.Success)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid OTP code"));
        }

        await _userRepository.UpdateUserAsync(new UpdateUserParams
        {
            Id = user.Id,
            IsVerified = true,
            ExpiredAt = DateTime.MaxValue,
        });

        return user;
    }

    public async Task<bool> ResendOTP(string email)
    {
        var user = await _userRepository.FindUserAsync(new UserQueryParams
        {
            Email = email,
        });

        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
        }

        var isSuccess = await _otpTokenUseCase.ResendOtpAsync(user.Id);
        return isSuccess;
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

        if (user.IsVerified == false)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Account not verified. Please verify OTP!"));

        // if (!PasswordHasher.ValidatePassword(param.Password, user.Password))
        //     throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid password!"));

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
    public async Task<UserDomain> UpdateUserAsync(UpdateUserParams param)
    {
        if (param.Id == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "User ID invalid!"));
        }

        return await _userRepository.UpdateUserAsync(param);
    }
    public async Task<UserStats> GetStats(string id, bool isSprintId)
    {
        return await _issueRepository.GetStats(id, isSprintId);
    }
    public async Task<(IEnumerable<UserDomain> Users, int TotalCount)> SearchUsersAsync(SearchUserQueryParams param)
    {
        var users = await _userRepository.SearchUsersAsync(param);
        return users;
    }

    public async Task ChangePassword(string userId, string oldPassword, string newPassword)
    {
        var user = await _userRepository.FindUserAsync(new UserQueryParams
        {
            UserId = userId
        });

        if (user == null || string.IsNullOrWhiteSpace(user.Id))
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        if (!PasswordHasher.ValidatePassword(oldPassword, user.Password))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Current password is incorrect"));

        var newPasswordHashed = PasswordHasher.HashPassword(newPassword);

        await _userRepository.UpdateUserAsync(new UpdateUserParams
        {
            Id = user.Id,
            Password = newPasswordHashed,
        });
    }
}
