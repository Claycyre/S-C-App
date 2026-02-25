namespace WeatherApp.Models;

public class NewsArchiveSnapshot
{
    public string ArchiveDate { get; set; } = "";
    public DateTime SavedAtUtc { get; set; }
    public NewsSectionsViewModel Sections { get; set; } = new();
}
