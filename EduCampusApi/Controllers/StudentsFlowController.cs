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
[Route("api/students")]
public class StudentsFlowController : ControllerBase
{
    private readonly IStudentStore _studentStore;
    private readonly IMapper _mapper;
    private readonly ILogger<StudentsFlowController> _logger;

    public StudentsFlowController(IStudentStore studentStore, IMapper mapper, ILogger<StudentsFlowController> logger)
    {
        _studentStore = studentStore;
        _mapper = mapper;
        _logger = logger;
    }

    private string CurrentTraceId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    private bool IsStudentForbiddenToAccess(int targetStudentId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Teacher"))
        {
            return false;
        }

        if (User.IsInRole("Student"))
        {
            var studentIdClaim = User.FindFirstValue("studentId");
            if (int.TryParse(studentIdClaim, out var currentStudentId))
            {
                return currentStudentId != targetStudentId;
            }
            return true;
        }

        return true;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(typeof(ApiResponsePacket<IReadOnlyList<StudentBriefDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var rawStudents = await _studentStore.GetAllAsync();
        var mappedDtos = _mapper.Map<IReadOnlyList<StudentBriefDto>>(rawStudents);

        _logger.LogInformation("преподаватель или администратор запросил список всех студентов, найдено {Count} записей", rawStudents.Count);
        return Ok(ApiResponsePacket<IReadOnlyList<StudentBriefDto>>.Success(mappedDtos, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentBriefDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        if (IsStudentForbiddenToAccess(id))
        {
            _logger.LogWarning("отклонен доступ студента к чужому профилю студента {Id}", id);
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponsePacket<StudentBriefDto>.Failure(
                StatusCodes.Status403Forbidden,
                "STUDENT_FORBIDDEN",
                "студент имеет доступ только к собственному профилю",
                CurrentTraceId));
        }

        var student = await _studentStore.GetByIdAsync(id);
        if (student == null)
        {
            _logger.LogWarning("студент с номером {Id} не обнаружен в базе данных", id);
            return NotFound(ApiResponsePacket<StudentBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "STUDENT_NOT_FOUND",
                $"студент с номером {id} не найден",
                CurrentTraceId));
        }

        var dto = _mapper.Map<StudentBriefDto>(student);
        _logger.LogInformation("получили данные студента {FullName} под номером {Id}", $"{student.FirstName} {student.LastName}", id);
        return Ok(ApiResponsePacket<StudentBriefDto>.Success(dto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpGet("{id:int}/courses")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentDetailedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentDetailedDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentWithCourses(int id)
    {
        if (IsStudentForbiddenToAccess(id))
        {
            _logger.LogWarning("студент пытается просмотреть чужие курсы и оценки для студента {Id}", id);
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponsePacket<StudentDetailedDto>.Failure(
                StatusCodes.Status403Forbidden,
                "STUDENT_FORBIDDEN_GRADES",
                "студент не имеет права просматривать чужие оценки и курсы",
                CurrentTraceId));
        }

        var student = await _studentStore.GetWithEnrollmentsAsync(id);
        if (student == null)
        {
            _logger.LogWarning("не удалось получить курсы: студент {Id} отсутствует в системе", id);
            return NotFound(ApiResponsePacket<StudentDetailedDto>.Failure(
                StatusCodes.Status404NotFound,
                "STUDENT_NOT_FOUND",
                $"студент с номером {id} не найден",
                CurrentTraceId));
        }

        var dto = _mapper.Map<StudentDetailedDto>(student);
        _logger.LogInformation("выдали студента {FullName} вместе со списком его курсов ({CourseCount} шт.)", $"{student.FirstName} {student.LastName}", dto.EnrolledCourses.Count);
        return Ok(ApiResponsePacket<StudentDetailedDto>.Success(dto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentBriefDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentBriefDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] StudentCreatePayload payload)
    {
        var emailOccupied = await _studentStore.ExistsByEmailAsync(payload.Email);
        if (emailOccupied)
        {
            _logger.LogWarning("попытка создать студента с уже занятой почтой {Email}", payload.Email);
            return Conflict(ApiResponsePacket<StudentBriefDto>.Failure(
                StatusCodes.Status409Conflict,
                "STUDENT_EMAIL_DUPLICATE",
                $"студент с адресом {payload.Email} уже зарегистрирован в системе",
                CurrentTraceId));
        }

        var studentEntity = _mapper.Map<StudentEntry>(payload);
        var createdStudent = await _studentStore.AddAsync(studentEntity);
        var createdDto = _mapper.Map<StudentBriefDto>(createdStudent);

        _logger.LogInformation("администратор добавил нового студента {FullName} с кодом {Id}", $"{createdStudent.FirstName} {createdStudent.LastName}", createdStudent.Id);
        return StatusCode(StatusCodes.Status201Created, ApiResponsePacket<StudentBriefDto>.Success(createdDto, StatusCodes.Status201Created, CurrentTraceId));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentBriefDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponsePacket<StudentBriefDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] StudentUpdatePayload payload)
    {
        var existingStudent = await _studentStore.GetByIdAsync(id);
        if (existingStudent == null)
        {
            _logger.LogWarning("попытка обновить данные несуществующего студента {Id}", id);
            return NotFound(ApiResponsePacket<StudentBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "STUDENT_NOT_FOUND",
                $"студент с номером {id} не найден",
                CurrentTraceId));
        }

        var emailOccupied = await _studentStore.ExistsByEmailAsync(payload.Email, id);
        if (emailOccupied)
        {
            _logger.LogWarning("конфликт почты при обновлении студента {Id}: адрес {Email} уже используется", id, payload.Email);
            return Conflict(ApiResponsePacket<StudentBriefDto>.Failure(
                StatusCodes.Status409Conflict,
                "STUDENT_EMAIL_DUPLICATE",
                $"адрес {payload.Email} уже занят другим студентом",
                CurrentTraceId));
        }

        _mapper.Map(payload, existingStudent);
        var updatedStudent = await _studentStore.UpdateAsync(existingStudent);
        var updatedDto = _mapper.Map<StudentBriefDto>(updatedStudent);

        _logger.LogInformation("администратор обновил профиль студента {Id} ({FullName})", id, $"{updatedStudent.FirstName} {updatedStudent.LastName}");
        return Ok(ApiResponsePacket<StudentBriefDto>.Success(updatedDto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var removed = await _studentStore.DeleteAsync(id);
        if (!removed)
        {
            _logger.LogWarning("попытка удалить несуществующего студента с номером {Id}", id);
            return NotFound(ApiResponsePacket<bool>.Failure(
                StatusCodes.Status404NotFound,
                "STUDENT_NOT_FOUND",
                $"студент с номером {id} не найден",
                CurrentTraceId));
        }

        _logger.LogInformation("администратор удалил студента с идентификатором {Id}", id);
        return Ok(ApiResponsePacket<bool>.Success(true, StatusCodes.Status200OK, CurrentTraceId));
    }
}