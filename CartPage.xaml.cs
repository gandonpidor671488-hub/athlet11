namespace Athlet;
public partial class CartPage : ContentPage
{
    int subtotal;
    List<CartLine> all = new();
    public CartPage() { InitializeComponent(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Store.CartChanged += OnCartChanged;
        PromoEntry.Text = Store.PromoCode ?? "";
        PromoMsg.Text = "";
        await Reload();
    }
    protected override void OnDisappearing() { base.OnDisappearing(); Store.CartChanged -= OnCartChanged; }
    void OnCartChanged() => MainThread.BeginInvokeOnMainThread(async () => await Reload());

    async Task Reload()
    {
        all = await Store.Cart(); List.ItemsSource = all;
        var sel = all.Where(x => x.Selected).ToList();
        subtotal = sel.Sum(x => x.Sum);
        if (Store.PromoCode != null && Store.DiscountFor(subtotal) == 0) Store.PromoCode = null;
        int disc = Store.DiscountFor(subtotal);
        int saving = sel.Sum(x => x.Saving);
        int total = subtotal - disc;
        int count = sel.Sum(x => x.Qty);

        HeadRow.IsVisible = all.Count > 0;
        FooterBox.IsVisible = all.Count > 0;
        bool allSel = all.Count > 0 && all.All(x => x.Selected);
        AllMark.Text = allSel ? "✓" : "";
        AllBox.BackgroundColor = allSel ? Color.FromArgb("#FF6A2B") : Colors.Transparent;

        SubL.Text = $"{subtotal:N0} ₽";
        SaveRow.IsVisible = saving > 0; SaveL.Text = $"−{saving:N0} ₽";
        DiscRow.IsVisible = disc > 0; DiscL.Text = $"−{disc:N0} ₽";
        TotalL.Text = $"{total:N0} ₽";

        int left = Store.FreeFrom - subtotal;
        FreeL.Text = subtotal == 0 ? $"Бесплатная доставка от {Store.FreeFrom:N0} ₽"
            : left > 0 ? $"Добавьте ещё на {left:N0} ₽ для бесплатной доставки" : "Доставка бесплатная 🎉";
        FreeBar.Progress = Math.Min(1.0, (double)subtotal / Store.FreeFrom);

        var eta = DateTime.Now.AddDays(3);
        EtaL.Text = subtotal == 0 ? "" : $"Курьером до {eta:dd.MM}, самовывоз завтра · бонусов за заказ: {total * 5 / 100:N0}";

        CheckoutL.Text = sel.Count > 0 ? $"К оформлению · {count}" : "Выберите товары";
        CheckoutSum.Text = sel.Count > 0 ? $"{total:N0} ₽" : "";
        CheckoutBtn.Opacity = sel.Count > 0 ? 1 : 0.45;
    }

    static CartLine? Line(object? s) => (s as BindableObject)?.BindingContext as CartLine;
    async void OnPlus(object? s, EventArgs e) { if (Line(s) is CartLine l) { await Store.SetQty(l.Item, l.Qty + 1); await Reload(); } }
    async void OnMinus(object? s, EventArgs e) { if (Line(s) is CartLine l) { await Store.SetQty(l.Item, l.Qty - 1); await Reload(); } }
    async void OnDel(object? s, TappedEventArgs e) { if (Line(s) is CartLine l) { await Store.SetQty(l.Item, 0); await Reload(); } }
    async void OnToFav(object? s, TappedEventArgs e)
    {
        if (Line(s) is not CartLine l) return;
        await Store.AddFavorite(l.P.Id);
        await Store.SetQty(l.Item, 0);
        PromoMsg.Text = "Товар перенесён в избранное"; PromoMsg.TextColor = Color.FromArgb("#2ECC71");
        await Reload();
    }

    async void OnCheck(object? s, TappedEventArgs e)
    {
        if (Line(s) is not CartLine l) return;
        if (!Store.Unselected.Remove(l.Item.Id)) Store.Unselected.Add(l.Item.Id);
        await Reload();
    }

    async void OnAll(object? s, TappedEventArgs e)
    {
        if (all.Count == 0) return;
        if (all.All(x => x.Selected)) foreach (var l in all) Store.Unselected.Add(l.Item.Id);
        else Store.Unselected.Clear();
        await Reload();
    }

    /// <summary>Корзина справа сверху: удаляет выбранные товары.</summary>
    async void OnClear(object? s, TappedEventArgs e)
    {
        var sel = all.Where(x => x.Selected).ToList();
        if (sel.Count == 0) return;
        if (!await DisplayAlert("Удалить выбранные?", "Выбранные товары будут удалены из корзины.", "Удалить", "Отмена")) return;
        foreach (var l in sel) { await Store.SetQty(l.Item, 0); Store.Unselected.Remove(l.Item.Id); }
        if (all.Count == sel.Count) { Store.PromoCode = null; PromoEntry.Text = ""; }
        await Reload();
    }

    async void OnPromo(object? s, EventArgs e) => await ApplyCode(PromoEntry.Text);
    async void OnChip(object? s, TappedEventArgs e)
    {
        var code = ((s as Border)?.Content as Label)?.Text;
        if (string.IsNullOrEmpty(code)) return;
        PromoEntry.Text = code;
        await ApplyCode(code);
    }
    async Task ApplyCode(string? code)
    {
        var (ok, msg) = Store.ApplyPromo(code, subtotal);
        PromoMsg.Text = msg;
        PromoMsg.TextColor = ok ? Color.FromArgb("#2ECC71") : Color.FromArgb("#FF6B6B");
        await Reload();
    }

    async void OnCheckout(object? s, TappedEventArgs e)
    {
        if (!all.Any(x => x.Selected)) return;
        await Shell.Current.GoToAsync("checkout");
    }
    async void OnCatalog(object? s, EventArgs e) => await Shell.Current.GoToAsync("//catalog");
}
