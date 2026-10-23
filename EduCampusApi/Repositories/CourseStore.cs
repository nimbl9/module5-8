using Microsoft.EntityFrameworkCore;
using module1_4.Data;
using module1_4.DTOs;
using module1_4.Models;

namespace module1_4.Repositories;

public class CourseStore : ICourseStore
{
    private readonly EduCampusContext _campusContext;

    public CourseStore(EduCampusContext campusContext)
    {
        _campusContext = campusContext;
    }

    public async Task<IReadOnlyList<CourseEntry>> GetAllAsync(CourseFilterQuery? filter = null)
    {
        var query = _campusContext.Courses
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.NameQuery))
            {
                var term = filter.NameQuery.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(term) || c.Description.ToLower().Contains(term));
            }

            if (filter.MinCredits.HasValue)
            {
                query = query.Where(c => c.Credits >= filter.MinCredits.Value);
            }

            if (filter.MaxCredits.HasValue)
            {
                query = query.Where(c => c.Credits <= filter.MaxCredits.Value);
            }

            if (filter.TeacherId.HasValue)
            {
                query = query.Where(c => c.TeacherId == filter.TeacherId.Value);
            }
        }

        return await query.OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<CourseEntry?> GetByIdAsync(int courseId)
    {
        return await _campusContext.Courses
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public async Task<bool> ExistsAsync(int courseId)
    {
        return await _campusContext.Courses
            .AnyAsync(c => c.Id == courseId);
    }

    public async Task<CourseEntry> AddAsync(CourseEntry course)
    {
        course.CreatedAt = DateTime.UtcNow;
        await _campusContext.Courses.AddAsync(course);
        await _campusContext.SaveChangesAsync();

        await _campusContext.Entry(course).Reference(c => c.Teacher).LoadAsync();
        return course;
    }

    public async Task<CourseEntry> UpdateAsync(CourseEntry course)
    {
        _campusContext.Courses.Update(course);
        await _campusContext.SaveChangesAsync();

        await _campusContext.Entry(course).Reference(c => c.Teacher).LoadAsync();
        return course;
    }

    public async Task<bool> DeleteAsync(int courseId)
    {
        var targetCourse = await _campusContext.Courses.FindAsync(courseId);
        if (targetCourse == null)
        {
            return false;
        }

        _campusContext.Courses.Remove(targetCourse);
        await _campusContext.SaveChangesAsync();
        return true;
    }
}