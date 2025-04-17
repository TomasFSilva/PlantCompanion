using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PlantCompanion
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<IFirebaseAuthClient>(sp => new FirebaseAuthClient(new FirebaseAuthConfig()
            {
                ApiKey = "AIzaSyBe3ZgX-UDJ6gN7oYLo9gRRL0Z83pGm9Uk",
                AuthDomain = "plantcompanion-c4536.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
                {
                   new EmailProvider()
                }
            }));

            // Configurar o Firebase Database
            var firebaseClient = new FirebaseClient("https://plantcompanion-c4536-default-rtdb.europe-west1.firebasedatabase.app/");
            builder.Services.AddSingleton(firebaseClient);

            // Registrar a classe App como um serviço
            builder.Services.AddSingleton<App>(sp =>
            {
                var authClient = sp.GetRequiredService<IFirebaseAuthClient>();
                var firebaseClient = sp.GetRequiredService<FirebaseClient>();
                return new App(authClient, firebaseClient);
            });

            return builder.Build();
        }
    }
}