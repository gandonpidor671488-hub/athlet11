namespace Athlet;
public partial class FavoritesPage : ContentPage
{
    public FavoritesPage() { InitializeComponent(); }
    protected override async void OnAppearing() { base.OnAppearing(); List.ItemsSource = await Store.Favorites(); }
    async void OnCatalog(object? s, EventArgs e) => await Shell.Current.GoToAsync("//catalog");
}
