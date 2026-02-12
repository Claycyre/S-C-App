using WeatherApp.Models;

namespace Weather.Models;

public class CurrentWeather

{
    public Location Location { get; set; }
    public Current Current { get; set; }
    public string Date { get; set; }
}
