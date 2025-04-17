using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlantCompanion
{
    public partial class HistoricoPage : ContentPage
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly string _userId;

        public HistoricoPage(FirebaseClient firebaseClient, string userId)
        {
            InitializeComponent();
            _firebaseClient = firebaseClient;
            _userId = userId;
            LoadPlantHistory();
        }

        private async void LoadPlantHistory()
        {
            var plants = await GetPlantHistoryAsync();
            PlantsCollectionView.ItemsSource = plants;
        }

        private async Task<List<Plant>> GetPlantHistoryAsync()
        {
            var plants = await _firebaseClient
                .Child("users")
                .Child(_userId)
                .Child("plants")
                .OnceAsync<Plant>();

            return plants.Select(p => new Plant
            {
                Key = p.Key, // Adicionando a propriedade Key
                Name = p.Object.Name,
                HealthStatus = p.Object.HealthStatus,
                UserImageUrl = p.Object.UserImageUrl,
                DefaultImageUrl = p.Object.DefaultImageUrl
            }).ToList();
        }

        private async void OnDeleteButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var plant = button?.CommandParameter as Plant;
            if (plant != null)
            {
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("plants")
                    .Child(plant.Key)
                    .DeleteAsync();

                // Recarregar o histórico de plantas
                LoadPlantHistory();
            }
        }

        public class Plant
        {
            public string Key { get; set; } // Adicionando a propriedade Key
            public string Name { get; set; }
            public string HealthStatus { get; set; }
            public string UserImageUrl { get; set; }
            public string DefaultImageUrl { get; set; }
        }
    }
}