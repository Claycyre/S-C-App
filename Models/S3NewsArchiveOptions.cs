namespace WeatherApp.Models;

public class S3NewsArchiveOptions
{
    public bool Enabled { get; set; }
    public string BucketName { get; set; } = "";
    public string Region { get; set; } = "ca-central-1";
    public string Prefix { get; set; } = "news-history";
}
