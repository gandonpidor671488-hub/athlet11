namespace Athlet;
public partial class InfoPage : ContentPage
{
    public InfoPage() { InitializeComponent(); }
    void OnCall(object? s, EventArgs e) { if (PhoneDialer.Default.IsSupported) PhoneDialer.Default.Open("+78001234567"); }
    async void OnMap(object? s, EventArgs e)
    {
        try { await Launcher.Default.OpenAsync(new Uri("https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString("ул. Спортивная, 10"))); }
        catch { }
    }
}
