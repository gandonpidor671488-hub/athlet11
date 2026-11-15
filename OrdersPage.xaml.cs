namespace Athlet;
public partial class OrdersPage : ContentPage
{
    IDispatcherTimer? timer;
    public OrdersPage() { InitializeComponent(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Reload();
        // раз в минуту пересчитываем «через N ч M мин»
        timer ??= Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMinutes(1);
        timer.Tick -= OnTick; timer.Tick += OnTick;
        timer.Start();
    }
    protected override void OnDisappearing() { base.OnDisappearing(); timer?.Stop(); }
    async void OnTick(object? s, EventArgs e) => await Reload();

    async Task Reload() => List.ItemsSource = await Store.Orders();
    static Order? Ord(object? s) => (s as BindableObject)?.BindingContext as Order;

    async void OnSkip(object? s, EventArgs e)
    {
        if (Ord(s) is Order o) { await Store.AccelerateOrder(o); await Reload(); }
    }
    async void OnPickup(object? s, EventArgs e)
    {
        if (Ord(s) is Order o) await Shell.Current.GoToAsync($"pickup?id={o.Id}");
    }
    async void OnGot(object? s, EventArgs e)
    {
        if (Ord(s) is Order o) { await Store.MarkReceived(o); await Reload(); }
    }
    async void OnCancel(object? s, EventArgs e)
    {
        if (Ord(s) is not Order o) return;
        if (!await DisplayAlert("Отменить заказ?", $"Заказ №{o.Id} будет отменён.", "Отменить", "Нет")) return;
        await Store.CancelOrder(o); await Reload();
    }
    async void OnRepeat(object? s, EventArgs e)
    {
        if (Ord(s) is not Order o) return;
        await Store.RepeatOrder(o);
        await Shell.Current.GoToAsync("//cart");
    }
}
