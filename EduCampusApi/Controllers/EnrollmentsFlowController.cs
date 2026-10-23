using System.Diagnostics;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using module1_4.Common;
using module1_4.DTOs;
using module1_4.Models;
using module1_4.Repositories;

namespace module1_4.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsFlowController : ControllerBase
{
    private readonly IEnrollmentStore _enrollmentStore;
    private readonly IStudentStore _studentStore;
    private readonly ICourseStore _courseStore;
    private readonly IMapper _mapper;
    private readonly ILogger<EnrollmentsFlowController> _logger;

    public EnrollmentsFlowController(
        IEnrollmentStore enrollmentStore,
        IStudentStore studentStore,
        ICourseStore courseStore,
        IMapper mapper,
        ILogger<EnrollmentsFlowController> logger)
    {
        _enrollmentStore = enrollmentStore;
        _studentStore = studentStore;
        _courseStore = courseStore;
        _mapper = mapper;
        _logger = logger;
    }

    private string CurrentTraceId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(typeof(ApiResponsePacket<IReadOnlyList<EnrollmentBriefDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var rawEnrollments = await _enrollmentStore.GetAllAsync();
        var mappedDtos = _mapper.Map<IReadOnlyList<EnrollmentBriefDto>>(rawEnrollments);

        _logger.LogInformation("запросили все активные записи на курсы, найдено {Count} записей", rawEnrollments.Count);
        return Ok(ApiResponsePacket<IReadOnlyList<EnrollmentBriefDto>>.Success(mappedDtos, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponsePacket<EnrollmentBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<EnrollmentBriefDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        var enrollment = await _enrollmentStore.GetByIdAsync(id);
        if (enrollment == null)
        {
            _logger.LogWarning("запись на курс с идентификатором {Id} не найдена", id);
            return NotFound(ApiResponsePacket<EnrollmentBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "ENROLLMENT_NOT_FOUND",
                $"запись с номером {id} не найдена",
                CurrentTraceId));
        }

        if (User.IsInRole("Student"))
        {
            var studentIdClaim = User.FindFirstValue("studentId");
            if (int.TryParse(studentIdClaim, out var currentStudentId) && currentStudentId != enrollment.StudentId)
            {
                _logger.LogWarning("студент пытается просмотреть чужую запись на курс {EnrollmentId}", id);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponsePacket<EnrollmentBriefDto>.Failure(
                    StatusCodes.Status403Forbidden,
                    "STUDENT_FORBIDDEN",
                    "студент может просматривать только собственные записи",
                    CurrentTraceId));
            }
        }

        var dto = _mapper.Map<EnrollmentBriefDto>(enrollment);
        _logger.LogInformation("выдали запись #{Id}: студент {Student} на курсе «{Course}»", id, dto.StudentFullName, dto.CourseName);
        return Ok(ApiResponsePacket<EnrollmentBriefDto>.Success(dto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Student")]
    [ProducesResponseType(typeof(ApiResponsePacket<EnrollmentBriefDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponsePacket<EnrollmentBriefDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponsePacket<EnrollmentBriefDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EnrollStudent([FromBody] EnrollmentCreatePayload payload)
    {
        if (User.IsInRole("Student"))
        {
            var studentIdClaim = User.FindFirstValue("studentId");
            if (int.TryParse(studentIdClaim, out var currentStudentId) && currentStudentId != payload.StudentId)
            {
                _logger.LogWarning("студент пытается записать другого студента на курс");
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponsePacket<EnrollmentBriefDto>.Failure(
                    StatusCodes.Status403Forbidden,
                    "STUDENT_FORBIDDEN",
                    "студент может записывать на курс только самого себя",
                    CurrentTraceId));
            }
        }

        var targetStudent = await _studentStore.GetByIdAsync(payload.StudentId);
        if (targetStudent == null)
        {
            _logger.LogWarning("отклонена запись на курс: студент {StudentId} не существует", payload.StudentId);
            return NotFound(ApiResponsePacket<EnrollmentBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "STUDENT_NOT_FOUND",
                $"нельзя записать несуществующего студента с кодом {payload.StudentId}",
                CurrentTraceId));
        }

        var targetCourse = await _courseStore.GetByIdAsync(payload.CourseId);
        if (targetCourse == null)
        {
            _logger.LogWarning("отклонена запись на курс: курс {CourseId} не существует", payload.CourseId);
            return NotFound(ApiResponsePacket<EnrollmentBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "COURSE_NOT_FOUND",
                $"нельзя записать студента на несуществующий курс с кодом {payload.CourseId}",
                CurrentTraceId));
        }

        var alreadyEnrolled = await _enrollmentStore.ExistsAsync(payload.StudentId, payload.CourseId);
        if (alreadyEnrolled)
        {
            _logger.LogWarning("дублирование записи: студент {StudentId} уже записан на курс {CourseId}", payload.StudentId, payload.CourseId);
            return Conflict(ApiResponsePacket<EnrollmentBriefDto>.Failure(
                StatusCodes.Status409Conflict,
                "ENROLLMENT_DUPLICATE",
                $"студент «{targetStudent.FirstName} {targetStudent.LastName}» уже числится на курсе «{targetCourse.Name}»",
                CurrentTraceId));
        }

        var newEnrollment = _mapper.Map<EnrollmentEntry>(payload);
        var createdEnrollment = await _enrollmentStore.AddAsync(newEnrollment);
        var createdDto = _mapper.Map<EnrollmentBriefDto>(createdEnrollment);

        _logger.LogInformation("записали студента {Student} на курс «{Course}», присвоен код записи {Id}", createdDto.StudentFullName, createdDto.CourseName, createdEnrollment.Id);
        return StatusCode(StatusCodes.Status201Created, ApiResponsePacket<EnrollmentBriefDto>.Success(createdDto, StatusCodes.Status201Created, CurrentTraceId));
    }

    [HttpPut("{id:int}/grade")]
    [HttpPatch("{id:int}/grade")]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(typeof(ApiResponsePacket<EnrollmentBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<EnrollmentBriefDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateGrade(int id, [FromBody] EnrollmentGradePatchPayload payload)
    {
        var existingEnrollment = await _enrollmentStore.GetByIdAsync(id);
        if (existingEnrollment == null)
        {
            _logger.LogWarning("попытка выставить оценку для несуществующей записи {Id}", id);
            return NotFound(ApiResponsePacket<EnrollmentBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "ENROLLMENT_NOT_FOUND",
                $"запись с номером {id} не найдена",
                CurrentTraceId));
        }

        if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
        {
            var teacherIdClaim = User.FindFirstValue("teacherId");
            if (string.IsNullOrWhiteSpace(teacherIdClaim) ||
                !int.TryParse(teacherIdClaim, out var currentTeacherId) ||
                existingEnrollment.Course?.TeacherId != currentTeacherId)
            {
                _logger.LogWarning("преподаватель с кодом {TeacherId} пытается изменить оценку на чужом курсе #{CourseId}", teacherIdClaim, existingEnrollment.CourseId);
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponsePacket<EnrollmentBriefDto>.Failure(
                    StatusCodes.Status403Forbidden,
                    "TEACHER_FORBIDDEN_COURSE",
                    "преподаватель имеет право выставлять и изменять оценки только по собственным учебным курсам",
                    CurrentTraceId));
            }
        }

        var updatedEnrollment = await _enrollmentStore.UpdateGradeAsync(id, payload.Grade);
        var dto = _mapper.Map<EnrollmentBriefDto>(updatedEnrollment!);

        _logger.LogInformation("изменили оценку для записи #{Id} (студент {Student}, курс «{Course}») на значение {Grade}", id, dto.StudentFullName, dto.CourseName, payload.Grade);
        return Ok(ApiResponsePacket<EnrollmentBriefDto>.Success(dto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnenrollStudent(int id)
    {
        var deleted = await _enrollmentStore.DeleteAsync(id);
        if (!deleted)
        {
            _logger.LogWarning("попытка отчислить студента по несуществующей записи {Id}", id);
            return NotFound(ApiResponsePacket<bool>.Failure(
                StatusCodes.Status404NotFound,
                "ENROLLMENT_NOT_FOUND",
                $"запись с номером {id} не найдена",
                CurrentTraceId));
        }

        _logger.LogInformation("администратор удалил студента с курса по записи с номером {Id}", id);
        return Ok(ApiResponsePacket<bool>.Success(true, StatusCodes.Status200OK, CurrentTraceId));
    }
}