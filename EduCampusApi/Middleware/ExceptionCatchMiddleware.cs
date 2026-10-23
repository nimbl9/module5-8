using System.Diagnostics;
using System.Text.Json;
using module1_4.Common;

namespace module1_4.Middleware;

public class ExceptionCatchMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionCatchMiddleware> _logger;

    public ExceptionCatchMiddleware(RequestDelegate next, ILogger<ExceptionCatchMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "произошла непредвиденная ошибка на сервере при обработке запроса: {Message}", ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var currentTraceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var failureEnvelope = ApiResponsePacket<object>.Failure(
                StatusCodes.Status500InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "на сервере произошла непредвиденная ошибка",
                currentTraceId
            );

            var serializedResult = JsonSerializer.Serialize(failureEnvelope, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(serializedResult);
        }
    }
}