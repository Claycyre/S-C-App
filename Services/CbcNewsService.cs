using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class CbcNewsService : ICbcNewsService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly IAmazonS3 _s3;
    private readonly ILogger<CbcNewsService> _logger;
    private readonly S3NewsArchiveOptions _archiveOptions;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    // Required feeds (as requested)
    private static readonly Dictionary<string, string> FeedUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tech"] = "https://rss.cbc.ca/lineup/technology.xml",
        ["politics"] = "https://rss.cbc.ca/lineup/politics.xml",
        ["sports"] = "https://rss.cbc.ca/lineup/sports.xml",
        ["business"] = "https://rss.cbc.ca/lineup/business.xml",
    };

    public CbcNewsService(
        HttpClient http,
        IMemoryCache cache,
        IAmazonS3 s3,
        ILogger<CbcNewsService> logger,
        IOptions<S3NewsArchiveOptions> archiveOptions)
    {
        _http = http;
        _cache = cache;
        _s3 = s3;
        _logger = logger;
        _archiveOptions = archiveOptions.Value;
    }

    public async Task<IReadOnlyList<NewsItem>> GetSectionAsync(string sectionKey, int take = 4, CancellationToken ct = default)
    {
        if (!FeedUrls.TryGetValue(sectionKey, out var url))
            return Array.Empty<NewsItem>();

        var cacheKey = $"cbc-rss:{sectionKey}:{take}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<NewsItem>? cached) && cached is not null)
            return cached;

        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
            var feed = SyndicationFeed.Load(reader);

            var items = (feed?.Items ?? Enumerable.Empty<SyndicationItem>())
                .Take(Math.Max(1, take))
                .Select(ToNewsItem)
                .Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Link))
                .ToList()
                .AsReadOnly();

            _cache.Set(cacheKey, items, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CBC RSS fetch failed for section {SectionKey}", sectionKey);
            return Array.Empty<NewsItem>();
        }
    }

    public async Task<NewsSectionsViewModel> GetCurrentSectionsAsync(int itemsPerSection = 4, CancellationToken ct = default)
    {
        var techTask = GetSectionAsync("tech", itemsPerSection, ct);
        var politicsTask = GetSectionAsync("politics", itemsPerSection, ct);
        var sportsTask = GetSectionAsync("sports", itemsPerSection, ct);
        var businessTask = GetSectionAsync("business", itemsPerSection, ct);

        await Task.WhenAll(techTask, politicsTask, sportsTask, businessTask);

        var vm = new NewsSectionsViewModel
        {
            Tech = await techTask,
            Politics = await politicsTask,
            Sports = await sportsTask,
            Business = await businessTask
        };

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (vm.HasItems)
        {
            await SaveSnapshotAsync(today, vm, ct);
            return vm;
        }

        var archivedCopy = await GetArchivedSectionsAsync(today, ct);
        return archivedCopy ?? vm;
    }

    public async Task<NewsSectionsViewModel?> GetArchivedSectionsAsync(DateOnly archiveDate, CancellationToken ct = default)
    {
        if (!IsArchiveEnabled)
            return null;

        var cacheKey = $"cbc-rss:archive:{archiveDate:yyyy-MM-dd}";
        if (_cache.TryGetValue(cacheKey, out NewsSectionsViewModel? cached) && cached is not null)
            return cached;

        try
        {
            var response = await _s3.GetObjectAsync(_archiveOptions.BucketName, BuildObjectKey(archiveDate), ct);
            await using var stream = response.ResponseStream;
            var snapshot = await JsonSerializer.DeserializeAsync<NewsArchiveSnapshot>(stream, JsonOptions, ct);

            if (snapshot?.Sections is null || !snapshot.Sections.HasItems)
                return null;

            _cache.Set(cacheKey, snapshot.Sections, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20)
            });

            return snapshot.Sections;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read archived news snapshot for {ArchiveDate}", archiveDate);
            return null;
        }
    }

    public IReadOnlyList<DateOnly> GetHistoryDates(int days = 10)
    {
        var totalDays = Math.Max(1, days);
        return Enumerable.Range(1, totalDays)
            .Select(offset => DateOnly.FromDateTime(DateTime.Today.AddDays(-offset)))
            .ToList();
    }

    private async Task SaveSnapshotAsync(DateOnly archiveDate, NewsSectionsViewModel sections, CancellationToken ct)
    {
        if (!IsArchiveEnabled)
            return;

        try
        {
            var snapshot = new NewsArchiveSnapshot
            {
                ArchiveDate = archiveDate.ToString("yyyy-MM-dd"),
                SavedAtUtc = DateTime.UtcNow,
                Sections = sections
            };

            var payload = JsonSerializer.Serialize(snapshot, JsonOptions);
            var key = BuildObjectKey(archiveDate);

            using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _archiveOptions.BucketName,
                Key = key,
                InputStream = contentStream,
                ContentType = "application/json"
            }, ct);

            await DeleteExpiredSnapshotsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to save archived news snapshot for {ArchiveDate}", archiveDate);
        }
    }

    private async Task DeleteExpiredSnapshotsAsync(CancellationToken ct)
    {
        if (!IsArchiveEnabled)
            return;

        try
        {
            var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));
            var keysToDelete = new List<KeyVersion>();
            string? continuationToken = null;

            do
            {
                var listResponse = await _s3.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _archiveOptions.BucketName,
                    Prefix = BuildPrefixWithSlash(),
                    ContinuationToken = continuationToken
                }, ct);

                foreach (var item in listResponse.S3Objects)
                {
                    var snapshotDate = TryExtractDateFromKey(item.Key);
                    if (snapshotDate is not null && snapshotDate.Value < cutoff)
                    {
                        keysToDelete.Add(new KeyVersion { Key = item.Key });
                    }
                }

                continuationToken = listResponse.IsTruncated ? listResponse.NextContinuationToken : null;
            }
            while (!string.IsNullOrWhiteSpace(continuationToken));

            if (keysToDelete.Count == 0)
                return;

            foreach (var batch in keysToDelete.Chunk(1000))
            {
                await _s3.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = _archiveOptions.BucketName,
                    Objects = batch.ToList()
                }, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to trim old news history snapshots from S3.");
        }
    }

    private bool IsArchiveEnabled => _archiveOptions.Enabled && !string.IsNullOrWhiteSpace(_archiveOptions.BucketName);

    private string BuildObjectKey(DateOnly date) => $"{BuildPrefixWithSlash()}{date:yyyy-MM-dd}.json";

    private string BuildPrefixWithSlash()
    {
        var prefix = string.IsNullOrWhiteSpace(_archiveOptions.Prefix) ? "news-history" : _archiveOptions.Prefix.Trim();
        return prefix.TrimEnd('/') + "/";
    }

    private static DateOnly? TryExtractDateFromKey(string key)
    {
        var fileName = Path.GetFileNameWithoutExtension(key);
        return DateOnly.TryParse(fileName, out var parsedDate) ? parsedDate : null;
    }

    private static NewsItem ToNewsItem(SyndicationItem item)
    {
        var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
        var summary = item.Summary?.Text ?? "";

        summary = NormalizeWhitespace(StripHtml(summary));
        summary = Truncate(summary, 140);

        DateTimeOffset? published = null;
        if (item.PublishDate != DateTimeOffset.MinValue)
            published = item.PublishDate;

        return new NewsItem
        {
            Title = NormalizeWhitespace(item.Title?.Text ?? ""),
            Link = link,
            Summary = summary,
            Published = published
        };
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var noTags = Regex.Replace(input, "<.*?>", string.Empty);
        noTags = noTags.Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&lt;", "<").Replace("&gt;", ">");
        return noTags;
    }

    private static string NormalizeWhitespace(string input)
        => Regex.Replace(input ?? "", "\\s+", " ").Trim();

    private static string Truncate(string input, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        if (input.Length <= maxChars) return input;
        return input[..Math.Max(0, maxChars - 1)].TrimEnd() + "…";
    }
}
