using Firebase.Auth;
using Firebase.Database;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Firebase.Database.Query;
using System.Net.Http;
using Newtonsoft.Json;

namespace PlantCompanion
{
    public partial class RegisterPage : ContentPage
    {
        private const string AbstractApiKey = "a4719d1bb91341dfb4149f32f8526f75";
        private readonly IFirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseClient;
        private string checkEmail = "sim"; // VERIFICA SE O EMAIL EXISTE
        //private string checkEmail = "nao"; // NAO VERIFICA SE O EMAIL EXISTE

        public RegisterPage(IFirebaseAuthClient authClient, FirebaseClient firebaseClient)
        {
            InitializeComponent();
            _authClient = authClient;
            _firebaseClient = firebaseClient;
        }

        private async void OnRegisterButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var name = NameEntry.Text;
                var email = EmailEntry.Text;
                var password = PasswordEntry.Text;
                var confirmPassword = ConfirmPasswordEntry.Text;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    WarningLabel.Text = "Todos os campos são obrigatórios.";
                    WarningLabel.IsVisible = true;
                    return;
                }

                if (password.Length < 6)
                {
                    WarningLabel.Text = "A password deve ter pelo menos 6 caracteres.";
                    WarningLabel.IsVisible = true;
                    return;
                }

                if (password != confirmPassword)
                {
                    WarningLabel.Text = "As passwords não coincidem.";
                    WarningLabel.IsVisible = true;
                    return;
                }

                if (checkEmail == "sim")
                {
                    var emailIsValid = await VerifyEmailAddress(email);
                    if (!emailIsValid)
                    {
                        await DisplayAlert("Erro", "Endereço de email inválido, possivelmente não existe.", "OK");
                        return;
                    }
                }

                var userCredential = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password);

                // Salvar o nome do usuário no Firebase Realtime Database
                await SaveUserName(userCredential.User.Uid, name);

                await DisplayAlert("Success", "Registro bem-sucedido!", "OK");
                // Navegar para a página de login ou principal
                Application.Current.MainPage = new NavigationPage(new LoginPage(_authClient, _firebaseClient));
            }
            catch (Exception ex)
            {
                WarningLabel.Text = "Erro ao registrar: " + ex.Message;
                WarningLabel.IsVisible = true;
            }
        }

        private async Task<bool> VerifyEmailAddress(string email)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var url = $"https://emailvalidation.abstractapi.com/v1/?api_key={AbstractApiKey}&email={email}";
                    var response = await client.GetAsync(url);
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(jsonResponse);

                    return result.deliverability == "DELIVERABLE";
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task SaveUserName(string userId, string name)
        {
            var user = new { name = name };
            await _firebaseClient
                .Child("users")
                .Child(userId)
                .PutAsync(user);
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            WarningLabel.IsVisible = false;
        }
    }
}