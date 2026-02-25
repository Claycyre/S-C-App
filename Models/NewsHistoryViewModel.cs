using Microsoft.AspNetCore.Mvc.Rendering;

namespace WeatherApp.Models;

public class NewsHistoryViewModel
{
    public DateOnly SelectedDate { get; set; }
    public IReadOnlyList<SelectListItem> AvailableDates { get; set; } = Array.Empty<SelectListItem>();
    public NewsSectionsViewModel Sections { get; set; } = new();
    public string? Message { get; set; }
}
