namespace EduCampusClient.Models;

public class ApiEnvelope<T>
{
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public T? Result { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TraceId { get; set; }
}

public class CourseListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TeacherId { get; set; }
    public string TeacherFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CourseDetailItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TeacherId { get; set; }
    public string TeacherFullName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public string TeacherDepartment { get; set; } = string.Empty;
    public int EnrolledStudentsCount { get; set; }
    public double? AverageGrade { get; set; }
    public DateTime CreatedAt { get; set; }
}