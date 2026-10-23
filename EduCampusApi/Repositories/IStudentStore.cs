using module1_4.Models;

namespace module1_4.Repositories;

public interface IStudentStore
{
    Task<IReadOnlyList<StudentEntry>> GetAllAsync();
    Task<StudentEntry?> GetByIdAsync(int studentId);
    Task<StudentEntry?> GetWithEnrollmentsAsync(int studentId);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
    Task<StudentEntry> AddAsync(StudentEntry student);
    Task<StudentEntry> UpdateAsync(StudentEntry student);
    Task<bool> DeleteAsync(int studentId);
}