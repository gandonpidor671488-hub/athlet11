namespace Athlet;
public partial class CheckoutPage : ContentPage
{
    List<CartLine> lines = new();
    int subtotal, discount, balance, maxBonus;

    bool ready, suppress;
    public CheckoutPage() { InitializeComponent(); ready = true; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        lines = await Store.Cart();
        subtotal = lines.Sum(x => x.Sum);
        discount = Store.DiscountFor(subtotal);
        CountL.Text = $"{lines.Sum(l => l.Qty)} шт.";
        BindableLayout.SetItemsSource(Thumbs, lines);
        SubL.Text = $"{subtotal:N0} ₽";
        DiscRow.IsVisible = discount > 0; DiscL.Text = $"−{discount:N0} ₽";
        balance = await Store.BonusBalance();
        maxBonus = Math.Min(balance, (subtotal - discount) * 30 / 100);
        BonusSw.IsEnabled = maxBonus > 0;
        BonusInfo.Text = maxBonus > 0
            ? $"Баланс {balance:N0}. Можно списать {maxBonus:N0} ₽ (до 30% заказа)"
            : $"Баланс {balance:N0}. Бонусы появятся после первого заказа";
        if (string.IsNullOrEmpty(NameE.Text)) NameE.Text = Store.ProfileName;
        if (string.IsNullOrEmpty(Phone.Text)) Phone.Text = Store.ProfilePhone;
        if (string.IsNullOrEmpty(Address.Text)) { suppress = true; Address.Text = Store.ProfileAddress; suppress = false; }
        UpdateTotals();
    }

    int DeliveryCost => Store.DeliveryCost(subtotal, Delivery.IsChecked);
    int BonusUsed => BonusSw.IsToggled ? maxBonus : 0;

    void OnBonus(object? s, ToggledEventArgs e) { if (ready) UpdateTotals(); }

    void UpdateTotals()
    {
        int d = DeliveryCost, used = BonusUsed, total = subtotal - discount - used + d;
        BonusRow.IsVisible = used > 0; BonusL2.Text = $"−{used:N0} ₽";
        DelL.Text = Delivery.IsChecked ? (d == 0 ? "Бесплатно" : $"{d:N0} ₽") : "Самовывоз";
        TotalL.Text = $"{total:N0} ₽";
        ConfirmSum.Text = TotalL.Text;
        var eta = DateTime.Now.AddDays(Delivery.IsChecked ? 3 : 1);
        ItemsL.Text = Delivery.IsChecked ? $"{eta:d MMMM}, {(d == 0 ? "0 ₽" : $"{d:N0} ₽")}" : $"Завтра, {eta:d MMMM}, 0 ₽";
        BonusL.Text = $"Вы получите {total * 5 / 100:N0} бонусов";
    }

    void OnMethod(object? s, CheckedChangedEventArgs e)
    {
        if (!ready) return;
        AddrBox.IsVisible = Delivery.IsChecked; PickBox.IsVisible = !Delivery.IsChecked;
        if (!Delivery.IsChecked) SuggestBox.IsVisible = false;
        UpdateTotals();
    }

    // Подсказки: список регионов России при вводе адреса
    void OnAddrChanged(object? s, TextChangedEventArgs e)
    {
        if (!ready || suppress) return;
        AddrField.Stroke = Color.FromArgb("#262B35");
        var text = e.NewTextValue ?? "";
        var found = text.Contains(',') ? new List<string>() : Regions.Find(text);
        if (found.Count == 1 && found[0].Equals(text.Trim(), StringComparison.OrdinalIgnoreCase)) found.Clear();
        BindableLayout.SetItemsSource(SuggestList, found);
        SuggestBox.IsVisible = found.Count > 0;
    }

    void OnPickRegion(object? s, TappedEventArgs e)
    {
        if (s is not Label l || string.IsNullOrEmpty(l.Text)) return;
        suppress = true;
        Address.Text = l.Text + ", ";
        suppress = false;
        SuggestBox.IsVisible = false;
        Address.Focus();
        Address.CursorPosition = Address.Text.Length;
    }

    async void OnConfirm(object? s, EventArgs e)
    {
        var digits = new string((Phone.Text ?? "").Where(char.IsDigit).ToArray());
        if (lines.Count == 0) { Err.Text = "Корзина пуста"; return; }
        if (string.IsNullOrWhiteSpace(NameE.Text)) { Err.Text = "Введите имя"; return; }
        if (digits.Length < 10) { Err.Text = "Введите корректный телефон"; return; }
        if (Delivery.IsChecked)
        {
            var a = (Address.Text ?? "").Trim().TrimEnd(',', ' ');
            if (a.Length == 0) { AddrField.Stroke = Color.FromArgb("#FF6B6B"); Err.Text = "Введите адрес доставки"; return; }
            var parts = a.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (a.Length < 10 || parts.Length < 2) { AddrField.Stroke = Color.FromArgb("#FF6B6B"); Err.Text = "Укажите полный адрес: регион, город, улица, дом"; return; }
        }
        Err.Text = "";
        var o = new Order
        {
            Name = NameE.Text.Trim(), Phone = Phone.Text!.Trim(),
            Method = Delivery.IsChecked ? "Доставка" : "Самовывоз",
            Address = Delivery.IsChecked ? Address.Text!.Trim() : "",
            Payment = PayCard.IsChecked ? "Картой онлайн" : "При получении",
            Comment = Comment.Text?.Trim() ?? "",
            Subtotal = subtotal, Discount = discount, BonusUsed = BonusUsed, Delivery = DeliveryCost,
            Total = subtotal - discount - BonusUsed + DeliveryCost, Created = DateTime.Now
        };
        if (RememberSw.IsToggled)
        {
            Store.ProfileName = o.Name; Store.ProfilePhone = o.Phone;
            if (Delivery.IsChecked) Store.ProfileAddress = o.Address;
        }
        var id = await Store.PlaceOrder(o, lines);
        Form.IsVisible = false; ConfirmBar.IsVisible = false; Done.IsVisible = true;
        DoneL.Text = $"Заказ №{id} оформлен!";
        DoneInfo.Text = o.IsDelivery
            ? $"Курьер привезёт заказ {o.Eta:dd.MM.yyyy}.\nСледить за доставкой можно в профиле."
            : $"Заказ будет готов {o.Eta:dd.MM.yyyy, HH:mm}.\nМы свяжемся с вами по телефону.";
    }

    async void OnOrders(object? s, EventArgs e)
    {
        await Shell.Current.GoToAsync("//profile");
        await Shell.Current.GoToAsync("orders");
    }
    async void OnHome(object? s, EventArgs e) => await Shell.Current.GoToAsync("//home");
}
