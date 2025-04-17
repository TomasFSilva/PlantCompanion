using Firebase.Auth;
using Firebase.Database;

namespace PlantCompanion
{
    public partial class AppShell : Shell
    {
        private readonly IFirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseClient;

        public AppShell(IFirebaseAuthClient authClient, FirebaseClient firebaseClient)
        {
            InitializeComponent();
            _authClient = authClient;
            _firebaseClient = firebaseClient;

            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(TempoPage), typeof(TempoPage));
            Routing.RegisterRoute(nameof(PerfilPage), typeof(PerfilPage));
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
            if (CurrentPage is MainPage mainPage)
            {
                mainPage.Initialize(_authClient, _firebaseClient);
            }
        }
    }
}