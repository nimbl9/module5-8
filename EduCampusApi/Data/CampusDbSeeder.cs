using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using module1_4.Models;

namespace module1_4.Data;

public static class CampusDbSeeder
{
    public static async Task SeedAsync(
        EduCampusContext context,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ILogger logger)
    {
        await context.Database.EnsureCreatedAsync();

        var roles = new[] { "Admin", "Teacher", "Student" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole(role));
            }
        }

        if (!await context.Students.AnyAsync())
        {
            var teacherErsultan = new TeacherEntry
            {
                FirstName = "Ерсултан",
                LastName = "Касымов",
                Email = "ersultan.kassymov@university.kz",
                Department = "Департамент программной инженерии"
            };

            var teacherGulnaz = new TeacherEntry
            {
                FirstName = "Гульназ",
                LastName = "Муратова",
                Email = "gulnaz.muratova@university.kz",
                Department = "Департамент компьютерных наук"
            };

            context.Teachers.AddRange(teacherErsultan, teacherGulnaz);
            await context.SaveChangesAsync();

            var studentAlikhan = new StudentEntry
            {
                FirstName = "Алихан",
                LastName = "Садыков",
                Email = "alikhan.sadykov@stud.university.kz",
                BirthDate = new DateTime(2004, 3, 12),
                CreatedAt = DateTime.UtcNow
            };

            var studentDina = new StudentEntry
            {
                FirstName = "Дина",
                LastName = "Ахметова",
                Email = "dina.akhmetova@stud.university.kz",
                BirthDate = new DateTime(2005, 7, 25),
                CreatedAt = DateTime.UtcNow
            };

            var studentAruzhan = new StudentEntry
            {
                FirstName = "Аружан",
                LastName = "Берикова",
                Email = "aruzhan.berikova@stud.university.kz",
                BirthDate = new DateTime(2004, 11, 5),
                CreatedAt = DateTime.UtcNow
            };

            var studentBauyrzhan = new StudentEntry
            {
                FirstName = "Бауыржан",
                LastName = "Омаров",
                Email = "bauyrzhan.omarov@stud.university.kz",
                BirthDate = new DateTime(2003, 9, 18),
                CreatedAt = DateTime.UtcNow
            };

            context.Students.AddRange(studentAlikhan, studentDina, studentAruzhan, studentBauyrzhan);
            await context.SaveChangesAsync();

            var courseWeb = new CourseEntry
            {
                Name = "Разработка веб-сервисов",
                Description = "Изучение принципов построения REST API, ASP.NET Core и EF Core",
                Credits = 5,
                TeacherId = teacherErsultan.Id,
                CreatedAt = DateTime.UtcNow
            };

            var courseDb = new CourseEntry
            {
                Name = "Архитектура баз данных",
                Description = "Проектирование реляционных баз данных, транзакции и оптимизация",
                Credits = 4,
                TeacherId = teacherGulnaz.Id,
                CreatedAt = DateTime.UtcNow
            };

            var courseAlgo = new CourseEntry
            {
                Name = "Алгоритмы и структуры данных",
                Description = "Сложность алгоритмов, деревья, графы и динамическое программирование",
                Credits = 6,
                TeacherId = teacherErsultan.Id,
                CreatedAt = DateTime.UtcNow
            };

            context.Courses.AddRange(courseWeb, courseDb, courseAlgo);
            await context.SaveChangesAsync();

            var enrollments = new[]
            {
                new EnrollmentEntry
                {
                    StudentId = studentAlikhan.Id,
                    CourseId = courseWeb.Id,
                    EnrollmentDate = DateTime.UtcNow.AddDays(-30),
                    Grade = 95
                },
                new EnrollmentEntry
                {
                    StudentId = studentAlikhan.Id,
                    CourseId = courseDb.Id,
                    EnrollmentDate = DateTime.UtcNow.AddDays(-28),
                    Grade = 88
                },
                new EnrollmentEntry
                {
                    StudentId = studentDina.Id,
                    CourseId = courseWeb.Id,
                    EnrollmentDate = DateTime.UtcNow.AddDays(-20),
                    Grade = 92
                },
                new EnrollmentEntry
                {
                    StudentId = studentAruzhan.Id,
                    CourseId = courseAlgo.Id,
                    EnrollmentDate = DateTime.UtcNow.AddDays(-10),
                    Grade = null
                }
            };

            context.Enrollments.AddRange(enrollments);
            await context.SaveChangesAsync();
        }

        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = "admin",
                Email = "admin@university.kz",
                FullName = "Нурлан Администратор",
                EmailConfirmed = true
            };
            var res = await userManager.CreateAsync(adminUser, "AdminPass123!");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        var ersultanUser = await userManager.FindByNameAsync("ersultan");
        if (ersultanUser == null)
        {
            var teacher1 = await context.Teachers.FirstOrDefaultAsync(t => t.FirstName == "Ерсултан");
            ersultanUser = new AppUser
            {
                UserName = "ersultan",
                Email = "ersultan.kassymov@university.kz",
                FullName = "Ерсултан Касымов",
                TeacherId = teacher1?.Id,
                EmailConfirmed = true
            };
            var res = await userManager.CreateAsync(ersultanUser, "TeacherPass123!");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(ersultanUser, "Teacher");
            }
        }

        var gulnazUser = await userManager.FindByNameAsync("gulnaz");
        if (gulnazUser == null)
        {
            var teacher2 = await context.Teachers.FirstOrDefaultAsync(t => t.FirstName == "Гульназ");
            gulnazUser = new AppUser
            {
                UserName = "gulnaz",
                Email = "gulnaz.muratova@university.kz",
                FullName = "Гульназ Муратова",
                TeacherId = teacher2?.Id,
                EmailConfirmed = true
            };
            var res = await userManager.CreateAsync(gulnazUser, "TeacherPass123!");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(gulnazUser, "Teacher");
            }
        }

        var alikhanUser = await userManager.FindByNameAsync("alikhan");
        if (alikhanUser == null)
        {
            var student1 = await context.Students.FirstOrDefaultAsync(s => s.FirstName == "Алихан");
            alikhanUser = new AppUser
            {
                UserName = "alikhan",
                Email = "alikhan.sadykov@stud.university.kz",
                FullName = "Алихан Садыков",
                StudentId = student1?.Id,
                EmailConfirmed = true
            };
            var res = await userManager.CreateAsync(alikhanUser, "StudentPass123!");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(alikhanUser, "Student");
            }
        }

        var dinaUser = await userManager.FindByNameAsync("dina");
        if (dinaUser == null)
        {
            var student2 = await context.Students.FirstOrDefaultAsync(s => s.FirstName == "Дина");
            dinaUser = new AppUser
            {
                UserName = "dina",
                Email = "dina.akhmetova@stud.university.kz",
                FullName = "Дина Ахметова",
                StudentId = student2?.Id,
                EmailConfirmed = true
            };
            var res = await userManager.CreateAsync(dinaUser, "StudentPass123!");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(dinaUser, "Student");
            }
        }

        logger.LogInformation("начальная база данных и пользователи identity успешно подготовлены");
    }
}