using module1_4.DTOs;
using module1_4.Models;

namespace module1_4.Repositories;

public interface ICourseStore
{
    Task<IReadOnlyList<CourseEntry>> GetAllAsync(CourseFilterQuery? filter = null);
    Task<CourseEntry?> GetByIdAsync(int courseId);
    Task<bool> ExistsAsync(int courseId);
    Task<CourseEntry> AddAsync(CourseEntry course);
    Task<CourseEntry> UpdateAsync(CourseEntry course);
    Task<bool> DeleteAsync(int courseId);
}