namespace WeatherApp.Models;

public class NewsItem
{
    public string Title { get; init; } = "";
    public string Link { get; init; } = "";
    public string Summary { get; init; } = "";
    public DateTimeOffset? Published { get; init; }
}
