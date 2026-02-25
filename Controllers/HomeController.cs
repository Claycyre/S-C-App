using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using Weather.Models;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICbcNewsService _newsService;

        public HomeController(ILogger<HomeController> logger, ICbcNewsService newsService)
        {
            _logger = logger;
            _newsService = newsService;
            ApiHelper.InitializeClient();
        }

        [HttpGet]
        public IActionResult Index(string? selectedCity)
        {
            const string apiKey = "25e80cc48b4d4e27b82184008251811";
            const string defaultCity = "Vancouver";

            var cityList = GetCanadianCityList();
            var citySet = new HashSet<string>(cityList, StringComparer.OrdinalIgnoreCase);

            selectedCity = string.IsNullOrWhiteSpace(selectedCity) ? defaultCity : selectedCity.Trim();
            if (!citySet.Contains(selectedCity))
            {
                selectedCity = defaultCity;
            }

            var vm = new CityWeatherViewModel
            {
                SelectedCity = selectedCity,
                Cities = cityList
                    .Select(c => new SelectListItem
                    {
                        Value = c,
                        Text = c,
                        Selected = string.Equals(c, selectedCity, StringComparison.OrdinalIgnoreCase)
                    })
                    .ToList()
            };

            try
            {
                string url = $"https://api.weatherapi.com/v1/current.json?key={apiKey}&q={Uri.EscapeDataString(selectedCity)}&aqi=yes";
                vm.Weather = ApiHelper.GetWeatherData(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load weather for city '{City}'", selectedCity);
                vm.ErrorMessage = "Could not load weather for the selected city right now. Please try again.";

                string fallbackUrl = $"https://api.weatherapi.com/v1/current.json?key={apiKey}&q={Uri.EscapeDataString(defaultCity)}&aqi=yes";
                vm.SelectedCity = defaultCity;
                vm.Weather = ApiHelper.GetWeatherData(fallbackUrl);
                foreach (var item in vm.Cities)
                {
                    item.Selected = string.Equals(item.Value, defaultCity, StringComparison.OrdinalIgnoreCase);
                }
            }

            return View("Index", vm);
        }

        [HttpGet]
        public IActionResult SunriseSunset(string? selectedCity)
        {
            const string apiKey = "25e80cc48b4d4e27b82184008251811";
            const string defaultCity = "Vancouver";

            var cityList = GetCanadianCityList();
            var citySet = new HashSet<string>(cityList, StringComparer.OrdinalIgnoreCase);

            selectedCity = string.IsNullOrWhiteSpace(selectedCity) ? defaultCity : selectedCity.Trim();
            if (!citySet.Contains(selectedCity))
            {
                selectedCity = defaultCity;
            }

            var vm = new CitySunriseViewModel
            {
                SelectedCity = selectedCity,
                Cities = cityList
                    .Select(c => new SelectListItem
                    {
                        Value = c,
                        Text = c,
                        Selected = string.Equals(c, selectedCity, StringComparison.OrdinalIgnoreCase)
                    })
                    .ToList(),
                SunData = new List<SunriseSunset>()
            };

            try
            {
                string weatherUrl = $"https://api.weatherapi.com/v1/current.json?key={apiKey}&q={Uri.EscapeDataString(selectedCity)}&aqi=no";
                CurrentWeather weather = ApiHelper.GetWeatherData(weatherUrl);
                double lat = weather.Location.lat;
                double lng = weather.Location.lon;

                for (int day = 0; day < 3; day++)
                {
                    DateTime date = DateTime.Today.AddDays(day);
                    string formattedDate = date.ToString("yyyy-MM-dd");

                    string url = $"https://api.sunrise-sunset.org/json?lat={lat}&lng={lng}&date={formattedDate}";
                    SunriseSunset data = ApiHelper.GetSunriseSunsetData(url);
                    data.Date = date.ToString("dddd, dd MMMM yyyy");
                    data.City = selectedCity;
                    vm.SunData.Add(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sunrise/sunset for city '{City}'", selectedCity);
                vm.ErrorMessage = "Could not load sunrise/sunset for the selected city right now. Please try again.";
            }

            return View("SunriseSunset", vm);
        }

        [HttpGet]
        public async Task<IActionResult> NewsHistory(string? selectedDate)
        {
            var availableDates = _newsService.GetHistoryDates(10);
            var selectedArchiveDate = ResolveSelectedArchiveDate(selectedDate, availableDates);
            var archivedSections = await _newsService.GetArchivedSectionsAsync(selectedArchiveDate, HttpContext.RequestAborted);

            var vm = new NewsHistoryViewModel
            {
                SelectedDate = selectedArchiveDate,
                AvailableDates = availableDates
                    .Select(date => new SelectListItem
                    {
                        Value = date.ToString("yyyy-MM-dd"),
                        Text = date.ToString("dddd, dd MMMM yyyy"),
                        Selected = date == selectedArchiveDate
                    })
                    .ToList(),
                Sections = archivedSections ?? new NewsSectionsViewModel()
            };

            if (archivedSections is null || !archivedSections.HasItems)
            {
                vm.Message = "No saved news was found for this date yet. Make sure S3 is configured, then open the main page daily so each day is archived automatically.";
            }

            return View("NewsHistory", vm);
        }

        private static DateOnly ResolveSelectedArchiveDate(string? selectedDate, IReadOnlyList<DateOnly> availableDates)
        {
            if (DateOnly.TryParse(selectedDate, out var parsed) && availableDates.Contains(parsed))
            {
                return parsed;
            }

            return availableDates.First();
        }

        private static List<string> GetCanadianCityList() => new()
        {
            "Toronto",
            "Montreal",
            "Vancouver",
            "Calgary",
            "Edmonton",
            "Ottawa",
            "Winnipeg",
            "Quebec City",
            "Hamilton",
            "Kitchener",
            "London",
            "Victoria",
            "Halifax",
            "Oshawa",
            "Windsor",
            "Saskatoon",
            "Regina",
            "St. John's",
            "Barrie",
            "Sherbrooke",
            "Kelowna",
            "Abbotsford",
            "Sudbury",
            "Kingston",
            "Saguenay",
            "Trois-Rivières",
            "Guelph",
            "Moncton",
            "Thunder Bay",
            "Red Deer"
        };

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
