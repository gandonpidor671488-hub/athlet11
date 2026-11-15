namespace Athlet;
public partial class HomePage : ContentPage
{
    IDispatcherTimer? timer;
    int readyId;

    public HomePage()
    {
        InitializeComponent();
        Cats.ItemsSource = Store.CategoryTiles;
        Goals.ItemsSource = Store.GoalNames;
        Banners.ItemsSource = Store.Banners;
        Banners.IndicatorView = Dots;
    }

    protected override void OnDisappearing() { base.OnDisappearing(); timer?.Stop(); }

    void StartBanners()
    {
        timer ??= Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(4);
        timer.Tick -= OnTick; timer.Tick += OnTick;
        timer.Start();
    }
    void OnTick(object? s, EventArgs e)
    {
        int n = Store.Banners.Count;
        if (n > 1) Banners.Position = (Banners.Position + 1) % n;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Hello.Text = string.IsNullOrWhiteSpace(Store.ProfileName) ? "Добро пожаловать" : $"Привет, {Store.ProfileName}!";
        Hits.ItemsSource = await Store.Hits();
        Sales.ItemsSource = await Store.Sales();
        News.ItemsSource = await Store.News();
        var recent = await Store.Recent();
        Recent.ItemsSource = recent;
        RecentBox.IsVisible = recent.Count > 0;
        StartBanners();
        await ShowPickup();
    }

    async Task ShowPickup()
    {
        var ready = await Store.ReadyPickup();
        PickupCard.IsVisible = ready != null;
        if (ready == null) return;
        readyId = ready.Id;
        var lines = await Store.OrderLines(ready);
        if (lines.Count > 0)
        {
            PickImg.Source = lines[0].P.ImageSrc;
            PickWeb.Source = Uri.TryCreate(lines[0].P.RemoteSrc, UriKind.Absolute, out var u) ? ImageSource.FromUri(u) : null;
        }
        PickSub.Text = $"{ready.TitleText} · ул. Спортивная, 10";
        PickLast.Text = ready.StorageText;
    }

    async void OnPickup(object? s, TappedEventArgs e)
    {
        if (readyId > 0) await Shell.Current.GoToAsync($"pickup?id={readyId}");
    }

    async void OnSearch(object? s, EventArgs e)
    {
        Store.PendingQuery = Search.Text; Store.PendingCategory = "Все"; Search.Text = "";
        await Shell.Current.GoToAsync("//catalog");
    }
    async void OnCat(object? s, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CatTile t) return;
        Cats.SelectedItem = null; Store.PendingCategory = t.Name; Store.PendingQuery = null;
        await Shell.Current.GoToAsync("//catalog");
    }
    async void OnBanner(object? s, TappedEventArgs e)
    {
        var act = ((s as BindableObject)?.BindingContext as Banner)?.Action ?? "all";
        Store.PendingCategory = "Все"; Store.PendingQuery = null;
        if (act == "sale") Store.PendingSale = true;
        await Shell.Current.GoToAsync("//catalog");
    }
    async void OnGoal(object? s, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not string g) return;
        Goals.SelectedItem = null; Store.PendingCategory = "Цель:" + g; Store.PendingQuery = null;
        await Shell.Current.GoToAsync("//catalog");
    }
    async void OnAllHits(object? s, TappedEventArgs e)
    {
        Store.PendingCategory = "Все"; Store.PendingSort = 4;
        await Shell.Current.GoToAsync("//catalog");
    }
    async void OnSaleLink(object? s, TappedEventArgs e)
    {
        Store.PendingCategory = "Все"; Store.PendingSale = true;
        await Shell.Current.GoToAsync("//catalog");
    }
    async void OnFavs(object? s, TappedEventArgs e) => await Shell.Current.GoToAsync("//favorites");
    async void OnCopyPromo(object? s, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync("ATHLET10");
        PromoBtn.Text = "Скопировано ✓"; await Task.Delay(1500); PromoBtn.Text = "Копировать";
    }
}
