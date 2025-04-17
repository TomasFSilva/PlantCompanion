using System.Threading.Tasks;
using PlantCompanion.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using UtcTimeLibrary;
using Microsoft.Maui.ApplicationModel;

namespace PlantCompanion;

public partial class TempoPage : ContentPage
{
    public List<Models.List> WeatherList;
    public double latitude;
    public double longitude;

    public TempoPage()
    {
        InitializeComponent();
        WeatherList = new List<Models.List>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckAndRequestLocationPermission();
        await GetLocation();
        await GetWeatherDataByLocation(latitude, longitude);
    }

    private async Task CheckAndRequestLocationPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permissão Negada", "Não foi possível acessar a localização. Por favor, habilite os serviços de localização.", "OK");
        }
    }

    public async Task GetLocation()
    {
        try
        {
            var location = await Geolocation.GetLocationAsync();
            if (location != null)
            {
                latitude = location.Latitude;
                longitude = location.Longitude;
            }
        }
        catch (FeatureNotSupportedException fnsEx)
        {
            // Handle not supported on device exception
        }
        catch (FeatureNotEnabledException fneEx)
        {
            // Handle not enabled on device exception
            await DisplayAlert("Serviços de Localização Desativados", "Por favor, habilite os serviços de localização no seu dispositivo.", "OK");
        }
        catch (PermissionException pEx)
        {
            // Handle permission exception
        }
        catch (Exception ex)
        {
            // Unable to get location
        }
    }

    private async void TapLocation_Tapped(object sender, EventArgs e)
    {
        await CheckAndRequestLocationPermission();
        await GetLocation();
        await GetWeatherDataByLocation(latitude, longitude);
    }

    public async Task GetWeatherDataByLocation(double latitude, double longitude)
    {
        var result = await ApiService.GetWeather(latitude, longitude);
        UpdateUI(result);
    }

    private async void ImageButton_Clicked(object sender, EventArgs e)
    {
        var response = await DisplayPromptAsync(title: "", message: "", placeholder: "Search Weather by City", accept: "Search", cancel: "Cancel");
        if (response != null)
        {
            await GetWeatherDataByCity(response);
        }
    }

    public async Task GetWeatherDataByCity(string city)
    {
        var result = await ApiService.GetWeatherByCity(city);
        UpdateUI(result);
    }

    public void UpdateUI(dynamic result)
    {
        foreach (var item in result.list)
        {
            WeatherList.Add(item);
        }
        CvWeather.ItemsSource = WeatherList;

        LblCity.Text = result.city.name;
        LblWeatherDescription.Text = result.list[0].weather[0].description;
        LblTemperature.Text = result.list[0].main.temperature; //+ "ºC";
        LblHumidity.Text = result.list[0].main.humidity + "%";
        LblWind.Text = result.list[0].wind.speed + "km/h";
        //ImgWeatherIcon.Source = result.list[0].weather[0].fullIconUrl;
        ImgWeatherIcon.Source = result.list[0].weather[0].customIcon;
    }

    private async void OnNavigateToMainPageClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage());
    }
}

/*
URLS
https://openweathermap.org/api
https://openweathermap.org/forecast5#name5
https://home.openweathermap.org/api_keys
https://api.openweathermap.org/data/2.5/forecast?lat=38.768951&lon=-9.158940&appid=4220fe8f426464f54c1e85a6d703d1de
https://jsonformatter.org/
https://json2csharp.com/
https://openweathermap.org/weather-conditions
https://openweathermap.org/img/wn/04d@2x.png
*/