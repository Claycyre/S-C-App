using Microsoft.AspNetCore.Mvc.Rendering;
using Weather.Models;

namespace WeatherApp.Models;

public class CityWeatherViewModel
{
    public string SelectedCity { get; set; } = "Vancouver";
    public List<SelectListItem> Cities { get; set; } = new();
    public CurrentWeather? Weather { get; set; }
    public string? ErrorMessage { get; set; }
}
