using Microsoft.AspNetCore.Identity;

namespace module1_4.Models;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public int? StudentId { get; set; }
    public StudentEntry? Student { get; set; }
    public int? TeacherId { get; set; }
    public TeacherEntry? Teacher { get; set; }
}

public class AppRole : IdentityRole<int>
{
    public AppRole() : base() { }
    public AppRole(string roleName) : base(roleName) { }
}