namespace Athlet;
public partial class ProductPage : ContentPage, IQueryAttributable
{
    Product? p;
    int qty = 1;

    public ProductPage()
    {
        InitializeComponent();
        StarsPicker.ItemsSource = new[] { "5 ★★★★★", "4 ★★★★", "3 ★★★", "2 ★★", "1 ★" };
        StarsPicker.SelectedIndex = 0;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> q)
    {
        if (q.TryGetValue("id", out var v) && int.TryParse(v?.ToString(), out var id)) await Show(id);
    }

    /// <summary>Показывает товар (вызывается и при переходе по «Похожим товарам»).</summary>
    public async Task Show(int id)
    {
        if (await Store.Get(id) is not Product x) return;
        p = x; qty = 1; QtyL.Text = "1";
        Title = x.Brand;
        Store.AddRecent(x.Id);
        Photo.Source = x.ImageSrc;
        PhotoWeb.Source = Web(x.RemoteSrc);
        BrandL.Text = x.Brand.ToUpper(); NameL.Text = x.Name;
        RatingL.Text = x.RatingText; ReviewsL.Text = x.ReviewsText; WeightL.Text = "· " + x.Weight;
        PriceL.Text = x.PriceText; OldL.Text = x.OldPriceText; SaveL.Text = x.SavingText;
        DiscBadge.IsVisible = x.HasDiscount; DiscL.Text = x.DiscountText;
        DescL.Text = x.Description; CompL.Text = x.Composition;
        NutriBox.IsVisible = x.Kcal > 0 || x.Protein > 0 || x.Carbs > 0 || x.Fat > 0;
        NutriTitle.Text = $"Пищевая ценность на порцию ({x.Serving})";
        KcalL.Text = x.KcalText; ProtL.Text = x.ProteinText; FatL.Text = x.FatText; CarbL.Text = x.CarbsText;
        var fl = x.FlavorList;
        Flavors.ItemsSource = fl;
        FlavorBox.IsVisible = fl.Count > 1;
        if (fl.Count > 0) Flavors.SelectedItem = fl[0];
        UpdateFav(); UpdateBuy();
        UrlE.Text = x.ImageUrl;
        await LoadReviews();
        Similar.ItemsSource = await Store.Similar(x);
        await Scroller.ScrollToAsync(0, 0, false);
    }

    async Task LoadReviews()
    {
        if (p == null) return;
        var l = await Store.ReviewsOf(p.Id);
        BindableLayout.SetItemsSource(RevList, l);
        RevTitle.Text = $"Отзывы ({p.Reviews})";
        ReviewsL.Text = p.ReviewsText; RatingL.Text = p.RatingText;
    }

    static ImageSource? Web(string? u) =>
        Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https") ? ImageSource.FromUri(uri) : null;

    async void OnShare(object? s, TappedEventArgs e)
    {
        if (p == null) return;
        await Share.Default.RequestAsync(new ShareTextRequest { Title = p.Name, Text = $"{p.Brand} {p.Name}, {p.PriceText}. Магазин ATHLET" });
    }

    void UpdateFav() { if (p == null) return; FavBtn.Text = p.FavIcon; FavBtn.TextColor = p.FavColor; }
    void UpdateBuy() { if (p != null) BuyBtn.Text = $"В корзину · {p.Price * qty:N0} ₽"; }

    async void OnFav(object? s, TappedEventArgs e) { if (p == null) return; await Store.ToggleFav(p); UpdateFav(); }
    void OnPlus(object? s, EventArgs e) { qty = Math.Min(qty + 1, 20); QtyL.Text = qty.ToString(); UpdateBuy(); }
    void OnMinus(object? s, EventArgs e) { qty = Math.Max(qty - 1, 1); QtyL.Text = qty.ToString(); UpdateBuy(); }

    async void OnAdd(object? s, EventArgs e)
    {
        if (p == null) return;
        string flavor = Flavors.SelectedItem as string ?? p.FlavorList.FirstOrDefault() ?? "";
        await Store.AddToCart(p.Id, flavor, qty);
        BuyBtn.Text = "✓ Добавлено в корзину"; await Task.Delay(1300); UpdateBuy();
    }

    async void OnReview(object? s, EventArgs e)
    {
        if (p == null) return;
        if (string.IsNullOrWhiteSpace(RevText.Text)) { RevMsg.Text = "Напишите комментарий"; return; }
        int stars = 5 - Math.Max(StarsPicker.SelectedIndex, 0);
        string author = string.IsNullOrWhiteSpace(Store.ProfileName) ? "Покупатель" : Store.ProfileName;
        await Store.AddReview(p, author, stars, RevText.Text.Trim());
        RevText.Text = ""; RevMsg.Text = "Спасибо! Отзыв добавлен";
        await LoadReviews();
    }

    async void OnUrl(object? s, EventArgs e)
    {
        if (p == null) return;
        string url = UrlE.Text?.Trim() ?? "";
        if (url != "" && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) { UrlE.Text = ""; return; }
        await Store.SetImageUrl(p.Id, url);
        p.ImageUrl = url;
        PhotoWeb.Source = Web(p.RemoteSrc);
    }
}
