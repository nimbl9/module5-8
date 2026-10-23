using System.Diagnostics;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using module1_4.Common;
using module1_4.DTOs;
using module1_4.Models;
using module1_4.Repositories;

namespace module1_4.Controllers;

[ApiController]
[Route("api/teachers")]
public class TeachersFlowController : ControllerBase
{
    private readonly ITeacherStore _teacherStore;
    private readonly IMapper _mapper;
    private readonly ILogger<TeachersFlowController> _logger;

    public TeachersFlowController(ITeacherStore teacherStore, IMapper mapper, ILogger<TeachersFlowController> logger)
    {
        _teacherStore = teacherStore;
        _mapper = mapper;
        _logger = logger;
    }

    private string CurrentTraceId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponsePacket<IReadOnlyList<TeacherBriefDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var rawTeachers = await _teacherStore.GetAllAsync();
        var mappedDtos = _mapper.Map<IReadOnlyList<TeacherBriefDto>>(rawTeachers);

        _logger.LogInformation("запросили перечень преподавателей, найдено {Count} сотрудников", rawTeachers.Count);
        return Ok(ApiResponsePacket<IReadOnlyList<TeacherBriefDto>>.Success(mappedDtos, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponsePacket<TeacherBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<TeacherBriefDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var teacher = await _teacherStore.GetByIdAsync(id);
        if (teacher == null)
        {
            _logger.LogWarning("преподаватель с номером {Id} не найден в реестре", id);
            return NotFound(ApiResponsePacket<TeacherBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "TEACHER_NOT_FOUND",
                $"преподаватель с номером {id} не найден",
                CurrentTraceId));
        }

        var dto = _mapper.Map<TeacherBriefDto>(teacher);
        _logger.LogInformation("преподаватель {FullName} успешно загружен из базы", $"{teacher.FirstName} {teacher.LastName}");
        return Ok(ApiResponsePacket<TeacherBriefDto>.Success(dto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<TeacherBriefDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponsePacket<TeacherBriefDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] TeacherCreatePayload payload)
    {
        var emailOccupied = await _teacherStore.ExistsByEmailAsync(payload.Email);
        if (emailOccupied)
        {
            _logger.LogWarning("попытка регистрации преподавателя с уже занятой почтой {Email}", payload.Email);
            return Conflict(ApiResponsePacket<TeacherBriefDto>.Failure(
                StatusCodes.Status409Conflict,
                "TEACHER_EMAIL_DUPLICATE",
                $"преподаватель с адресом {payload.Email} уже зарегистрирован",
                CurrentTraceId));
        }

        var teacherEntity = _mapper.Map<TeacherEntry>(payload);
        var createdTeacher = await _teacherStore.AddAsync(teacherEntity);
        var createdDto = _mapper.Map<TeacherBriefDto>(createdTeacher);

        _logger.LogInformation("администратор зарегистрировал преподавателя {FullName} ({Department}), код {Id}", $"{createdTeacher.FirstName} {createdTeacher.LastName}", createdTeacher.Department, createdTeacher.Id);
        return StatusCode(StatusCodes.Status201Created, ApiResponsePacket<TeacherBriefDto>.Success(createdDto, StatusCodes.Status201Created, CurrentTraceId));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<TeacherBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<TeacherBriefDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponsePacket<TeacherBriefDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] TeacherUpdatePayload payload)
    {
        var existingTeacher = await _teacherStore.GetByIdAsync(id);
        if (existingTeacher == null)
        {
            _logger.LogWarning("попытка обновления несуществующего преподавателя {Id}", id);
            return NotFound(ApiResponsePacket<TeacherBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "TEACHER_NOT_FOUND",
                $"преподаватель с номером {id} не найден",
                CurrentTraceId));
        }

        var emailOccupied = await _teacherStore.ExistsByEmailAsync(payload.Email, id);
        if (emailOccupied)
        {
            _logger.LogWarning("конфликт адреса почты при изменении данных преподавателя {Id}: {Email}", id, payload.Email);
            return Conflict(ApiResponsePacket<TeacherBriefDto>.Failure(
                StatusCodes.Status409Conflict,
                "TEACHER_EMAIL_DUPLICATE",
                $"почта {payload.Email} уже занята другим преподавателем",
                CurrentTraceId));
        }

        _mapper.Map(payload, existingTeacher);
        var updatedTeacher = await _teacherStore.UpdateAsync(existingTeacher);
        var updatedDto = _mapper.Map<TeacherBriefDto>(updatedTeacher);

        _logger.LogInformation("администратор обновил карточку преподавателя {Id} ({FullName})", id, $"{updatedTeacher.FirstName} {updatedTeacher.LastName}");
        return Ok(ApiResponsePacket<TeacherBriefDto>.Success(updatedDto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var existingTeacher = await _teacherStore.GetByIdAsync(id);
        if (existingTeacher == null)
        {
            _logger.LogWarning("попытка удалить несуществующего преподавателя {Id}", id);
            return NotFound(ApiResponsePacket<bool>.Failure(
                StatusCodes.Status404NotFound,
                "TEACHER_NOT_FOUND",
                $"преподаватель с номером {id} не найден",
                CurrentTraceId));
        }

        var hasCourses = await _teacherStore.HasAssignedCoursesAsync(id);
        if (hasCourses)
        {
            _logger.LogWarning("отклонено удаление преподавателя {Id}: за ним числятся учебные курсы", id);
            return Conflict(ApiResponsePacket<bool>.Failure(
                StatusCodes.Status409Conflict,
                "TEACHER_HAS_COURSES",
                "нельзя удалить преподавателя, за которым закреплены учебные курсы",
                CurrentTraceId));
        }

        await _teacherStore.DeleteAsync(id);
        _logger.LogInformation("администратор удалил преподавателя {FullName} с кодом {Id}", $"{existingTeacher.FirstName} {existingTeacher.LastName}", id);
        return Ok(ApiResponsePacket<bool>.Success(true, StatusCodes.Status200OK, CurrentTraceId));
    }
}