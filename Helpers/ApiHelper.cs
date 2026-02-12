using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Weather.Models;
using WeatherApp.Models;

namespace WeatherApp.Helpers
{
    public static class ApiHelper
    {
        public static HttpClient ApiClient { get; set; }
        public static void InitializeClient()
        {
            ApiClient = new HttpClient();
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public static CurrentWeather GetWeatherData(string url)
        {
            using (HttpResponseMessage response = ApiClient.GetAsync(url).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    string json = response.Content.ReadAsStringAsync().Result;



                    var options = new JsonSerializerOptions

                    {

                        PropertyNameCaseInsensitive = true

                    };



                    return JsonSerializer.Deserialize<CurrentWeather>(json, options)

                                                           ?? new CurrentWeather();
                }

                else
                {
                    // Handle non-success status codes 
                    throw new HttpRequestException($"Request failed with status " +
                                                   $"code: {response.StatusCode}");
                }
            }
        }

        public static SunriseSunset GetSunriseSunsetData(string url)
        {
            HttpResponseMessage response = ApiClient.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                return JsonSerializer.Deserialize<SunriseSunset>(json) ?? new SunriseSunset();
            }
            else
            {
                throw new HttpRequestException(
                    $"Request failed with status code: {response.StatusCode}");
            }
        }

        public static SunriseSunset SunriseSunset(string url)
        {
            using (HttpResponseMessage response = ApiClient.GetAsync(url).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    string json = response.Content.ReadAsStringAsync().Result;



                    var options = new JsonSerializerOptions

                    {

                        PropertyNameCaseInsensitive = true

                    };



                    return JsonSerializer.Deserialize<SunriseSunset>(json, options)

                                                           ?? new SunriseSunset();
                }

                else
                {
                    // Handle non-success status codes 
                    throw new HttpRequestException($"Request failed with status " +
                                                   $"code: {response.StatusCode}");
                }
            }
        }
    }
}