namespace Athlet;
public partial class PickupPage : ContentPage, IQueryAttributable
{
    Order? order;
    bool zoom;
    readonly BarcodeDrawable drawable = new();

    public PickupPage() { InitializeComponent(); Bars.Drawable = drawable; }

    public async void ApplyQueryAttributes(IDictionary<string, object> q)
    {
        if (q.TryGetValue("id", out var v) && int.TryParse(v?.ToString(), out var id)) await Load(id);
    }

    async Task Load(int id)
    {
        order = await Store.GetOrder(id);
        if (order == null) return;
        drawable.Code = order.PickupCode; Bars.Invalidate();
        CodeL.Text = order.PickupCodeText;

        bool paid = order.Payment == "Картой онлайн";
        PayTitle.Text = paid ? "Оплачено" : "К оплате";
        TotalTitle.Text = paid ? "Сумма заказа (оплачена)" : "Итого к оплате в пункте";
        TotalL.Text = order.TotalText;
        LeftL.Text = $"{order.StorageText} · {order.StorageLeftText}";
        BindableLayout.SetItemsSource(LinesList, await Store.OrderLines(order));
        BindableLayout.SetItemsSource(UpsellList, await Store.Upsell());
        GotBtn.IsVisible = !order.Received;
        MsgL.Text = order.Received ? "Заказ получен. Спасибо за покупку!" : "";
    }

    void OnZoom(object? s, TappedEventArgs e)
    {
        zoom = !zoom;
        Bars.HeightRequest = zoom ? 260 : 130;
        ZoomL.Text = zoom ? "Нажмите, чтобы уменьшить" : "Нажмите, чтобы увеличить";
    }

    void OnToggle(object? s, TappedEventArgs e)
    {
        LinesBox.IsVisible = !LinesBox.IsVisible;
        Chevron.Text = LinesBox.IsVisible ? "⌃" : "⌄";
    }

    async void OnGot(object? s, EventArgs e)
    {
        if (order == null) return;
        await Store.MarkReceived(order);
        GotBtn.IsVisible = false;
        MsgL.Text = "Заказ получен. Спасибо за покупку!";
        await Task.Delay(1500);
        await Shell.Current.GoToAsync("..");
    }
}
