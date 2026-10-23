using System.Text.Json;
using EduCampusClient.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduCampusClient.Pages;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IHttpClientFactory clientFactory, ILogger<IndexModel> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public List<CourseListItem> Courses { get; set; } = new();
    public string? ErrorAlert { get; set; }

    public async Task OnGetAsync()
    {
        var client = _clientFactory.CreateClient("CampusApiClient");

        try
        {
            var response = await client.GetAsync("api/v1/courses");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<CourseListItem>>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (envelope != null && envelope.IsSuccess && envelope.Result != null)
                {
                    Courses = envelope.Result;
                    _logger.LogInformation("клиент успешно загрузил список из {Count} курсов через http", Courses.Count);
                }
                else
                {
                    ErrorAlert = envelope?.ErrorMessage ?? "сервер вернул отрицательный результат операции";
                }
            }
            else
            {
                ErrorAlert = $"сервер вернул код ошибки: {(int)response.StatusCode} {response.ReasonPhrase}";
                _logger.LogWarning("неуспешный ответ от api при загрузке курсов: {StatusCode}", response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorAlert = $"не удалось подключиться к веб-сервису API ({client.BaseAddress}): служба недоступна";
            _logger.LogError(ex, "ошибка сети или недоступность api сервиса");
        }
        catch (Exception ex)
        {
            ErrorAlert = $"произошла непредвиденная ошибка: {ex.Message}";
            _logger.LogError(ex, "ошибка при обработке ответа api");
        }
    }
}