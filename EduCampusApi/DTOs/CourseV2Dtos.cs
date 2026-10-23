namespace module1_4.DTOs;

public class CourseV2BriefDto
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