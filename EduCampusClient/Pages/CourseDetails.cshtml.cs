using System.Text.Json;
using EduCampusClient.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduCampusClient.Pages;

public class CourseDetailsModel : PageModel
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<CourseDetailsModel> _logger;

    public CourseDetailsModel(IHttpClientFactory clientFactory, ILogger<CourseDetailsModel> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public CourseDetailItem? Course { get; set; }
    public string? ErrorAlert { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = _clientFactory.CreateClient("CampusApiClient");

        try
        {
            var response = await client.GetAsync($"api/v2/courses/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var envelope = JsonSerializer.Deserialize<ApiEnvelope<CourseDetailItem>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (envelope != null && envelope.IsSuccess && envelope.Result != null)
                {
                    Course = envelope.Result;
                    _logger.LogInformation("успешно загружена v2 карточка курса {Id} через http клиент", id);
                    return Page();
                }

                ErrorAlert = envelope?.ErrorMessage ?? "курс не найден";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ErrorAlert = $"курс с номером #{id} не найден в базе данных (404 Not Found)";
            }
            else
            {
                ErrorAlert = $"сервер вернул статус ошибки: {(int)response.StatusCode}";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorAlert = $"не удалось соединиться с веб-сервисом: {ex.Message}";
            _logger.LogError(ex, "ошибка сети при вызове api");
        }
        catch (Exception ex)
        {
            ErrorAlert = $"непредвиденная ошибка: {ex.Message}";
        }

        return Page();
    }
}