using module1_4.Models;

namespace module1_4.Repositories;

public interface IEnrollmentStore
{
    Task<IReadOnlyList<EnrollmentEntry>> GetAllAsync();
    Task<EnrollmentEntry?> GetByIdAsync(int enrollmentId);
    Task<bool> ExistsAsync(int studentId, int courseId);
    Task<EnrollmentEntry> AddAsync(EnrollmentEntry enrollment);
    Task<EnrollmentEntry?> UpdateGradeAsync(int enrollmentId, double? grade);
    Task<bool> DeleteAsync(int enrollmentId);
}