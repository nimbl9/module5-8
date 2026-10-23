using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using module1_4.Common;
using module1_4.DTOs;
using module1_4.Models;
using module1_4.Services;

namespace module1_4.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthFlowController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthFlowController> _logger;

    public AuthFlowController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        RoleManager<AppRole> roleManager,
        IJwtTokenService jwtTokenService,
        ILogger<AuthFlowController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    private string CurrentTraceId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponsePacket<TokenResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<TokenResultDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginPayload payload)
    {
        var user = await _userManager.FindByNameAsync(payload.UserNameOrEmail)
                   ?? await _userManager.FindByEmailAsync(payload.UserNameOrEmail);

        if (user == null)
        {
            _logger.LogWarning("попытка входа с несуществующим логином: {Login}", payload.UserNameOrEmail);
            return Unauthorized(ApiResponsePacket<TokenResultDto>.Failure(
                StatusCodes.Status401Unauthorized,
                "INVALID_CREDENTIALS",
                "неверное имя пользователя или пароль",
                CurrentTraceId));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("учетная запись пользователя {UserName} временно заблокирована", user.UserName);
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponsePacket<TokenResultDto>.Failure(
                StatusCodes.Status403Forbidden,
                "USER_LOCKED_OUT",
                "учетная запись временно заблокирована из-за множества неудачных попыток входа",
                CurrentTraceId));
        }

        var checkPassword = await _signInManager.CheckPasswordSignInAsync(user, payload.Password, lockoutOnFailure: true);
        if (!checkPassword.Succeeded)
        {
            _logger.LogWarning("пользователь {UserName} ввел неверный пароль", user.UserName);
            return Unauthorized(ApiResponsePacket<TokenResultDto>.Failure(
                StatusCodes.Status401Unauthorized,
                "INVALID_CREDENTIALS",
                "неверное имя пользователя или пароль",
                CurrentTraceId));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expires) = _jwtTokenService.GenerateToken(user, roles);

        var resultDto = new TokenResultDto
        {
            AccessToken = token,
            ExpiresAtUtc = expires,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles
        };

        _logger.LogInformation("пользователь {UserName} успешно аутентифицирован и получил jwt-токен", user.UserName);
        return Ok(ApiResponsePacket<TokenResultDto>.Success(resultDto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponsePacket<CurrentUserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponsePacket<CurrentUserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterPayload payload)
    {
        var existingUserByName = await _userManager.FindByNameAsync(payload.UserName);
        if (existingUserByName != null)
        {
            _logger.LogWarning("попытка регистрации с уже занятым именем пользователя {UserName}", payload.UserName);
            return BadRequest(ApiResponsePacket<CurrentUserDto>.Failure(
                StatusCodes.Status400BadRequest,
                "USERNAME_DUPLICATE",
                "пользователь с таким именем уже существует",
                CurrentTraceId));
        }

        var existingUserByEmail = await _userManager.FindByEmailAsync(payload.Email);
        if (existingUserByEmail != null)
        {
            _logger.LogWarning("попытка регистрации с уже занятым email {Email}", payload.Email);
            return BadRequest(ApiResponsePacket<CurrentUserDto>.Failure(
                StatusCodes.Status400BadRequest,
                "EMAIL_DUPLICATE",
                "пользователь с такой почтой уже существует",
                CurrentTraceId));
        }

        var newUser = new AppUser
        {
            UserName = payload.UserName,
            Email = payload.Email,
            FullName = payload.FullName,
            StudentId = payload.StudentId,
            TeacherId = payload.TeacherId
        };

        var creationResult = await _userManager.CreateAsync(newUser, payload.Password);
        if (!creationResult.Succeeded)
        {
            var errors = string.Join("; ", creationResult.Errors.Select(e => e.Description));
            _logger.LogWarning("ошибка при регистрации пользователя {UserName}: {Errors}", payload.UserName, errors);
            return BadRequest(ApiResponsePacket<CurrentUserDto>.Failure(
                StatusCodes.Status400BadRequest,
                "USER_CREATION_FAILED",
                errors,
                CurrentTraceId));
        }

        await _userManager.AddToRoleAsync(newUser, "Student");

        var roles = await _userManager.GetRolesAsync(newUser);
        var resultDto = new CurrentUserDto
        {
            Id = newUser.Id,
            UserName = newUser.UserName,
            Email = newUser.Email,
            FullName = newUser.FullName,
            Roles = roles,
            StudentId = newUser.StudentId,
            TeacherId = newUser.TeacherId
        };

        _logger.LogInformation("зарегистрирован новый пользователь {UserName} с ролью student", newUser.UserName);
        return StatusCode(StatusCodes.Status201Created, ApiResponsePacket<CurrentUserDto>.Success(resultDto, StatusCodes.Status201Created, CurrentTraceId));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponsePacket<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<CurrentUserDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(ApiResponsePacket<CurrentUserDto>.Failure(
                StatusCodes.Status401Unauthorized,
                "UNAUTHORIZED",
                "пользователь не авторизован",
                CurrentTraceId));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound(ApiResponsePacket<CurrentUserDto>.Failure(
                StatusCodes.Status404NotFound,
                "USER_NOT_FOUND",
                "пользователь не найден",
                CurrentTraceId));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var dto = new CurrentUserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles,
            StudentId = user.StudentId,
            TeacherId = user.TeacherId
        };

        _logger.LogInformation("пользователь {UserName} запросил сведения о своей учетной записи", user.UserName);
        return Ok(ApiResponsePacket<CurrentUserDto>.Success(dto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRole([FromBody] RoleAssignPayload payload)
    {
        var targetUser = await _userManager.FindByNameAsync(payload.UserName);
        if (targetUser == null)
        {
            _logger.LogWarning("попытка назначения роли несуществующему пользователю {UserName}", payload.UserName);
            return NotFound(ApiResponsePacket<bool>.Failure(
                StatusCodes.Status404NotFound,
                "USER_NOT_FOUND",
                $"пользователь {payload.UserName} не найден",
                CurrentTraceId));
        }

        var roleExists = await _roleManager.RoleExistsAsync(payload.RoleName);
        if (!roleExists)
        {
            _logger.LogWarning("попытка назначить несуществующую роль {RoleName}", payload.RoleName);
            return BadRequest(ApiResponsePacket<bool>.Failure(
                StatusCodes.Status400BadRequest,
                "ROLE_NOT_FOUND",
                $"роль {payload.RoleName} не существует",
                CurrentTraceId));
        }

        var alreadyInRole = await _userManager.IsInRoleAsync(targetUser, payload.RoleName);
        if (!alreadyInRole)
        {
            await _userManager.AddToRoleAsync(targetUser, payload.RoleName);
        }

        _logger.LogInformation("администратор успешно назначил роль {RoleName} пользователю {UserName}", payload.RoleName, payload.UserName);
        return Ok(ApiResponsePacket<bool>.Success(true, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpGet("expired-sample-token")]
    [AllowAnonymous]
    public async Task<IActionResult> GetExpiredTokenSample()
    {
        var user = await _userManager.FindByNameAsync("admin") ?? await _userManager.Users.FirstOrDefaultAsync();
        if (user == null)
        {
            return BadRequest(ApiResponsePacket<string>.Failure(
                StatusCodes.Status400BadRequest,
                "NO_USERS",
                "пользователи отсутствуют в системе",
                CurrentTraceId));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var expiredToken = _jwtTokenService.GenerateExpiredToken(user, roles);

        _logger.LogInformation("сгенерирован тестовый просроченный токен для проверки отклонения запросов");
        return Ok(ApiResponsePacket<string>.Success(expiredToken, StatusCodes.Status200OK, CurrentTraceId));
    }
}