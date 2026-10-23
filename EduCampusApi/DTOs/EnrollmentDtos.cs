using System.ComponentModel.DataAnnotations;

namespace module1_4.DTOs;

public class EnrollmentCreatePayload
{
    [Required(ErrorMessage = "идентификатор студента обязателен")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "идентификатор курса обязателен")]
    public int CourseId { get; set; }
}

public class EnrollmentGradePatchPayload
{
    [Range(0, 100, ErrorMessage = "оценка должна находиться в диапазоне от 0 до 100 баллов")]
    public double? Grade { get; set; }
}

public class EnrollmentBriefDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public double? Grade { get; set; }
}