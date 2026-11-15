namespace Athlet;
public partial class CheckoutPage : ContentPage
{
    List<CartLine> lines = new();
    int subtotal, discount, balance, maxBonus;
    bool delivery;      // false — пункт выдачи, true — курьер
    bool payNow;        // false — при получении, true — сразу (онлайн, демо)

    static readonly Color AccentC = Color.FromArgb("#FF6A2B");
    static readonly Color MutedC = Color.FromArgb("#8A93A3");
    static readonly Color LineC = Color.FromArgb("#262B35");
    static readonly Color CardC = Color.FromArgb("#13161C");
    static readonly Color Card2C = Color.FromArgb("#1C2028");

    public CheckoutPage()
    {
        InitializeComponent();
        Address.TextEdited += (_, _) => { AddrErr.Text = ""; Err.Text = ""; };
        Paint();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        lines = await Store.SelectedCart();          // оформляются только выбранные в корзине товары
        subtotal = lines.Sum(x => x.Sum);
        discount = Store.DiscountFor(subtotal);
        int pcs = lines.Sum(x => x.Qty);
        SubHead.Text = $"{pcs} {PcsWord(pcs)}, {subtotal - discount:N0} ₽";
        CountL.Text = $"{pcs} шт.";
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
        if (string.IsNullOrWhiteSpace(Address.Text)) Address.Text = Store.ProfileAddress;
        UpdateTotals();
    }

    static string PcsWord(int n) => (n % 10 == 1 && n % 100 != 11) ? "товар"
        : (n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20)) ? "товара" : "товаров";

    int DeliveryCost => Store.DeliveryCost(subtotal, delivery);
    int BonusUsed => BonusSw.IsToggled ? maxBonus : 0;

    void OnBonus(object? s, ToggledEventArgs e) { if (TotalL != null) UpdateTotals(); }

    void OnSegPickup(object? s, TappedEventArgs e) { delivery = false; Paint(); UpdateTotals(); }
    void OnSegCourier(object? s, TappedEventArgs e) { delivery = true; Paint(); UpdateTotals(); }
    void OnPayNow(object? s, TappedEventArgs e) { payNow = true; Paint(); }
    void OnPayLater(object? s, TappedEventArgs e) { payNow = false; Paint(); }

    /// <summary>Перекрашивает переключатели и показывает нужный блок адреса.</summary>
    void Paint()
    {
        SegPickup.BackgroundColor = delivery ? Colors.Transparent : AccentC;
        SegPickupL.TextColor = delivery ? MutedC : Colors.White;
        SegCourier.BackgroundColor = delivery ? AccentC : Colors.Transparent;
        SegCourierL.TextColor = delivery ? Colors.White : MutedC;
        PickupAddr.IsVisible = !delivery;
        AddrBox.IsVisible = delivery;

        PayNowB.BackgroundColor = payNow ? Card2C : CardC; PayNowB.Stroke = new SolidColorBrush(payNow ? AccentC : LineC);
        PayLaterB.BackgroundColor = payNow ? CardC : Card2C; PayLaterB.Stroke = new SolidColorBrush(payNow ? LineC : AccentC);
        PayHint.Text = payNow ? "Оплата картой онлайн (демо), деньги спишутся сразу" : "Оплатите заказ при получении";
    }

    void UpdateTotals()
    {
        int d = DeliveryCost, used = BonusUsed, total = subtotal - discount - used + d;
        BonusRow.IsVisible = used > 0; BonusL2.Text = $"−{used:N0} ₽";
        DelL.Text = delivery ? (d == 0 ? "Бесплатно" : $"{d:N0} ₽") : "Самовывоз";
        TotalL.Text = $"{total:N0} ₽";
        BtnSum.Text = $"{total:N0} ₽";
        BonusL.Text = $"Вы получите {total * 5 / 100:N0} бонусов";
        var eta = DateTime.Now.AddDays(delivery ? 3 : 1);
        var price = d == 0 ? "0 ₽" : $"{d:N0} ₽";
        DateL.Text = delivery ? $"Курьером до {eta:dd.MM}, {price}" : $"Завтра, {eta:dd.MM}, 0 ₽";
    }

    async void OnConfirm(object? s, TappedEventArgs e)
    {
        var digits = new string((Phone.Text ?? "").Where(char.IsDigit).ToArray());
        if (lines.Count == 0) { Err.Text = "Не выбраны товары в корзине"; return; }
        if (delivery)
        {
            // адрес доставки обязателен: пустой (или только регион без города/улицы) не пропускаем
            var addrErr = Regions.CheckAddress(Address.Text);
            if (addrErr != null)
            {
                Address.ShowError = true; AddrErr.Text = addrErr; Err.Text = addrErr;
                Address.FocusField();
                return;
            }
        }
        if (string.IsNullOrWhiteSpace(NameE.Text)) { Err.Text = "Введите имя"; return; }
        if (digits.Length < 10) { Err.Text = "Введите корректный телефон"; return; }
        Err.Text = ""; AddrErr.Text = "";
        var o = new Order
        {
            Name = NameE.Text.Trim(), Phone = Phone.Text!.Trim(),
            Method = delivery ? "Доставка" : "Самовывоз",
            Address = delivery ? Address.Text.Trim() : "",
            Payment = payNow ? "Картой онлайн" : "При получении",
            Comment = Comment.Text?.Trim() ?? "",
            Subtotal = subtotal, Discount = discount, BonusUsed = BonusUsed, Delivery = DeliveryCost,
            Total = subtotal - discount - BonusUsed + DeliveryCost, Created = DateTime.Now
        };
        if (RememberSw.IsToggled)
        {
            Store.ProfileName = o.Name; Store.ProfilePhone = o.Phone;
            if (delivery) Store.ProfileAddress = o.Address;
        }
        var id = await Store.PlaceOrder(o, lines);
        Form.IsVisible = false; BottomBar.IsVisible = false; Done.IsVisible = true;
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
