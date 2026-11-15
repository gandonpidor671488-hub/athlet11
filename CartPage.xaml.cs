namespace Athlet;
public partial class CartPage : ContentPage
{
    int subtotal;
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
        var l = await Store.Cart(); List.ItemsSource = l;
        subtotal = l.Sum(x => x.Sum);
        if (Store.PromoCode != null && Store.DiscountFor(subtotal) == 0) Store.PromoCode = null;
        int disc = Store.DiscountFor(subtotal);
        int saving = l.Sum(x => x.Saving);
        int total = subtotal - disc;

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

        int count = l.Sum(x => x.Qty);
        CheckL.Text = $"К оформлению · {count}";
        CheckSum.Text = $"{total:N0} ₽";
        Checkout.IsEnabled = l.Count > 0;
        AddrL.Text = string.IsNullOrWhiteSpace(Store.ProfileAddress) ? "Укажите при оформлении заказа" : Store.ProfileAddress;
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

    async void OnClear(object? s, EventArgs e)
    {
        var lines = await Store.Cart();
        if (lines.Count == 0) return;
        if (!await DisplayAlert("Очистить корзину?", "Все товары будут удалены из корзины.", "Очистить", "Отмена")) return;
        foreach (var l in lines) await Store.SetQty(l.Item, 0);
        Store.PromoCode = null; PromoEntry.Text = "";
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

    async void OnCheckout(object? s, EventArgs e) => await Shell.Current.GoToAsync("checkout");
    async void OnCatalog(object? s, EventArgs e) => await Shell.Current.GoToAsync("//catalog");
}
