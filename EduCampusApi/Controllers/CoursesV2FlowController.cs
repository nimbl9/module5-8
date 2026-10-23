using System.Diagnostics;
using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using module1_4.Common;
using module1_4.DTOs;
using module1_4.Repositories;

namespace module1_4.Controllers;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/courses")]
public class CoursesV2FlowController : ControllerBase
{
    private readonly ICourseStore _courseStore;
    private readonly IMapper _mapper;
    private readonly ILogger<CoursesV2FlowController> _logger;

    public CoursesV2FlowController(
        ICourseStore courseStore,
        IMapper mapper,
        ILogger<CoursesV2FlowController> logger)
    {
        _courseStore = courseStore;
        _mapper = mapper;
        _logger = logger;
    }

    private string CurrentTraceId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponsePacket<IReadOnlyList<CourseV2BriefDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] CourseFilterQuery filter)
    {
        var rawCourses = await _courseStore.GetAllAsync(filter);
        var mappedDtos = _mapper.Map<IReadOnlyList<CourseV2BriefDto>>(rawCourses);

        _logger.LogInformation("клиент обратился ко второй версии api курсов (v2), возвращено {Count} расширенных записей", rawCourses.Count);
        return Ok(ApiResponsePacket<IReadOnlyList<CourseV2BriefDto>>.Success(mappedDtos, StatusCodes.Status200OK, CurrentTraceId));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseV2BriefDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponsePacket<CourseV2BriefDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _courseStore.GetByIdAsync(id);
        if (course == null)
        {
            _logger.LogWarning("курс {Id} не найден в реестре v2", id);
            return NotFound(ApiResponsePacket<CourseV2BriefDto>.Failure(
                StatusCodes.Status404NotFound,
                "COURSE_NOT_FOUND",
                $"курс с номером {id} не найден",
                CurrentTraceId));
        }

        var dto = _mapper.Map<CourseV2BriefDto>(course);
        _logger.LogInformation("выдали расширенную карточку v2 для курса «{CourseName}» с {StudentsCount} студентами", course.Name, dto.EnrolledStudentsCount);
        return Ok(ApiResponsePacket<CourseV2BriefDto>.Success(dto, StatusCodes.Status200OK, CurrentTraceId));
    }
}