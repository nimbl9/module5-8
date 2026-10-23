using System.ComponentModel.DataAnnotations;

namespace module1_4.DTOs;

public class TeacherCreatePayload
{
    [Required(ErrorMessage = "имя преподавателя обязательно")]
    [MaxLength(80, ErrorMessage = "имя не может быть длиннее 80 символов")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "фамилия преподавателя обязательна")]
    [MaxLength(80, ErrorMessage = "фамилия не может быть длиннее 80 символов")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "электронная почта обязательна")]
    [EmailAddress(ErrorMessage = "некорректный формат адреса электронной почты")]
    [MaxLength(120, ErrorMessage = "почта не может превышать 120 символов")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "департамент обязателен")]
    [MaxLength(150, ErrorMessage = "название кафедры/департамента не может превышать 150 символов")]
    public string Department { get; set; } = string.Empty;
}

public class TeacherUpdatePayload
{
    [Required(ErrorMessage = "имя преподавателя обязательно")]
    [MaxLength(80, ErrorMessage = "имя не может быть длиннее 80 символов")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "фамилия преподавателя обязательна")]
    [MaxLength(80, ErrorMessage = "фамилия не может быть длиннее 80 символов")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "электронная почта обязательна")]
    [EmailAddress(ErrorMessage = "некорректный формат адреса электронной почты")]
    [MaxLength(120, ErrorMessage = "почта не может превышать 120 символов")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "департамент обязателен")]
    [MaxLength(150, ErrorMessage = "название кафедры/департамента не может превышать 150 символов")]
    public string Department { get; set; } = string.Empty;
}

public class TeacherBriefDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}