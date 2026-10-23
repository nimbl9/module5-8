using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace module1_4.Models;

[Table("Enrollments")]
public class EnrollmentEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int StudentId { get; set; }

    [ForeignKey(nameof(StudentId))]
    public StudentEntry? Student { get; set; }

    public int CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public CourseEntry? Course { get; set; }

    [Required]
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

    [Range(0, 100)]
    public double? Grade { get; set; }
}