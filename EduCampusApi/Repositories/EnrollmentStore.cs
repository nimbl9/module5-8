using Microsoft.EntityFrameworkCore;
using module1_4.Data;
using module1_4.Models;

namespace module1_4.Repositories;

public class EnrollmentStore : IEnrollmentStore
{
    private readonly EduCampusContext _campusContext;

    public EnrollmentStore(EduCampusContext campusContext)
    {
        _campusContext = campusContext;
    }

    public async Task<IReadOnlyList<EnrollmentEntry>> GetAllAsync()
    {
        return await _campusContext.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .AsNoTracking()
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync();
    }

    public async Task<EnrollmentEntry?> GetByIdAsync(int enrollmentId)
    {
        return await _campusContext.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);
    }

    public async Task<bool> ExistsAsync(int studentId, int courseId)
    {
        return await _campusContext.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
    }

    public async Task<EnrollmentEntry> AddAsync(EnrollmentEntry enrollment)
    {
        enrollment.EnrollmentDate = DateTime.UtcNow;
        await _campusContext.Enrollments.AddAsync(enrollment);
        await _campusContext.SaveChangesAsync();

        await _campusContext.Entry(enrollment).Reference(e => e.Student).LoadAsync();
        await _campusContext.Entry(enrollment).Reference(e => e.Course).LoadAsync();
        return enrollment;
    }

    public async Task<EnrollmentEntry?> UpdateGradeAsync(int enrollmentId, double? grade)
    {
        var targetEnrollment = await _campusContext.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (targetEnrollment == null)
        {
            return null;
        }

        targetEnrollment.Grade = grade;
        await _campusContext.SaveChangesAsync();
        return targetEnrollment;
    }

    public async Task<bool> DeleteAsync(int enrollmentId)
    {
        var targetEnrollment = await _campusContext.Enrollments.FindAsync(enrollmentId);
        if (targetEnrollment == null)
        {
            return false;
        }

        _campusContext.Enrollments.Remove(targetEnrollment);
        await _campusContext.SaveChangesAsync();
        return true;
    }
}