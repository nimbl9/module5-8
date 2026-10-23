using System.ComponentModel.DataAnnotations;

namespace module1_4.DTOs;

public class StudentCreatePayload
{
    [Required(ErrorMessage = "имя студента обязательно")]
    [MaxLength(80, ErrorMessage = "имя не может быть длиннее 80 символов")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "фамилия студента обязательна")]
    [MaxLength(80, ErrorMessage = "фамилия не может быть длиннее 80 символов")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "электронная почта обязательна")]
    [EmailAddress(ErrorMessage = "некорректный формат адреса электронной почты")]
    [MaxLength(120, ErrorMessage = "почта не может превышать 120 символов")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "дата рождения обязательна")]
    public DateTime BirthDate { get; set; }
}

public class StudentUpdatePayload
{
    [Required(ErrorMessage = "имя студента обязательно")]
    [MaxLength(80, ErrorMessage = "имя не может быть длиннее 80 символов")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "фамилия студента обязательна")]
    [MaxLength(80, ErrorMessage = "фамилия не может быть длиннее 80 символов")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "электронная почта обязательна")]
    [EmailAddress(ErrorMessage = "некорректный формат адреса электронной почты")]
    [MaxLength(120, ErrorMessage = "почта не может превышать 120 символов")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "дата рождения обязательна")]
    public DateTime BirthDate { get; set; }
}

public class StudentBriefDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentDetailedDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StudentCourseRecordDto> EnrolledCourses { get; set; } = new();
}

public class StudentCourseRecordDto
{
    public int EnrollmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public double? Grade { get; set; }
}