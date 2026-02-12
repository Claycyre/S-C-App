namespace WeatherApp.Models

{
    public class SunriseSunset
    {
        public Results results { get; set; } 
        public string Date { get; set; }
        public string City { get; set; }
        public string status { get; set; }
        public string DayLength { get; set; }
    }
}