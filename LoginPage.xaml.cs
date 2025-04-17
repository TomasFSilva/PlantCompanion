using Firebase.Auth;
using Firebase.Database;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PlantCompanion
{
    public partial class LoginPage : ContentPage
    {
        private const string FirebaseApiKey = "AIzaSyBe3ZgX-UDJ6gN7oYLo9gRRL0Z83pGm9Uk";
        private readonly IFirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseClient;

        public LoginPage(IFirebaseAuthClient authClient, FirebaseClient firebaseClient)
        {
            InitializeComponent();
            _authClient = authClient;
            _firebaseClient = firebaseClient;
        }

        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var email = EmailEntry.Text;
                var password = PasswordEntry.Text;
                var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);
                WarningLabel.IsVisible = false;
                await DisplayAlert("Success", "Login bem-sucedido!", "OK");
                Application.Current.MainPage = new AppShell(_authClient, _firebaseClient);
            }
            catch (Exception ex)
            {
                WarningLabel.Text = GetErrorMessage(ex);
                WarningLabel.IsVisible = true;
            }
        }

        private async void OnRegisterButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage(_authClient, _firebaseClient));
        }

        private string GetErrorMessage(Exception ex)
        {
            if (ex is FirebaseAuthException authEx)
            {
                switch (authEx.Reason)
                {
                    case AuthErrorReason.UnknownEmailAddress:
                        return "Email não encontrado.";
                    case AuthErrorReason.WrongPassword:
                        return "Password incorreta.";
                    case AuthErrorReason.TooManyAttemptsTryLater:
                        return "Muitas tentativas. Tente novamente mais tarde.";
                    default:
                        return "Erro ao fazer login. Verifique suas credenciais.";
                }
            }
            return "Erro desconhecido. Tente novamente.";
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            WarningLabel.IsVisible = false;
        }

        private async void OnForgotPasswordTapped(object sender, EventArgs e)
        {
            try
            {
                var email = EmailEntry.Text;
                if (string.IsNullOrEmpty(email))
                {
                    await DisplayAlert("Erro", "Por favor, insira o seu endereço de email.", "OK");
                    return;
                }

                var success = await SendPasswordResetEmail(email);
                if (success)
                {
                    await DisplayAlert("Sucesso", "Email para redefinição da password enviado.", "OK");
                }
                else
                {
                    await DisplayAlert("Erro", "Falha ao enviar email de redefinição da password.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", ex.Message, "OK");
            }
        }

        private async Task<bool> SendPasswordResetEmail(string email)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var requestData = new
                    {
                        requestType = "PASSWORD_RESET",
                        email = email
                    };

                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={FirebaseApiKey}";
                    var response = await client.PostAsync(url, content);

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}