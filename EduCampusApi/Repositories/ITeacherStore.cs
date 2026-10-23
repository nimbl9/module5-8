using module1_4.Models;

namespace module1_4.Repositories;

public interface ITeacherStore
{
    Task<IReadOnlyList<TeacherEntry>> GetAllAsync();
    Task<TeacherEntry?> GetByIdAsync(int teacherId);
    Task<bool> ExistsAsync(int teacherId);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
    Task<bool> HasAssignedCoursesAsync(int teacherId);
    Task<TeacherEntry> AddAsync(TeacherEntry teacher);
    Task<TeacherEntry> UpdateAsync(TeacherEntry teacher);
    Task<bool> DeleteAsync(int teacherId);
}