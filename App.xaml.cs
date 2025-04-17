using Firebase.Auth;
using Firebase.Database;
using Microsoft.Maui.Controls;

namespace PlantCompanion
{
    public partial class App : Application
    {
        private readonly IFirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseClient;

        public App(IFirebaseAuthClient authClient, FirebaseClient firebaseClient)
        {
            InitializeComponent();
            _authClient = authClient;
            _firebaseClient = firebaseClient;

            // Verificar se o usuário está autenticado
            var user = _authClient.User;
            if (user != null)
            {
                MainPage = new AppShell(_authClient, _firebaseClient);
            }
            else
            {
                MainPage = new NavigationPage(new LoginPage(_authClient, _firebaseClient));
            }
        }
    }
}