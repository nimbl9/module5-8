using System.Diagnostics;
using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using module1_4.Common;
using module1_4.DTOs;
using module1_4.Models;
using module1_4.Repositories;

namespace module1_4.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
[Route("api/courses")]
public class CoursesFlowController : ControllerBase
{
    private readonly ICourseStore _courseStore;
    private readonly ITeacherStore _teacherStore;
    private readonly IMapper _mapper;
    private readonly ILogger<CoursesFlowController> _logger;

    public CoursesFlowController(
        ICourseStore courseStore,
        ITeacherStore teacherStore,
        IMapper mapper,
        ILogger<CoursesFlowController> logger)
    {
        _courseStore = courseStore;
        _teacherStore = teacherStore;
        _mapper = mapper;
        _logger = logger;
    }

    private string CurrentTraceId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponsePacket<IReadOnlyList<CourseBriefDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] CourseFilterQuery filter)
    {
        var rawCourses = await _courseStore.GetAllAsync(filter);
        var mappedDtos = _mapper.Map<IReadOnlyList<CourseBriefDto>>(rawCourses);

        _logger.LogInformation("анонимный или авторизованный клиент запросил список курсов v1, найдено {Count} предметов", rawCourses.Count);
        return Ok(ApiResponsePacket<IReadOnlyList<CourseBriefDto>>.Success(mappedDtos, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseBriefDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _courseStore.GetByIdAsync(id);
        if (course == null)
        {
            _logger.LogWarning("курс с идентификатором {Id} не найден", id);
            return NotFound(ApiResponsePacket<CourseBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "COURSE_NOT_FOUND",
                $"курс с номером {id} не найден",
                CurrentTraceId));
        }

        var dto = _mapper.Map<CourseBriefDto>(course);
        _logger.LogInformation("выдали информацию по курсу «{CourseName}» ({Id}) версии v1", course.Name, id);
        return Ok(ApiResponsePacket<CourseBriefDto>.Success(dto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseBriefDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseBriefDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CourseCreatePayload payload)
    {
        var teacherExists = await _teacherStore.ExistsAsync(payload.TeacherId);
        if (!teacherExists)
        {
            _logger.LogWarning("отклонено создание курса «{CourseName}»: преподаватель {TeacherId} не существует", payload.Name, payload.TeacherId);
            return BadRequest(ApiResponsePacket<CourseBriefDto>.Failure(
                StatusCodes.Status400BadRequest,
                "TEACHER_NOT_FOUND",
                $"нельзя закрепить курс за несуществующим преподавателем с кодом {payload.TeacherId}",
                CurrentTraceId));
        }

        var courseEntity = _mapper.Map<CourseEntry>(payload);
        var createdCourse = await _courseStore.AddAsync(courseEntity);
        var createdDto = _mapper.Map<CourseBriefDto>(createdCourse);

        _logger.LogInformation("администратор создал курс «{CourseName}» (кредитов: {Credits}), преподаватель {TeacherName}", createdCourse.Name, createdCourse.Credits, createdDto.TeacherFullName);
        return StatusCode(StatusCodes.Status201Created, ApiResponsePacket<CourseBriefDto>.Success(createdDto, StatusCodes.Status201Created, CurrentTraceId));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseBriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseBriefDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseBriefDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] CourseUpdatePayload payload)
    {
        var existingCourse = await _courseStore.GetByIdAsync(id);
        if (existingCourse == null)
        {
            _logger.LogWarning("попытка обновить несуществующий курс с номером {Id}", id);
            return NotFound(ApiResponsePacket<CourseBriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "COURSE_NOT_FOUND",
                $"курс с номером {id} не найден",
                CurrentTraceId));
        }

        var teacherExists = await _teacherStore.ExistsAsync(payload.TeacherId);
        if (!teacherExists)
        {
            _logger.LogWarning("попытка перепривязать курс {Id} на несуществующего преподавателя {TeacherId}", id, payload.TeacherId);
            return BadRequest(ApiResponsePacket<CourseBriefDto>.Failure(
                StatusCodes.Status400BadRequest,
                "TEACHER_NOT_FOUND",
                $"преподаватель с кодом {payload.TeacherId} не существует",
                CurrentTraceId));
        }

        _mapper.Map(payload, existingCourse);
        var updatedCourse = await _courseStore.UpdateAsync(existingCourse);
        var updatedDto = _mapper.Map<CourseBriefDto>(updatedCourse);

        _logger.LogInformation("администратор обновил курс {Id} «{CourseName}»", id, updatedCourse.Name);
        return Ok(ApiResponsePacket<CourseBriefDto>.Success(updatedDto, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var removed = await _courseStore.DeleteAsync(id);
        if (!removed)
        {
            _logger.LogWarning("попытка удалить несуществующий курс {Id}", id);
            return NotFound(ApiResponsePacket<bool>.Failure(
                StatusCodes.Status404NotFound,
                "COURSE_NOT_FOUND",
                $"курс с номером {id} не найден",
                CurrentTraceId));
        }

        _logger.LogInformation("администратор удалил курс с номером {Id}", id);
        return Ok(ApiResponsePacket<bool>.Success(true, StatusCodes.Status200OK, CurrentTraceId));
    }
}