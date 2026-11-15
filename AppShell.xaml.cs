namespace Athlet;
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("product", typeof(ProductPage));
        Routing.RegisterRoute("checkout", typeof(CheckoutPage));
        Routing.RegisterRoute("orders", typeof(OrdersPage));
        Routing.RegisterRoute("info", typeof(InfoPage));
        Routing.RegisterRoute("pickup", typeof(PickupPage));
        Store.CartChanged += async () => await UpdateCartTitle();
        Navigated += async (_, _) =>
        {
            await UpdateCartTitle();
            if ((CurrentPage as ContentPage)?.Content is View v) { v.Opacity = 0; await v.FadeTo(1, 220, Easing.CubicOut); }
        };
    }

    async Task UpdateCartTitle()
    {
        try
        {
            int n = await Store.CartCount();
            MainThread.BeginInvokeOnMainThread(() => CartTab.Title = n > 0 ? $"Корзина ({n})" : "Корзина");
        }
        catch { }
    }
}
