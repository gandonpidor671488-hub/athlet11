namespace Athlet;

/// <summary>Поле адреса: при вводе показывает список регионов России, выбор подставляет регион и запятую.</summary>
public partial class AddressField : ContentView
{
    bool suppress;
    public event EventHandler? TextEdited;

    public AddressField() { InitializeComponent(); }

    public string Text
    {
        get => E.Text ?? "";
        set { suppress = true; E.Text = value ?? ""; suppress = false; HideList(); }
    }

    public string Placeholder { get => E.Placeholder; set => E.Placeholder = value; }

    public bool ShowError
    {
        set => Box.Stroke = new SolidColorBrush(Color.FromArgb(value ? "#FF6B6B" : "#262B35"));
    }

    public void FocusField() => E.Focus();

    void OnText(object? s, TextChangedEventArgs e)
    {
        if (suppress) return;
        ShowError = false;
        TextEdited?.Invoke(this, EventArgs.Empty);
        Refresh();
    }

    void OnFocus(object? s, FocusEventArgs e) => Refresh();

    async void OnUnfocus(object? s, FocusEventArgs e)
    {
        await Task.Delay(300);          // чтобы успело сработать нажатие на пункт списка
        if (!E.IsFocused) HideList();
    }

    void Refresh()
    {
        var l = Regions.Suggest(E.Text);
        if (l.Count == 0) { HideList(); return; }
        Items.Children.Clear();
        foreach (var r in l)
        {
            var lbl = new Label { Text = r, FontSize = 15, Padding = new Thickness(16, 12), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 };
            var tap = new TapGestureRecognizer();
            var region = r;
            tap.Tapped += (_, _) => Pick(region);
            lbl.GestureRecognizers.Add(tap);
            Items.Children.Add(lbl);
        }
        Scroll.HeightRequest = Math.Min(l.Count * 44, 220);
        ListBox.IsVisible = true;
    }

    void Pick(string region)
    {
        suppress = true; E.Text = region + ", "; suppress = false;
        ShowError = false;
        TextEdited?.Invoke(this, EventArgs.Empty);
        HideList();
        E.Focus();
        E.CursorPosition = E.Text.Length;
    }

    void HideList() { ListBox.IsVisible = false; Items.Children.Clear(); }
}
