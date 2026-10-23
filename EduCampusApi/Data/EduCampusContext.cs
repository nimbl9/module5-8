using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using module1_4.Models;

namespace module1_4.Data;

public class EduCampusContext : IdentityDbContext<AppUser, AppRole, int>
{
    public EduCampusContext(DbContextOptions<EduCampusContext> options) : base(options)
    {
    }

    public DbSet<StudentEntry> Students => Set<StudentEntry>();
    public DbSet<TeacherEntry> Teachers => Set<TeacherEntry>();
    public DbSet<CourseEntry> Courses => Set<CourseEntry>();
    public DbSet<EnrollmentEntry> Enrollments => Set<EnrollmentEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasOne(u => u.Student)
                  .WithMany()
                  .HasForeignKey(u => u.StudentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(u => u.Teacher)
                  .WithMany()
                  .HasForeignKey(u => u.TeacherId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StudentEntry>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.FirstName).IsRequired().HasMaxLength(80);
            entity.Property(s => s.LastName).IsRequired().HasMaxLength(80);
            entity.Property(s => s.Email).IsRequired().HasMaxLength(120);
            entity.HasIndex(s => s.Email).IsUnique();
        });

        modelBuilder.Entity<TeacherEntry>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.FirstName).IsRequired().HasMaxLength(80);
            entity.Property(t => t.LastName).IsRequired().HasMaxLength(80);
            entity.Property(t => t.Email).IsRequired().HasMaxLength(120);
            entity.Property(t => t.Department).IsRequired().HasMaxLength(150);
            entity.HasIndex(t => t.Email).IsUnique();
        });

        modelBuilder.Entity<CourseEntry>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(160);
            entity.Property(c => c.Description).HasMaxLength(600);
            entity.Property(c => c.Credits).IsRequired();

            entity.HasOne(c => c.Teacher)
                  .WithMany(t => t.Courses)
                  .HasForeignKey(c => c.TeacherId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EnrollmentEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.Enrollments)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                  .WithMany(c => c.Enrollments)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}