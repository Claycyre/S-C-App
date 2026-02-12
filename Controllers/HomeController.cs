using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Weather.Models;
using WeatherApp.Helpers;
using WeatherApp.Models;
using static System.Net.WebRequestMethods;

namespace WeatherApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            ApiHelper.InitializeClient();
        }

        public IActionResult Index()

        {
        string url = "http://api.weatherapi.com/v1/current.json?key=25e80cc48b4d4e27b82184008251811&q=Burnaby&aqi=yes";
        string url2 = "http://api.weatherapi.com/v1/current.json?key=25e80cc48b4d4e27b82184008251811&q=Vancouver&aqi=no";

        CurrentWeather weatherJson = ApiHelper.GetWeatherData(url);
        CurrentWeather weatherJson2 = ApiHelper.GetWeatherData(url2);

        var campuses = new List<CurrentWeather>
        {
            weatherJson,
            weatherJson2
        };

        return View("Index", campuses);
        }

        public IActionResult SunriseSunset()
        {
            var cities = new List<(string Name, double Lat, double Lng)>
            {
                ("Burnaby", 49.25, -122.95),
                ("Vancouver", 49.2827, -123.1207)
            };

            var sunDataList = new List<SunriseSunset>();

            for (int day = 0; day < 3; day++)
            {
                DateTime date = DateTime.Today.AddDays(day);
                string formattedDate = date.ToString("yyyy-MM-dd");

                foreach (var city in cities)
                {
                    string url = $"https://api.sunrise-sunset.org/json?lat={city.Lat}&lng={city.Lng}&date={formattedDate}";

                    SunriseSunset data = ApiHelper.GetSunriseSunsetData(url);

                    data.Date = date.ToString("dddd, dd MMMM yyyy");
                    data.City = city.Name;
                    sunDataList.Add(data);
                }

            }

            return View(sunDataList);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
