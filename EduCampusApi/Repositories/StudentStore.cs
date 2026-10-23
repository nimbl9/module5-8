using Microsoft.EntityFrameworkCore;
using module1_4.Data;
using module1_4.Models;

namespace module1_4.Repositories;

public class StudentStore : IStudentStore
{
    private readonly EduCampusContext _campusContext;

    public StudentStore(EduCampusContext campusContext)
    {
        _campusContext = campusContext;
    }

    public async Task<IReadOnlyList<StudentEntry>> GetAllAsync()
    {
        return await _campusContext.Students
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync();
    }

    public async Task<StudentEntry?> GetByIdAsync(int studentId)
    {
        return await _campusContext.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId);
    }

    public async Task<StudentEntry?> GetWithEnrollmentsAsync(int studentId)
    {
        return await _campusContext.Students
            .AsNoTracking()
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId);
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _campusContext.Students
            .AnyAsync(s => s.Email.ToLower() == normalizedEmail && (!excludeId.HasValue || s.Id != excludeId.Value));
    }

    public async Task<StudentEntry> AddAsync(StudentEntry student)
    {
        student.CreatedAt = DateTime.UtcNow;
        await _campusContext.Students.AddAsync(student);
        await _campusContext.SaveChangesAsync();
        return student;
    }

    public async Task<StudentEntry> UpdateAsync(StudentEntry student)
    {
        _campusContext.Students.Update(student);
        await _campusContext.SaveChangesAsync();
        return student;
    }

    public async Task<bool> DeleteAsync(int studentId)
    {
        var itemToDelete = await _campusContext.Students.FindAsync(studentId);
        if (itemToDelete == null)
        {
            return false;
        }

        _campusContext.Students.Remove(itemToDelete);
        await _campusContext.SaveChangesAsync();
        return true;
    }
}