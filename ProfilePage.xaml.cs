namespace Athlet;
public partial class ProfilePage : ContentPage
{
    public ProfilePage() { InitializeComponent(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        NameE.Text = Store.ProfileName; PhoneE.Text = Store.ProfilePhone; AddrE.Text = Store.ProfileAddress;
        ShowHeader();
        var orders = (await Store.Orders()).Where(o => !o.Canceled).ToList();
        int spent = orders.Sum(o => o.Total);
        OrdersN.Text = orders.Count.ToString();
        SpentN.Text = spent >= 10000 ? $"{spent / 1000} т₽" : $"{spent:N0}";
        BonusN.Text = (await Store.BonusBalance()).ToString("N0");
        WebSw.IsToggled = Store.WebImages;
        var active = orders.FirstOrDefault(o => !o.IsFinished);
        ActiveBox.IsVisible = active != null;
        if (active != null)
        {
            ActTitle.Text = $"{active.TitleText} · {active.TotalText}";
            ActNum.Text = active.DaysNumber; ActCap.Text = active.DaysCaption;
            ActDays.Text = active.DaysLeftText;
            ActBar.Progress = active.Progress;
            ActStatus.Text = $"{active.StatusText} · {active.ArrivalText}";
        }
    }

    void OnWeb(object? s, ToggledEventArgs e) => Store.WebImages = e.Value;

    void ShowHeader()
    {
        var n = Store.ProfileName;
        Initial.Text = string.IsNullOrWhiteSpace(n) ? "A" : n.Trim().Substring(0, 1).ToUpper();
        WhoL.Text = string.IsNullOrWhiteSpace(n) ? "Гость" : n;
        PhoneL.Text = string.IsNullOrWhiteSpace(Store.ProfilePhone) ? "Добавьте телефон ниже" : Store.ProfilePhone;
    }

    async void OnSave(object? s, EventArgs e)
    {
        Store.ProfileName = NameE.Text?.Trim() ?? ""; Store.ProfilePhone = PhoneE.Text?.Trim() ?? ""; Store.ProfileAddress = AddrE.Text?.Trim() ?? "";
        ShowHeader();
        SaveBtn.Text = "✓ Сохранено"; await Task.Delay(1300); SaveBtn.Text = "Сохранить";
    }
    async void OnOrders(object? s, EventArgs e) => await Shell.Current.GoToAsync("orders");
    async void OnOrdersTap(object? s, TappedEventArgs e) => await Shell.Current.GoToAsync("orders");
    async void OnInfoTap(object? s, TappedEventArgs e) => await Shell.Current.GoToAsync("info");
}
