namespace Athlet;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder()
        .UseMauiApp<App>()
        .ConfigureFonts(f =>
        {
            f.AddFont("Inter-Regular.otf", "Inter");
            f.AddFont("Inter-Bold.otf", "InterBold");
        })
        .Build();
}
