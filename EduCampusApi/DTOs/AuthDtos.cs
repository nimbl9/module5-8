using System.ComponentModel.DataAnnotations;

namespace module1_4.DTOs;

public class LoginPayload
{
    [Required(ErrorMessage = "логин или email обязателен")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "пароль обязателен")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterPayload
{
    [Required(ErrorMessage = "имя пользователя обязательно")]
    [MaxLength(60)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "электронная почта обязательна")]
    [EmailAddress(ErrorMessage = "некорректный адрес электронной почты")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "пароль обязателен")]
    [MinLength(6, ErrorMessage = "пароль должен быть длиной не менее 6 символов")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "полное имя обязательно")]
    public string FullName { get; set; } = string.Empty;

    public int? StudentId { get; set; }
    public int? TeacherId { get; set; }
}

public class TokenResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAtUtc { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class CurrentUserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public int? StudentId { get; set; }
    public int? TeacherId { get; set; }
}

public class RoleAssignPayload
{
    [Required(ErrorMessage = "имя пользователя обязательно")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "роль обязательна")]
    public string RoleName { get; set; } = string.Empty;
}