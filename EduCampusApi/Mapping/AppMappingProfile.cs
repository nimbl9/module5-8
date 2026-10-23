using AutoMapper;
using module1_4.DTOs;
using module1_4.Models;

namespace module1_4.Mapping;

public class AppMappingProfile : Profile
{
    public AppMappingProfile()
    {
        CreateMap<StudentCreatePayload, StudentEntry>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore());

        CreateMap<StudentUpdatePayload, StudentEntry>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore());

        CreateMap<StudentEntry, StudentBriefDto>();

        CreateMap<StudentEntry, StudentDetailedDto>()
            .ForMember(dest => dest.EnrolledCourses, opt => opt.MapFrom(src => src.Enrollments));

        CreateMap<EnrollmentEntry, StudentCourseRecordDto>()
            .ForMember(dest => dest.EnrollmentId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.CourseId))
            .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course != null ? src.Course.Name : string.Empty))
            .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Course != null ? src.Course.Credits : 0))
            .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.EnrollmentDate))
            .ForMember(dest => dest.Grade, opt => opt.MapFrom(src => src.Grade));

        CreateMap<TeacherCreatePayload, TeacherEntry>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Courses, opt => opt.Ignore());

        CreateMap<TeacherUpdatePayload, TeacherEntry>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Courses, opt => opt.Ignore());

        CreateMap<TeacherEntry, TeacherBriefDto>();

        CreateMap<CourseCreatePayload, CourseEntry>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Teacher, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore());

        CreateMap<CourseUpdatePayload, CourseEntry>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Teacher, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore());

        CreateMap<CourseEntry, CourseBriefDto>()
            .ForMember(dest => dest.TeacherFullName, opt => opt.MapFrom(src => src.Teacher != null ? $"{src.Teacher.FirstName} {src.Teacher.LastName}".Trim() : string.Empty));

        CreateMap<CourseEntry, CourseV2BriefDto>()
            .ForMember(dest => dest.TeacherFullName, opt => opt.MapFrom(src => src.Teacher != null ? $"{src.Teacher.FirstName} {src.Teacher.LastName}".Trim() : string.Empty))
            .ForMember(dest => dest.TeacherEmail, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.Email : string.Empty))
            .ForMember(dest => dest.TeacherDepartment, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.Department : string.Empty))
            .ForMember(dest => dest.EnrolledStudentsCount, opt => opt.MapFrom(src => src.Enrollments.Count))
            .ForMember(dest => dest.AverageGrade, opt => opt.MapFrom(src => src.Enrollments.Where(e => e.Grade.HasValue).Select(e => e.Grade!.Value).DefaultIfEmpty().Average()));

        CreateMap<EnrollmentCreatePayload, EnrollmentEntry>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.EnrollmentDate, opt => opt.Ignore())
            .ForMember(dest => dest.Grade, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore());

        CreateMap<EnrollmentEntry, EnrollmentBriefDto>()
            .ForMember(dest => dest.StudentFullName, opt => opt.MapFrom(src => src.Student != null ? $"{src.Student.FirstName} {src.Student.LastName}".Trim() : string.Empty))
            .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course != null ? src.Course.Name : string.Empty));
    }
}