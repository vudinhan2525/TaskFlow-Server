using FluentValidation;
using FluentValidation.Results;
using Grpc.Core;
using MainService.Domain.Entities;
using MainService.Domain.Enums;
using MainService.Domain.UseCases;
using TaskFlow.UserService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;
using AutoMapper;
using MainService.Domain.Interfaces;
using Google.Protobuf.WellKnownTypes;
using System.Text.Json;

public class UserController : UserService.UserServiceBase
{
    private readonly IValidator<CreateUserReq> _createUserValidator;
    private readonly IValidator<LoginUserReq> _loginUserValidator;
    private readonly IValidator<UpdateUserReq> _updateUserValidator;
    private readonly IValidator<RegisterUserReq> _registerUserValidator;
    private readonly IValidator<ChangePasswordReq> _changePasswordValidator;
    private readonly IMapper _mapper;
    private readonly UserUseCase _userUseCase;
    private readonly ILogger<UserController> _logger;
    private readonly IConfiguration _configuration;

    public UserController(
        IValidator<CreateUserReq> createUserValidator,
        IValidator<LoginUserReq> loginUserValidator,
        IValidator<UpdateUserReq> updateUserValidator,
        IValidator<RegisterUserReq> registerUserValidator,
        IValidator<ChangePasswordReq> changePasswordValidator,
        UserUseCase userUseCase,
        ILogger<UserController> logger,
        IConfiguration configuration,
        IMapper mapper)
    {
        _createUserValidator = createUserValidator ?? throw new ArgumentNullException(nameof(createUserValidator));
        _loginUserValidator = loginUserValidator ?? throw new ArgumentNullException(nameof(loginUserValidator));
        _updateUserValidator = updateUserValidator ?? throw new ArgumentNullException(nameof(updateUserValidator));
        _registerUserValidator = registerUserValidator ?? throw new ArgumentNullException(nameof(registerUserValidator));
        _changePasswordValidator = changePasswordValidator ?? throw new ArgumentNullException(nameof(changePasswordValidator));
        _userUseCase = userUseCase ?? throw new ArgumentNullException(nameof(userUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public override async Task<UpdateUserRes> UpdateUser(UpdateUserReq request, ServerCallContext context)
    {
        var validationResult = await _updateUserValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var updatedUser = await _userUseCase.UpdateUser(new UserDomain
        {
            Id = request.UserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = request.Password,
        });

        var userResponse = _mapper.Map<UserRes>(updatedUser);

        return new UpdateUserRes
        {
            Status = "Updated successfully",
            Data = userResponse
        };
    }

    public override async Task<CreateUserRes> CreateUser(CreateUserReq request, ServerCallContext context)
    {
        var validationResult = await _createUserValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var user = await _userUseCase.CreateUser(new UserDomain
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = request.Password,
            Role = System.Enum.TryParse(request.Role, out UserRole parsedRole) ? parsedRole : UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var userResponse = _mapper.Map<UserRes>(user);
        return new CreateUserRes
        {
            Status = "User created successfully",
            Data = userResponse
        };
    }

    public override async Task<GetUserRes> GetMe(GetUserReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;

        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User is not authenticated."));
        }

        var user = await _userUseCase.GetById(userId);
        var userResponse = _mapper.Map<UserRes>(user);

        return new GetUserRes
        {
            Status = "User fetched successfully",
            Data = userResponse
        };
    }

    public override async Task<GetUserRes> GetById(GetUserReq request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.UserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "User ID is required."));
        }

        var user = await _userUseCase.GetById(request.UserId);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
        }

        var userResponse = _mapper.Map<UserRes>(user);

        return new GetUserRes
        {
            Status = "User fetched successfully",
            Data = userResponse
        };
    }

    public override async Task<CreateUserRes> Register(RegisterUserReq request, ServerCallContext context)
    {
        var validationResult = await _registerUserValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var user = await _userUseCase.CreateUser(new UserDomain
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = request.Password,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        if (user.Id == null) throw new Exception("Can't happen");
        await SetJwtToken(user.Id, user.Role, request.Email, context);

        var userResponse = _mapper.Map<UserRes>(user);
        return new CreateUserRes
        {
            Status = "User created successfully",
            Data = userResponse
        };
    }

    public override async Task<LoginUserRes> Login(LoginUserReq request, ServerCallContext context)
    {
        var validationResult = await _loginUserValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var user = await _userUseCase.LoginUser(new LoginReqParams
        {
            Email = request.Email,
            Password = request.Password
        });

        if (user.Id == null) throw new Exception("Can't happen");
        await SetJwtToken(user.Id, user.Role, request.Email, context);

        var userResponse = _mapper.Map<UserRes>(user);

        return new LoginUserRes
        {
            Status = "success",
            Message = "Login successfully",
            Data = userResponse
        };
    }

    public override async Task<ListUsersRes> ListUsers(ListUsersReq request, ServerCallContext context)
    {
        try
        {
            // Ensure valid pagination values
            var page = request.Page <= 0 ? 1 : request.Page;
            var limit = request.Limit <= 0 ? 10 : request.Limit;

            var result = await _userUseCase.SearchUsersAsync(
                request.Name,
                request.Email,
                (int)page,
                (int)limit
            );
            var users = result.Users;
            var totalCount = result.TotalCount;

            var response = new ListUsersRes
            {
                Status = "success",
                Pagination = new PaginationRes
                {
                    TotalItems = totalCount,
                    CurrentPage = page,
                    Limit = limit,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.Limit)
                }
            };

            response.Data.AddRange(users.Select(u => _mapper.Map<UserRes>(u)));
            return response;
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, "Error searching users"));
        }
    }

    public override async Task<GetUserRes> GetByEmail(GetUserByEmailReq request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email is required"));
            }

            _logger.LogInformation("Getting user by email: {Email}", request.Email);
            var user = await _userUseCase.FindUserAsync(new UserQueryParams { Email = request.Email });

            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"User with email {request.Email} not found"));
            }

            return new GetUserRes
            {
                Status = "success",
                Data = _mapper.Map<UserRes>(user)
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", request.Email);
            throw new RpcException(new Status(StatusCode.Internal, "Error retrieving user"));
        }
    }
    public override async Task<LogoutUserRes> Logout(LogoutUserReq request, ServerCallContext context)
    {
        var metadata = new Metadata
        {
            { "Set-Cookie", "token=; Path=/; HttpOnly; Secure; SameSite=Strict; Max-Age=0" }
        };
        await context.WriteResponseHeadersAsync(metadata);

        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} logged out successfully", userId);
        }

        return new LogoutUserRes
        {
            Status = "success",
            Message = "Logged out successfully"
        };
    }

    // TODO: Move to stat service
    public override async Task<GetStatsRes> GetStats(GetStatsReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;

        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User is not authenticated."));
        }
        
        var domainStats = await _userUseCase.GetStats(request.ProjectId);
        
        if (domainStats == null)
        {
            throw new RpcException(new Status(StatusCode.Internal, "Failed to retrieve stats."));
        }

        // Use the mapper to convert from domain model to response model
        var stats = _mapper.Map<TaskFlow.UserService.UserStats>(domainStats);

        return new GetStatsRes
        {
            Status = "success",
            Message = "Stats retrieved successfully",
            Data = stats
        };
    }

    public override async Task<ChangePasswordRes> ChangePassword(ChangePasswordReq request, ServerCallContext context)
    {
        var userId = context.UserState.ContainsKey("UserId") ? context.UserState["UserId"] as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User is not authenticated"));
        }

        // Ensure user can only change their own password
        if (userId != request.UserId)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Cannot change another user's password"));
        }

        var validationResult = await _changePasswordValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        try
        {
            await _userUseCase.ChangePassword(request.UserId, request.OldPassword, request.NewPassword);
            
            return new ChangePasswordRes
            {
                Status = "success",
                Message = "Password changed successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, "Error changing password"));
        }
    }

    private async Task SetJwtToken(string userId, UserRole role, string email, ServerCallContext context)
    {
        string? privateKeyPem = _configuration["JWT_SECRET"];
        if (string.IsNullOrEmpty(privateKeyPem))
        {
            throw new Exception("Private key not found in configuration.");
        }

        string jwtToken = CreateJwtToken(userId, role, email, privateKeyPem);

        var metadata = new Metadata
        {
            { "Set-Cookie", $"token={jwtToken}; Path=/; HttpOnly; Secure; SameSite=Strict" }
        };
        await context.WriteResponseHeadersAsync(metadata);
    }

    private static string CreateJwtToken(string userId, UserRole role, string email, string privateKeyPem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem.ToCharArray());

        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Name, email),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Iss, "127.0.0.1"),
            new Claim("role", role.ToString()),
            new Claim("userId", userId)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = "127.0.0.1",
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}