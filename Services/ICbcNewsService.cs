using WeatherApp.Models;

namespace WeatherApp.Services;

public interface ICbcNewsService
{
    Task<IReadOnlyList<NewsItem>> GetSectionAsync(string sectionKey, int take = 4, CancellationToken ct = default);
    Task<NewsSectionsViewModel> GetCurrentSectionsAsync(int itemsPerSection = 4, CancellationToken ct = default);
    Task<NewsSectionsViewModel?> GetArchivedSectionsAsync(DateOnly archiveDate, CancellationToken ct = default);
    IReadOnlyList<DateOnly> GetHistoryDates(int days = 10);
}
