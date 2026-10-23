using Microsoft.EntityFrameworkCore;
using module1_4.Data;
using module1_4.Models;

namespace module1_4.Repositories;

public class TeacherStore : ITeacherStore
{
    private readonly EduCampusContext _campusContext;

    public TeacherStore(EduCampusContext campusContext)
    {
        _campusContext = campusContext;
    }

    public async Task<IReadOnlyList<TeacherEntry>> GetAllAsync()
    {
        return await _campusContext.Teachers
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync();
    }

    public async Task<TeacherEntry?> GetByIdAsync(int teacherId)
    {
        return await _campusContext.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teacherId);
    }

    public async Task<bool> ExistsAsync(int teacherId)
    {
        return await _campusContext.Teachers
            .AnyAsync(t => t.Id == teacherId);
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _campusContext.Teachers
            .AnyAsync(t => t.Email.ToLower() == normalizedEmail && (!excludeId.HasValue || t.Id != excludeId.Value));
    }

    public async Task<bool> HasAssignedCoursesAsync(int teacherId)
    {
        return await _campusContext.Courses
            .AnyAsync(c => c.TeacherId == teacherId);
    }

    public async Task<TeacherEntry> AddAsync(TeacherEntry teacher)
    {
        await _campusContext.Teachers.AddAsync(teacher);
        await _campusContext.SaveChangesAsync();
        return teacher;
    }

    public async Task<TeacherEntry> UpdateAsync(TeacherEntry teacher)
    {
        _campusContext.Teachers.Update(teacher);
        await _campusContext.SaveChangesAsync();
        return teacher;
    }

    public async Task<bool> DeleteAsync(int teacherId)
    {
        var targetTeacher = await _campusContext.Teachers.FindAsync(teacherId);
        if (targetTeacher == null)
        {
            return false;
        }

        _campusContext.Teachers.Remove(targetTeacher);
        await _campusContext.SaveChangesAsync();
        return true;
    }
}