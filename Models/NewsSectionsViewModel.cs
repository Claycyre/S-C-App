namespace WeatherApp.Models;

public class NewsSectionsViewModel
{
    public IReadOnlyList<NewsItem> Tech { get; init; } = Array.Empty<NewsItem>();
    public IReadOnlyList<NewsItem> Politics { get; init; } = Array.Empty<NewsItem>();
    public IReadOnlyList<NewsItem> Sports { get; init; } = Array.Empty<NewsItem>();
    public IReadOnlyList<NewsItem> Business { get; init; } = Array.Empty<NewsItem>();

    public bool HasItems => Tech.Any() || Politics.Any() || Sports.Any() || Business.Any();
}
