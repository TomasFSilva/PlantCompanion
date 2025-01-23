using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PlantCompanion
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnTakePhotoButtonClicked(object sender, EventArgs e)
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                var stream = await photo.OpenReadAsync();

                // Exibir a foto na interface dentro do quadrado
                PlantImage.Source = ImageSource.FromStream(() => stream);

                await IdentifyPlant(stream);
            }
        }

        private async Task IdentifyPlant(Stream photoStream)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.plant.id/v2/identify");
            request.Headers.Add("Api-Key", "IVPhMFZTJsg6VBWJbWu2bqg1Exv84dNIIMt9AZ2wqUMjKAUDCj");

            // Convert the photo stream to a base64 string
            using (var memoryStream = new MemoryStream())
            {
                await photoStream.CopyToAsync(memoryStream);
                var base64Image = Convert.ToBase64String(memoryStream.ToArray());

                var jsonContent = new
                {
                    images = new[] { $"data:image/jpg;base64,{base64Image}" },
                    latitude = 49.207, // Use latitude from device if necessary
                    longitude = 16.608, // Use longitude from device if necessary
                    similar_images = true // Enable similar images for better results
                };

                var content = new StringContent(JsonSerializer.Serialize(jsonContent), System.Text.Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var plantInfo = JsonSerializer.Deserialize<PlantInfo>(result);

                        if (plantInfo?.Suggestions != null && plantInfo.IsPlant)
                        {
                            // Get plant name and probability of being healthy
                            var plantName = plantInfo.Suggestions.FirstOrDefault()?.PlantName ?? "Desconhecido";
                            var healthStatus = plantInfo.IsPlant ? "Saudável" : "Doente";
                            var plantProbability = plantInfo.IsPlantProbability;

                            // Check if the plant is likely to be healthy or not based on the probability
                            if (plantProbability < 0.6) // Adjust the threshold as necessary
                            {
                                healthStatus = "Provavelmente Doente";
                            }

                            ResultLabel.Text = $"Nome: {plantName}, Saúde: {healthStatus} (Probabilidade: {plantProbability * 100}%)";
                        }
                        else
                        {
                            ResultLabel.Text = "Informações da planta não disponíveis.";
                        }
                    }
                    catch (JsonException ex)
                    {
                        await DisplayAlert("Erro", $"Erro ao desserializar JSON: {ex.Message}", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Erro", $"Erro na solicitação: {response.StatusCode}", "OK");
                }
            }
        }

        public class PlantInfo
        {
            [JsonPropertyName("suggestions")]
            public List<Suggestion> Suggestions { get; set; }
            [JsonPropertyName("is_plant")]
            public bool IsPlant { get; set; }
            [JsonPropertyName("is_plant_probability")]
            public double IsPlantProbability { get; set; }
            [JsonPropertyName("meta_data")]
            public MetaData MetaData { get; set; }
        }

        public class Suggestion
        {
            [JsonPropertyName("plant_name")]
            public string PlantName { get; set; }
            [JsonPropertyName("probability")]
            public double Probability { get; set; }
            [JsonPropertyName("plant_details")]
            public PlantDetails PlantDetails { get; set; }
        }

        public class PlantDetails
        {
            [JsonPropertyName("scientific_name")]
            public string ScientificName { get; set; }
            [JsonPropertyName("structured_name")]
            public StructuredName StructuredName { get; set; }
        }

        public class StructuredName
        {
            [JsonPropertyName("genus")]
            public string Genus { get; set; }
            [JsonPropertyName("species")]
            public string Species { get; set; }
        }

        public class MetaData
        {
            [JsonPropertyName("latitude")]
            public double Latitude { get; set; }
            [JsonPropertyName("longitude")]
            public double Longitude { get; set; }
            [JsonPropertyName("date")]
            public string Date { get; set; }
            [JsonPropertyName("datetime")]
            public string DateTime { get; set; }
        }
    }
}
