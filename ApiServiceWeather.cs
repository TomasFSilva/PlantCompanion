using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UtcTimeLibrary;

namespace PlantCompanion.Models
{
    public class City
    {
        public int id { get; set; }
        public string name { get; set; }
        public Coord coord { get; set; }
        public string country { get; set; }
        public int population { get; set; }
        public int timezone { get; set; }
        public int sunrise { get; set; }
        public int sunset { get; set; }
    }

    public class Clouds
    {
        public int all { get; set; }
    }

    public class Coord
    {
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class List
    {
        public int dt { get; set; }
        public string dateTime
        {
            get
            {
                var dateTime = DateTimeOffset.FromUnixTimeSeconds(dt).UtcDateTime;
                int hour = dateTime.Hour;

                switch (hour)
                {
                    case 0:
                        return "00H";
                    case 1:
                        return "01H";
                    case 2:
                        return "02H";
                    case 3:
                        return "03H";
                    case 4:
                        return "04H";
                    case 5:
                        return "05H";
                    case 6:
                        return "06H";
                    case 7:
                        return "07H";
                    case 8:
                        return "08H";
                    case 9:
                        return "09H";
                    case 10:
                        return "10H";
                    case 11:
                        return "11H";
                    case 12:
                        return "12H";
                    case 13:
                        return "13H";
                    case 14:
                        return "14H";
                    case 15:
                        return "15H";
                    case 16:
                        return "16H";
                    case 17:
                        return "17H";
                    case 18:
                        return "18H";
                    case 19:
                        return "19H";
                    case 20:
                        return "20H";
                    case 21:
                        return "21H";
                    case 22:
                        return "22H";
                    case 23:
                        return "23H";
                    case 24:
                        return "24H";
                    default:
                        return hour.ToString("00") + "H";
                }
            }
        }
        public Main main { get; set; }
        public List<Weather> weather { get; set; }
        public Clouds clouds { get; set; }
        public Wind wind { get; set; }
        public int visibility { get; set; }
        public double pop { get; set; }
        public Sys sys { get; set; }
        public string dt_txt { get; set; }
        public Rain rain { get; set; }
    }

    public class Main
    {
        public double temp { get; set; }
        public string temperature => Math.Round(temp).ToString() + "ºC";
        public double feels_like { get; set; }
        public double temp_min { get; set; }
        public double temp_max { get; set; }
        public int pressure { get; set; }
        public int sea_level { get; set; }
        public int grnd_level { get; set; }
        public int humidity { get; set; }
        public double temp_kf { get; set; }
    }

    public class Rain
    {
        [JsonProperty("3h")]
        public double _3h { get; set; }
    }

    public class Root
    {
        public string cod { get; set; }
        public int message { get; set; }
        public int cnt { get; set; }
        public List<List> list { get; set; }
        public City city { get; set; }
    }

    public class Sys
    {
        public string pod { get; set; }
    }

    public class Weather
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        //public string fullIconUrl => string.Format("https://openweathermap.org/img/wn/{0}@2x.png",icon);
        public string customIcon => string.Format("icon_{0}.png", icon);
    }

    public class Wind
    {
        public double speed { get; set; }
        public int deg { get; set; }
        public double gust { get; set; }
    }

    public static class ApiService
    {
        public static async Task<Root> GetWeather(double latitude, double longitude)
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&appid=4220fe8f426464f54c1e85a6d703d1de";
            var client = new HttpClient();
            var response = await client.GetStringAsync(url);
            var weatherData = JsonConvert.DeserializeObject<Root>(response);
            return weatherData;
        }

        public static async Task<Root> GetWeatherByCity(string city)
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&units=metric&appid=4220fe8f426464f54c1e85a6d703d1de";
            var client = new HttpClient();
            var response = await client.GetStringAsync(url);
            var weatherData = JsonConvert.DeserializeObject<Root>(response);
            return weatherData;
        }
    }
}
