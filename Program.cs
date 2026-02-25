using Amazon;
using Amazon.S3;
using WeatherApp.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

builder.Services.Configure<S3NewsArchiveOptions>(builder.Configuration.GetSection("S3NewsArchive"));
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var options = new S3NewsArchiveOptions();
    builder.Configuration.GetSection("S3NewsArchive").Bind(options);

    var config = new AmazonS3Config();
    if (!string.IsNullOrWhiteSpace(options.Region))
    {
        config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
    }

    return new AmazonS3Client(config);
});

builder.Services.AddHttpClient<WeatherApp.Services.ICbcNewsService, WeatherApp.Services.CbcNewsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("S-C-App/1.0 (ASP.NET Core MVC; CBC RSS Reader)");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
