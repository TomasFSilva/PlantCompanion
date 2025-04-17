namespace PlantCompanion;

public partial class PerfilPage : ContentPage
{
    public PerfilPage()
    {
        InitializeComponent();
    }

    private async void Send_to_Profile(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfileDetails());
    }
}