using Microsoft.AspNetCore.Mvc.Rendering;

namespace WeatherApp.Models;

public class CitySunriseViewModel
{
    public string SelectedCity { get; set; } = "Vancouver";
    public List<SelectListItem> Cities { get; set; } = new();
    public List<SunriseSunset> SunData { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
