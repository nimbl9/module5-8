using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace module1_4.Models;

[Table("Courses")]
public class CourseEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(600)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 60)]
    public int Credits { get; set; }

    public int TeacherId { get; set; }

    [ForeignKey(nameof(TeacherId))]
    public TeacherEntry? Teacher { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EnrollmentEntry> Enrollments { get; set; } = new List<EnrollmentEntry>();
}