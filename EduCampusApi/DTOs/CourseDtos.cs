using System.ComponentModel.DataAnnotations;

namespace module1_4.DTOs;

public class CourseCreatePayload
{
    [Required(ErrorMessage = "наименование курса обязательно")]
    [MaxLength(160, ErrorMessage = "наименование курса не может превышать 160 символов")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(600, ErrorMessage = "описание не может превышать 600 символов")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 60, ErrorMessage = "количество кредитов должно находиться в диапазоне от 1 до 60")]
    public int Credits { get; set; }

    [Required(ErrorMessage = "идентификатор преподавателя обязателен")]
    public int TeacherId { get; set; }
}

public class CourseUpdatePayload
{
    [Required(ErrorMessage = "наименование курса обязательно")]
    [MaxLength(160, ErrorMessage = "наименование курса не может превышать 160 символов")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(600, ErrorMessage = "описание не может превышать 600 символов")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 60, ErrorMessage = "количество кредитов должно находиться в диапазоне от 1 до 60")]
    public int Credits { get; set; }

    [Required(ErrorMessage = "идентификатор преподавателя обязателен")]
    public int TeacherId { get; set; }
}

public class CourseBriefDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TeacherId { get; set; }
    public string TeacherFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CourseFilterQuery
{
    public string? NameQuery { get; set; }
    public int? MinCredits { get; set; }
    public int? MaxCredits { get; set; }
    public int? TeacherId { get; set; }
}