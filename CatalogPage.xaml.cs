namespace Athlet;
public partial class CatalogPage : ContentPage
{
    string cat = "Все";
    bool sale;

    public CatalogPage()
    {
        InitializeComponent();
        Cats.ItemsSource = new[] { "Все" }.Concat(Store.Categories).ToList();
        SortPicker.ItemsSource = Store.SortModes; SortPicker.SelectedIndex = 0;
        BrandPicker.ItemsSource = new[] { "Все бренды" }.Concat(Store.Brands).ToList(); BrandPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (Store.PendingCategory != null) cat = Store.PendingCategory;
        if (Store.PendingQuery != null) Search.Text = Store.PendingQuery;
        if (Store.PendingSale) { sale = true; SaleSwitch.IsToggled = true; Store.PendingSale = false; }
        if (Store.PendingSort > 0) { SortPicker.SelectedIndex = Store.PendingSort; Store.PendingSort = 0; }
        Store.PendingCategory = Store.PendingQuery = null;
        Cats.SelectedItem = cat;
        await Load();
    }

    async Task Load()
    {
        string? brand = BrandPicker.SelectedIndex > 0 ? Store.Brands[BrandPicker.SelectedIndex - 1] : null;
        var l = await Store.Products(cat, Search.Text, Math.Max(SortPicker.SelectedIndex, 0), brand, sale);
        List.ItemsSource = l;
        CountL.Text = $"Найдено: {l.Count}";
    }

    async void OnText(object? s, TextChangedEventArgs e) => await Load();
    async void OnFilter(object? s, EventArgs e) => await Load();
    async void OnSale(object? s, ToggledEventArgs e) { sale = e.Value; await Load(); }
    async void OnCat(object? s, SelectionChangedEventArgs e)
    {
        cat = e.CurrentSelection.FirstOrDefault() as string ?? "Все"; await Load();
    }
}
