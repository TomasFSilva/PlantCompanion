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
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using System.Text;

namespace PlantCompanion
{
    public partial class MainPage : ContentPage
    {
        private readonly string plantIdApiKey = "UXUnq8Jze9sG9VdzQVauJdDe9XSoCbQ2eAHP1fVBEf5nxGCYYD";
        private readonly string plantIdIdentifyUrl = "https://api.plant.id/v2/identify";
        private readonly string plantIdHealthUrl = "https://api.plant.id/v2/health_assessment";
        private FirebaseClient _firebaseClient;
        private IFirebaseAuthClient _authClient;

        public MainPage()
        {
            InitializeComponent();
            CheckAndRequestLocationPermission();
        }

        public void Initialize(IFirebaseAuthClient authClient, FirebaseClient firebaseClient)
        {
            _authClient = authClient;
            _firebaseClient = firebaseClient;
        }

        // Método para captura de foto
        private async void OnTakePhotoButtonClicked(object sender, EventArgs e)
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                var stream = await photo.OpenReadAsync();
                await IdentifyPlant(stream);
            }
        }

        // Método para upload de imagem
        private async void OnUploadButtonClicked(object sender, EventArgs e)
        {
            var fileResult = await MediaPicker.PickPhotoAsync();
            if (fileResult != null)
            {
                var stream = await fileResult.OpenReadAsync();
                await IdentifyPlant(stream);
            }
        }

        // Método para identificar a planta com base na imagem
        private async Task IdentifyPlant(Stream photoStream)
        {
            // Verificar se _authClient está null
            if (_authClient == null)
            {
                await DisplayAlert("Erro", "_authClient está null", "OK");
                return;
            }
            
            // Limpar o CarouselView e o ResultLabel antes de adicionar novos dados
            ImageCarousel.ItemsSource = null;
            ResultLabel.Text = string.Empty;
            HealthStatusLabel.Text = string.Empty;

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, plantIdIdentifyUrl);
            request.Headers.Add("Api-Key", plantIdApiKey);

            using (var memoryStream = new MemoryStream())
            {
                await photoStream.CopyToAsync(memoryStream);
                var base64Image = Convert.ToBase64String(memoryStream.ToArray());

                var jsonContent = new
                {
                    images = new[] { $"data:image/jpg;base64,{base64Image}" },
                    latitude = 49.207, // Use latitude from device if necessary
                    longitude = 16.608, // Use longitude from device if necessary
                    similar_images = true
                };

                var content = new StringContent(JsonSerializer.Serialize(jsonContent), System.Text.Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                await DisplayAlert("Resposta da API", result, "OK"); // Exibir a resposta da API

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var plantInfo = JsonSerializer.Deserialize<PlantInfo>(result);

                        if (plantInfo?.Suggestions != null && plantInfo.IsPlant)
                        {
                            var plantName = plantInfo.Suggestions.FirstOrDefault()?.PlantName ?? "Desconhecido";

                            ResultLabel.Text = $"Nome: {plantName}";

                            // Chamar a função AssessPlantHealth para obter os detalhes da saúde da planta
                            var healthAssessment = await AssessPlantHealth(base64Image, 49.207, 16.608);
                            HealthStatusLabel.Text = healthAssessment;

                            // Adicionar a imagem do upload ao CarouselView
                            var images = new List<string>();
                            var uploadImageUrl = plantInfo.Images?.FirstOrDefault()?.Url;
                            if (!string.IsNullOrEmpty(uploadImageUrl))
                            {
                                images.Add(uploadImageUrl);
                                await DisplayAlert("Imagem do Upload", $"URL da Imagem: {uploadImageUrl}", "OK");
                            }

                            // Buscar a imagem da planta usando a Google Search API
                            var plantImageUrl = await FetchPlantImageUrl(plantName);
                            if (!string.IsNullOrEmpty(plantImageUrl))
                            {
                                images.Add(plantImageUrl);
                                await DisplayAlert("Imagem da Pesquisa", $"URL da Imagem: {plantImageUrl}", "OK");
                            }

                            // Atualizar o CarouselView com as imagens
                            ImageCarousel.ItemsSource = images;
                        
                            await SavePlantInfoToFirebase(plantName, healthAssessment, uploadImageUrl, plantImageUrl);
                        }
                        else
                        {
                            await DisplayAlert("Sem Sugestões", "Nenhuma sugestão de planta foi encontrada.", "OK");
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

        private async Task<string> AssessPlantHealth(string base64Image, double latitude, double longitude)
        {
            using var client = new HttpClient();
            var jsonContent = new
            {
                api_key = plantIdApiKey,
                images = new[] { base64Image },
                latitude = latitude,
                longitude = longitude
            };

            var content = new StringContent(JsonSerializer.Serialize(jsonContent), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(plantIdHealthUrl, content);
                var result = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"🔥 Resposta da API de saúde:\n{result}");

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Erro ao obter avaliação da saúde. Código: {response.StatusCode}";
                }

                var healthInfo = JsonSerializer.Deserialize<PlantHealthApiResponse>(result);

                if (healthInfo?.HealthAssessment == null)
                {
                    return "⚠️ Não foi possível determinar a saúde da planta.";
                }

                if (healthInfo.HealthAssessment.IsHealthy)
                {
                    return $"✅ A planta parece saudável! (Confiança: {healthInfo.HealthAssessment.IsHealthyProbability:P2})";
                }

                if (healthInfo.HealthAssessment.Diseases != null && healthInfo.HealthAssessment.Diseases.Count > 0)
                {
                    string healthInfoText = "🚨 Problemas detectados:\n";
                    foreach (var disease in healthInfo.HealthAssessment.Diseases)
                    {
                        healthInfoText += $"- {disease.Name} (Probabilidade: {disease.Probability:P2})\n";
                    }
                    return healthInfoText;
                }

                return "⚠️ Não há informações suficientes sobre a saúde da planta.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro ao processar resposta da saúde: {ex.Message}");
                return "Erro ao processar avaliação da saúde.";
            }
        }
        
        // Método para buscar a URL da imagem da planta usando a Google Search API
        private async Task<string> FetchPlantImageUrl(string plantName)
        {
            var apiKey = "AIzaSyB_5pTc2dywOeXg2gGb2cl1CrD1SZrtJhc"; // Sua chave de API
            var cx = "b4f2321f746c64db1"; // Substitua pelo seu ID do mecanismo de pesquisa
            var query = Uri.EscapeDataString(plantName);
            const string language = "PT-pt";

            var url = $"https://www.googleapis.com/customsearch/v1?q={query}&cx={cx}&searchType=image&key={apiKey}&hl={language}";

            var client = new HttpClient();
            var response = await client.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(result);
                    var firstImageResult = jsonDoc.RootElement.GetProperty("items").EnumerateArray().FirstOrDefault();
                    if (firstImageResult.ValueKind != JsonValueKind.Undefined)
                    {
                        return firstImageResult.GetProperty("link").GetString();
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

            return null;
        }

        // Método para salvar informações no Firebase
        private async Task SavePlantInfoToFirebase(string plantName, string healthStatus, string userImageUrl, string defaultImageUrl)
        {
            var user = _authClient.User;
            if (user != null)
            {
                var plant = new
                {
                    Name = plantName,
                    HealthStatus = healthStatus,
                    UserImageUrl = userImageUrl,
                    DefaultImageUrl = defaultImageUrl
                };

                await DisplayAlert("Debug", "Antes de salvar no Firebase", "OK");

                await _firebaseClient
                    .Child("users")
                    .Child(user.Uid)
                    .Child("plants")
                    .PostAsync(plant);

                await DisplayAlert("Debug", "Depois de salvar no Firebase", "OK");
            }
            else
            {
                await DisplayAlert("Erro", "Usuário não autenticado.", "OK");
            }
        }

        // Definição das classes para processar a resposta da API
        public class PlantInfo
        {
            [JsonPropertyName("suggestions")]
            public List<Suggestion> Suggestions { get; set; }
            [JsonPropertyName("is_plant")]
            public bool IsPlant { get; set; }
            [JsonPropertyName("is_plant_probability")]
            public double IsPlantProbability { get; set; }
            [JsonPropertyName("images")]
            public List<ImageInfo> Images { get; set; }
        }

        public class Suggestion
        {
            [JsonPropertyName("plant_name")]
            public string PlantName { get; set; }
        }

        public class ImageInfo
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public class PlantHealthApiResponse
        {
            [JsonPropertyName("health_assessment")]
            public HealthAssessment HealthAssessment { get; set; }
        }

        public class HealthAssessment
        {
            [JsonPropertyName("is_healthy")]
            public bool IsHealthy { get; set; }

            [JsonPropertyName("is_healthy_probability")]
            public double IsHealthyProbability { get; set; }

            [JsonPropertyName("diseases")]
            public List<DiseaseDetected> Diseases { get; set; }
        }

        public class DiseaseDetected
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("probability")]
            public double Probability { get; set; }
        }

        private async void CheckAndRequestLocationPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                // Permissão concedida, você pode acessar a localização
                await GetLocationAsync();
            }
            else
            {
                // Permissão negada, notifique o usuário
                await DisplayAlert("Permissão Negada", "Não foi possível acessar a localização. Por favor, habilite os serviços de localização.", "OK");
            }
        }

        private async Task GetLocationAsync()
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();
                if (location != null)
                {
                    // Use a localização
                    Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}");
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
                await DisplayAlert("Erro", "Serviço de localização não suportado no dispositivo.", "OK");
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Handle not enabled on device exception
                await DisplayAlert("Serviços de Localização Desativados", "Por favor, habilite os serviços de localização no seu dispositivo.", "OK");
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
                await DisplayAlert("Permissão Negada", "Permissão de localização negada. Por favor, conceda permissão para acessar a localização.", "OK");
            }
            catch (Exception ex)
            {
                // Unable to get location
                await DisplayAlert("Erro", "Não foi possível obter a localização. Tente novamente mais tarde.", "OK");
            }
        }

        private async void OnHistoricoButtonClicked(object sender, EventArgs e)
        {
            var user = _authClient.User;
            if (user != null)
            {
                await Navigation.PushAsync(new HistoricoPage(_firebaseClient, user.Uid));
            }
            else
            {
                await DisplayAlert("Erro", "Usuário não autenticado.", "OK");
            }
        }

    }
}