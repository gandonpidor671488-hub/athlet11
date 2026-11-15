using System.ComponentModel;
using SQLite;
namespace Athlet;

// ───────────────────────── Модели ─────────────────────────

/// <summary>Товар каталога.</summary>
public class Product : INotifyPropertyChanged
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Category { get; set; } = "";
    public string Image { get; set; } = "";        // файл из Resources/Images
    public string ImageUrl { get; set; } = "";     // необязательная ссылка на фото в интернете
    public string Weight { get; set; } = "";
    public string Description { get; set; } = "";
    public string Composition { get; set; } = "";
    public string Flavors { get; set; } = "";      // вкусы через запятую
    public string Serving { get; set; } = "";
    public int Price { get; set; }
    public int OldPrice { get; set; }
    public double Rating { get; set; }
    public int Reviews { get; set; }
    public int Kcal { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carbs { get; set; }
    public bool IsHit { get; set; }
    public bool IsNew { get; set; }

    /// <summary>Нарисованная упаковка из ресурсов приложения (всегда есть, работает без интернета).</summary>
    [Ignore] public string ImageSrc => Image;
    /// <summary>Фото из интернета (лежит поверх ImageSrc; если не загрузилось, видна упаковка).</summary>
    [Ignore] public string? RemoteSrc => Store.WebImages && !string.IsNullOrWhiteSpace(ImageUrl) ? ImageUrl.Trim() : null;
    [Ignore] public string PriceText => $"{Price:N0} ₽";
    [Ignore] public bool HasDiscount => OldPrice > Price;
    [Ignore] public string OldPriceText => HasDiscount ? $"{OldPrice:N0} ₽" : "";
    [Ignore] public string DiscountText => HasDiscount ? $"−{100 - Price * 100 / OldPrice}%" : "";
    [Ignore] public string SavingText => HasDiscount ? $"Экономия {OldPrice - Price:N0} ₽" : "";
    [Ignore] public string RatingText => $"★ {Rating:0.0}";
    [Ignore] public string ReviewsText => $"{Reviews} отзывов";
    [Ignore] public string KcalText => Kcal.ToString();
    [Ignore] public string ProteinText => Protein.ToString("0.#");
    [Ignore] public string FatText => Fat.ToString("0.#");
    [Ignore] public string CarbsText => Carbs.ToString("0.#");
    [Ignore] public List<string> FlavorList =>
        Flavors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    bool fav;
    [Ignore] public bool IsFav
    {
        get => fav;
        set
        {
            fav = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFav)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FavIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FavColor)));
        }
    }
    bool added;
    [Ignore] public bool Added
    {
        get => added;
        set
        {
            added = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Added)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AddIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AddColor)));
        }
    }
    [Ignore] public string AddIcon => added ? "✓" : "+";
    [Ignore] public Color AddColor => added ? Color.FromArgb("#2ECC71") : Color.FromArgb("#FF5A1F");
    [Ignore] public string FavIcon => fav ? "♥" : "♡";
    [Ignore] public Color FavColor => fav ? Color.FromArgb("#FF4D6A") : Colors.White;
    public event PropertyChangedEventHandler? PropertyChanged;
}

public class CartItem
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int ProductId { get; set; }
    public string Flavor { get; set; } = "";
    public int Qty { get; set; }
}

public class Favorite { [PrimaryKey] public int ProductId { get; set; } }

public class Review
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int ProductId { get; set; }
    public string Author { get; set; } = "";
    public int Stars { get; set; }
    public string Text { get; set; } = "";
    public DateTime Date { get; set; }
    [Ignore] public string StarsText => new string('★', Stars) + new string('☆', 5 - Stars);
    [Ignore] public string DateText => Date.ToString("dd.MM.yyyy");
}

public class TrackStep
{
    public string Title { get; set; } = "";
    public string TimeText { get; set; } = "";
    public string Dot { get; set; } = "○";
    public Color DotColor { get; set; } = Colors.Gray;
    public Color TitleColor { get; set; } = Colors.Gray;
}

/// <summary>Заказ. Срок: курьер 3 дня, самовывоз 1 день. Этапы считаются по прошедшему времени.</summary>
public class Order
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Method { get; set; } = "";
    public string Address { get; set; } = "";
    public string Payment { get; set; } = "";
    public string Comment { get; set; } = "";
    public string Items { get; set; } = "";        // текст для показа
    public string ItemsData { get; set; } = "";    // id|вкус|кол-во;… для «Повторить заказ»
    public int Subtotal { get; set; }
    public int Discount { get; set; }
    public int BonusUsed { get; set; }
    public int Delivery { get; set; }
    public int Total { get; set; }
    public DateTime Created { get; set; }
    public int ShiftDays { get; set; }             // демо: «перемотка» времени вперёд
    public bool Canceled { get; set; }
    public bool Received { get; set; }             // клиент подтвердил получение

    static readonly string[] DeliveryTitles = { "Заказ принят", "Собирается на складе", "Передан в службу доставки", "В пути к вам", "Доставлен" };
    static readonly string[] DeliveryShort = { "Принят", "Собирается", "Передан курьеру", "В пути", "Доставлен" };
    static readonly double[] DeliveryAt = { 0, 0.03, 0.2, 0.4, 1.0 };
    static readonly string[] PickupTitles = { "Заказ принят", "Собирается на складе", "Готов к выдаче" };
    static readonly string[] PickupShort = { "Принят", "Собирается", "Готов к выдаче" };
    static readonly double[] PickupAt = { 0, 0.1, 1.0 };

    [Ignore] public bool IsDelivery => Method == "Доставка";
    [Ignore] public double TotalDays => IsDelivery ? 3 : 1;
    [Ignore] public DateTime Now => DateTime.Now.AddDays(ShiftDays);
    [Ignore] public DateTime Eta => Created.AddDays(TotalDays);
    [Ignore] public TimeSpan Remaining => Eta - Now;
    [Ignore] public bool IsFinished => !Canceled && Remaining <= TimeSpan.Zero;
    [Ignore] public int StepCount => IsDelivery ? DeliveryAt.Length : PickupAt.Length;
    [Ignore] public int Step
    {
        get
        {
            var at = IsDelivery ? DeliveryAt : PickupAt;
            double f = (Now - Created).TotalDays / TotalDays;
            int st = 0;
            for (int i = 0; i < at.Length; i++) if (f >= at[i]) st = i;
            return st;
        }
    }
    [Ignore] public double Progress => Canceled ? 0 : (double)Step / (StepCount - 1);
    [Ignore] public bool CanAccelerate => !Canceled && !IsFinished;
    [Ignore] public bool CanCancel => !Canceled && !IsFinished && Step <= 1;
    [Ignore] public bool HasCourier => IsDelivery && !Canceled && !IsFinished && Step == 3;

    [Ignore] public string TitleText => $"Заказ №{Id}";
    [Ignore] public string TrackText => $"Трек-номер: ATH{Id:D6}";
    [Ignore] public string DateText => Created.ToString("dd.MM.yyyy HH:mm");
    [Ignore] public string TotalText => $"{Total:N0} ₽";
    [Ignore] public string PlaceText => IsDelivery ? "📍 " + Address : "🏬 Самовывоз: ул. Спортивная, 10";
    [Ignore] public string CourierText => "🚚 Курьер Андрей уже везёт ваш заказ";
    [Ignore] public string ArrivalText => Canceled ? "" : (IsDelivery ? "Ожидаемая доставка: " : "Будет готов: ") + Eta.ToString("dd.MM.yyyy, HH:mm");
    [Ignore] public string StatusText => Canceled ? "Отменён" : Received ? "Получен" : (IsDelivery ? DeliveryShort : PickupShort)[Step];
    [Ignore] public Color StatusColor => Canceled ? Color.FromArgb("#8D97A6") : (IsFinished || Received) ? Color.FromArgb("#2ECC71") : Color.FromArgb("#FF5A1F");

    // выдача в пункте: штрихкод, срок хранения
    [Ignore] public bool CanPickup => !IsDelivery && IsFinished && !Received;
    [Ignore] public bool CanConfirm => IsFinished && !Received;
    [Ignore] public string PickupCode => (100600000000L + Id * 7919L).ToString("D12");
    [Ignore] public string PickupCodeText => $"{PickupCode.Substring(0, 4)} {PickupCode.Substring(4, 4)} {PickupCode.Substring(8, 4)}";
    [Ignore] public DateTime LastStorageDay => Eta.Date.AddDays(5);
    [Ignore] public string StorageText => $"Последний день хранения: {LastStorageDay:dd.MM.yyyy}";
    [Ignore] public string StorageLeftText
    {
        get
        {
            int d = (LastStorageDay - Now.Date).Days;
            return d <= 0 ? "Последний день хранения" : $"Хранится ещё {d} {Store.DaysWord(d)}";
        }
    }

    [Ignore] int DaysCeil => Math.Max(1, (int)Math.Ceiling(Remaining.TotalDays));
    [Ignore] public string DaysNumber => Canceled ? "✕" : IsFinished ? "✓" : DaysCeil.ToString();
    [Ignore] public string DaysCaption => Canceled ? "отмена" : IsFinished ? "готово" : Store.DaysWord(DaysCeil);
    [Ignore] public string DaysLeftText
    {
        get
        {
            if (Canceled) return "Заказ отменён";
            if (Received) return "Заказ получен";
            if (IsFinished) return IsDelivery ? "Заказ доставлен" : "Заказ готов к выдаче";
            var r = Remaining;
            string lead = IsDelivery ? "Прибудет" : "Будет готов";
            if (r.TotalDays >= 1) return $"{lead} через {(int)r.TotalDays} {Store.DaysWord((int)r.TotalDays)} {r.Hours} ч";
            if (r.TotalHours >= 1) return $"{lead} через {(int)r.TotalHours} ч {r.Minutes} мин";
            return $"{lead} через {Math.Max(1, r.Minutes)} мин";
        }
    }
    [Ignore] public string BreakdownText
    {
        get
        {
            var parts = new List<string> { $"Товары {Subtotal:N0} ₽" };
            if (Discount > 0) parts.Add($"скидка −{Discount:N0} ₽");
            if (BonusUsed > 0) parts.Add($"бонусы −{BonusUsed:N0}");
            parts.Add($"доставка {Delivery:N0} ₽");
            return string.Join(" · ", parts);
        }
    }
    [Ignore] public List<TrackStep> Steps
    {
        get
        {
            var res = new List<TrackStep>();
            if (Canceled) return res;
            var titles = IsDelivery ? DeliveryTitles : PickupTitles;
            var at = IsDelivery ? DeliveryAt : PickupAt;
            int cur = Step;
            for (int i = 0; i < titles.Length; i++)
            {
                bool done = i < cur || (i == cur && IsFinished);
                bool active = i == cur && !IsFinished;
                res.Add(new TrackStep
                {
                    Title = titles[i],
                    TimeText = Created.AddDays(TotalDays * at[i]).ToString("dd.MM, HH:mm"),
                    Dot = done ? "✓" : active ? "●" : "○",
                    DotColor = done ? Color.FromArgb("#2ECC71") : active ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#4A5361"),
                    TitleColor = done || active ? Colors.White : Color.FromArgb("#8D97A6")
                });
            }
            return res;
        }
    }
}

public class OrderLine
{
    public Product P { get; set; } = null!;
    public int Qty { get; set; }
    public string Flavor { get; set; } = "";
    public string Note { get; set; } = "";
    public string SumText => $"{P.Price * Qty:N0} ₽";
    public string SubText => string.IsNullOrEmpty(Flavor) ? $"{Qty} шт · {P.Weight}" : $"{Qty} шт · {Flavor} · {P.Weight}";
}

public class CartLine
{
    public CartItem Item { get; set; } = null!;
    public Product P { get; set; } = null!;
    public int Qty => Item.Qty;
    public int Sum => P.Price * Item.Qty;
    public string SumText => $"{Sum:N0} ₽";
    public string UnitText => $"{P.Price:N0} ₽ за шт.";
    public string OldSumText => P.HasDiscount ? $"{P.OldPrice * Qty:N0} ₽" : "";
    public int Saving => P.HasDiscount ? (P.OldPrice - P.Price) * Qty : 0;
    public string FlavorText => string.IsNullOrEmpty(Item.Flavor) ? P.Weight : $"{Item.Flavor} · {P.Weight}";
}

public class Banner
{
    public string Image { get; set; } = "";
    public string Tag { get; set; } = "";
    public string Title { get; set; } = "";
    public string Sub { get; set; } = "";
    public string Action { get; set; } = "";
}

public class CatTile
{
    public string Name { get; }
    public string Image { get; }
    public CatTile(string name, string image) { Name = name; Image = image; }
}

// ───────────────────────── Хранилище ─────────────────────────

/// <summary>Локальная база SQLite: каталог, корзина, избранное, отзывы, заказы. Профиль — в Preferences.</summary>
public static class Store
{
    public static readonly string[] Categories = { "Протеин", "Креатин", "Аминокислоты", "Гейнеры", "Витамины", "Батончики", "Предтрены", "Жиросжигатели", "Аксессуары" };
    public static readonly string[] Brands = { "ATHLET", "PowerLab", "IronFuel", "NutriPro" };
    public static readonly string[] SortModes = { "По умолчанию", "Сначала дешевле", "Сначала дороже", "По названию", "По рейтингу" };
    public static readonly CatTile[] CategoryTiles =
    {
        new CatTile("Протеин", "p01.png"),
        new CatTile("Креатин", "p06.png"),
        new CatTile("Аминокислоты", "p09.png"),
        new CatTile("Гейнеры", "p13.png"),
        new CatTile("Витамины", "p16.png"),
        new CatTile("Батончики", "p20.png"),
        new CatTile("Предтрены", "p23.png"),
        new CatTile("Жиросжигатели", "p34.png"),
        new CatTile("Аксессуары", "p38.png")
    };

    public static readonly List<Banner> Banners = new()
    {
        new Banner { Image = "hero.png", Tag = "АКЦИЯ", Title = "До −20% на хиты", Sub = "Протеин, креатин, гейнеры", Action = "sale" },
        new Banner { Image = "hero2.png", Tag = "НОВИНКИ", Title = "Свежие поступления", Sub = "Аминокислоты и витамины", Action = "new" },
        new Banner { Image = "hero3.png", Tag = "ДОСТАВКА", Title = "Бесплатно от 5 000 ₽", Sub = "Курьером за 3 дня", Action = "all" },
    };

    public const int FreeFrom = 5000;       // бесплатная доставка от суммы
    public const int DeliveryPrice = 300;
    public static string? PendingCategory, PendingQuery;
    public static bool PendingSale;
    public static int PendingSort;

    /// <summary>Подборки по цели: какие категории входят.</summary>
    public static readonly Dictionary<string, string[]> Goals = new()
    {
        ["💪 Набор массы"] = new[] { "Протеин", "Гейнеры", "Креатин" },
        ["🔥 Похудение"] = new[] { "Жиросжигатели", "Аминокислоты", "Протеин" },
        ["⚡ Энергия"] = new[] { "Предтрены", "Батончики", "Аминокислоты" },
        ["🛡 Здоровье"] = new[] { "Витамины", "Аминокислоты" },
    };
    public static readonly string[] GoalNames = { "💪 Набор массы", "🔥 Похудение", "⚡ Энергия", "🛡 Здоровье" };

    /// <summary>Использовать ли фото из интернета (поверх нарисованных упаковок).</summary>
    public static bool WebImages { get => Preferences.Default.Get("web_images", true); set => Preferences.Default.Set("web_images", value); }

    public static event Action? CartChanged;
    public static void RaiseCartChanged() => CartChanged?.Invoke();
    public static string? PromoCode;

    public static readonly Dictionary<string, string> Promos = new()
    {
        ["ATHLET10"] = "−10% на весь заказ",
        ["FIT15"] = "−15% при заказе от 3 000 ₽",
        ["WELCOME"] = "−300 ₽ при заказе от 1 500 ₽",
    };

    // профиль
    public static string ProfileName { get => Preferences.Default.Get("name", ""); set => Preferences.Default.Set("name", value ?? ""); }
    public static string ProfilePhone { get => Preferences.Default.Get("phone", ""); set => Preferences.Default.Set("phone", value ?? ""); }
    public static string ProfileAddress { get => Preferences.Default.Get("address", ""); set => Preferences.Default.Set("address", value ?? ""); }

    static SQLiteAsyncConnection? db;

    static async Task<SQLiteAsyncConnection> Conn()
    {
        if (db != null) return db;
        var c = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "athlet_v4.db3"));
        await c.CreateTableAsync<Product>();
        await c.CreateTableAsync<CartItem>();
        await c.CreateTableAsync<Favorite>();
        await c.CreateTableAsync<Review>();
        await c.CreateTableAsync<Order>();
        if (await c.Table<Product>().CountAsync() == 0)
        {
            await c.InsertAllAsync(Seed());
            await c.InsertAllAsync(SeedReviews(await c.Table<Product>().ToListAsync()));
        }
        return db = c;
    }

    public static string DaysWord(int n) => (n % 10 == 1 && n % 100 != 11) ? "день"
        : (n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20)) ? "дня" : "дней";

    // ── каталог ──
    static async Task<List<Product>> Mark(List<Product> l)
    {
        var c = await Conn();
        var fav = (await c.Table<Favorite>().ToListAsync()).Select(f => f.ProductId).ToHashSet();
        foreach (var p in l) p.IsFav = fav.Contains(p.Id);
        return l;
    }

    public static async Task<List<Product>> Products(string? cat = null, string? q = null, int sort = 0, string? brand = null, bool sale = false)
    {
        var l = await (await Conn()).Table<Product>().ToListAsync();
        if (cat != null && cat.StartsWith("Цель:"))
        {
            var cats = Goals.TryGetValue(cat.Substring(5), out var g) ? g : Array.Empty<string>();
            l = l.Where(p => cats.Contains(p.Category)).ToList();
        }
        else if (!string.IsNullOrEmpty(cat) && cat != "Все") l = l.Where(p => p.Category == cat).ToList();
        if (!string.IsNullOrEmpty(brand)) l = l.Where(p => p.Brand == brand).ToList();
        if (sale) l = l.Where(p => p.HasDiscount).ToList();
        if (!string.IsNullOrWhiteSpace(q))
            l = l.Where(p => (p.Name + " " + p.Brand + " " + p.Category).Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
        l = sort switch
        {
            1 => l.OrderBy(p => p.Price).ToList(),
            2 => l.OrderByDescending(p => p.Price).ToList(),
            3 => l.OrderBy(p => p.Name).ToList(),
            4 => l.OrderByDescending(p => p.Rating).ToList(),
            _ => l
        };
        return await Mark(l);
    }

    public static async Task<Product?> Get(int id)
    {
        var p = await (await Conn()).FindAsync<Product>(id);
        if (p != null) await Mark(new List<Product> { p });
        return p;
    }

    public static async Task<List<Product>> Hits() => await Mark((await All()).Where(p => p.IsHit).ToList());
    public static async Task<List<Product>> News() => await Mark((await All()).Where(p => p.IsNew).ToList());
    public static async Task<List<Product>> Sales() => await Mark((await All()).Where(p => p.HasDiscount).OrderByDescending(p => p.OldPrice - p.Price).ToList());
    public static async Task<List<Product>> Similar(Product p) =>
        await Mark((await All()).Where(x => x.Category == p.Category && x.Id != p.Id).Take(8).ToList());
    static async Task<List<Product>> All() => await (await Conn()).Table<Product>().ToListAsync();

    public static async Task SetImageUrl(int id, string url)
    {
        var c = await Conn(); var p = await c.FindAsync<Product>(id);
        if (p == null) return;
        p.ImageUrl = url?.Trim() ?? ""; await c.UpdateAsync(p);
    }

    // ── недавно просмотренные ──
    public static void AddRecent(int id)
    {
        var l = Preferences.Default.Get("recent", "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        l.Remove(id.ToString()); l.Insert(0, id.ToString());
        Preferences.Default.Set("recent", string.Join(",", l.Take(10)));
    }
    public static async Task<List<Product>> Recent()
    {
        var ids = Preferences.Default.Get("recent", "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
        var all = await All();
        return await Mark(ids.Select(i => all.FirstOrDefault(p => p.Id == i)).Where(p => p != null).Select(p => p!).ToList());
    }

    public static async Task<List<Product>> Upsell()
    {
        var inCart = (await Cart()).Select(l => l.P.Id).ToHashSet();
        var l = (await All()).Where(p => !inCart.Contains(p.Id) && p.Price <= 1500).OrderByDescending(p => p.Rating).Take(8).ToList();
        return await Mark(l);
    }

    // ── избранное ──
    public static async Task AddFavorite(int productId) => await (await Conn()).InsertOrReplaceAsync(new Favorite { ProductId = productId });
    public static async Task ToggleFav(Product p)
    {
        var c = await Conn();
        if (p.IsFav) await c.DeleteAsync<Favorite>(p.Id);
        else await c.InsertOrReplaceAsync(new Favorite { ProductId = p.Id });
        p.IsFav = !p.IsFav;
    }
    public static async Task<List<Product>> Favorites()
    {
        var c = await Conn();
        var ids = (await c.Table<Favorite>().ToListAsync()).Select(f => f.ProductId).ToHashSet();
        return await Mark((await All()).Where(p => ids.Contains(p.Id)).ToList());
    }
    public static async Task<int> FavoritesCount() => await (await Conn()).Table<Favorite>().CountAsync();

    // ── отзывы ──
    public static async Task<List<Review>> ReviewsOf(int productId) =>
        (await (await Conn()).Table<Review>().Where(r => r.ProductId == productId).ToListAsync()).OrderByDescending(r => r.Date).ToList();

    public static async Task AddReview(Product p, string author, int stars, string text)
    {
        var c = await Conn();
        await c.InsertAsync(new Review { ProductId = p.Id, Author = author, Stars = stars, Text = text, Date = DateTime.Now });
        p.Rating = Math.Round((p.Rating * p.Reviews + stars) / (p.Reviews + 1), 1);
        p.Reviews++;
        await c.UpdateAsync(p);
    }

    // ── корзина ──
    public static async Task AddToCart(int productId, string flavor, int qty)
    {
        var c = await Conn();
        var i = await c.Table<CartItem>().Where(x => x.ProductId == productId && x.Flavor == flavor).FirstOrDefaultAsync();
        if (i == null) await c.InsertAsync(new CartItem { ProductId = productId, Flavor = flavor, Qty = qty });
        else { i.Qty += qty; await c.UpdateAsync(i); }
    }
    public static async Task SetQty(CartItem item, int qty)
    {
        var c = await Conn();
        if (qty <= 0) await c.DeleteAsync(item);
        else { item.Qty = qty; await c.UpdateAsync(item); }
    }
    public static async Task<List<CartLine>> Cart()
    {
        var c = await Conn(); var res = new List<CartLine>();
        foreach (var i in await c.Table<CartItem>().ToListAsync())
            if (await c.FindAsync<Product>(i.ProductId) is Product p) res.Add(new CartLine { Item = i, P = p });
        return res;
    }
    public static async Task<int> CartCount() => (await Cart()).Sum(x => x.Qty);

    // ── промокоды и доставка ──
    public static int DiscountFor(int subtotal) => PromoCode switch
    {
        "ATHLET10" => subtotal / 10,
        "FIT15" when subtotal >= 3000 => subtotal * 15 / 100,
        "WELCOME" when subtotal >= 1500 => 300,
        _ => 0
    };

    public static (bool ok, string msg) ApplyPromo(string? code, int subtotal)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        if (code == "") { PromoCode = null; return (true, "Промокод снят"); }
        if (!Promos.ContainsKey(code)) return (false, "Такого промокода нет");
        var prev = PromoCode; PromoCode = code;
        if (DiscountFor(subtotal) == 0) { PromoCode = prev; return (false, "Не выполнены условия: " + Promos[code]); }
        return (true, "Промокод применён: " + Promos[code]);
    }

    public static int DeliveryCost(int subtotal, bool delivery) => delivery && subtotal < FreeFrom ? DeliveryPrice : 0;

    // ── заказы ──
    public static async Task<int> PlaceOrder(Order o, List<CartLine> lines)
    {
        var c = await Conn();
        o.Items = string.Join("\n", lines.Select(l =>
            $"{l.P.Name}{(string.IsNullOrEmpty(l.Item.Flavor) ? "" : " (" + l.Item.Flavor + ")")} × {l.Qty}"));
        o.ItemsData = string.Join(";", lines.Select(l => $"{l.P.Id}|{l.Item.Flavor}|{l.Qty}"));
        await c.InsertAsync(o);
        await c.DeleteAllAsync<CartItem>();
        PromoCode = null;
        return o.Id;
    }
    public static async Task<List<Order>> Orders() =>
        (await (await Conn()).Table<Order>().ToListAsync()).OrderByDescending(o => o.Created).ToList();

    public static async Task<Order?> GetOrder(int id) => await (await Conn()).FindAsync<Order>(id);
    /// <summary>Первый заказ, который уже можно забрать в пункте выдачи.</summary>
    public static async Task<Order?> ReadyPickup() => (await Orders()).FirstOrDefault(o => o.CanPickup);
    public static async Task MarkReceived(Order o) { var c = await Conn(); o.Received = true; await c.UpdateAsync(o); }

    public static async Task<List<OrderLine>> OrderLines(Order o)
    {
        var c = await Conn(); var res = new List<OrderLine>();
        foreach (var part in o.ItemsData.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var f = part.Split('|');
            if (f.Length == 3 && int.TryParse(f[0], out var id) && int.TryParse(f[2], out var qty)
                && await c.FindAsync<Product>(id) is Product p)
                res.Add(new OrderLine { P = p, Qty = qty, Flavor = f[1], Note = o.StorageText });
        }
        return res;
    }

    public static async Task AccelerateOrder(Order o)
    {
        var c = await Conn(); o.ShiftDays++; await c.UpdateAsync(o);
    }
    public static async Task CancelOrder(Order o)
    {
        var c = await Conn(); o.Canceled = true; await c.UpdateAsync(o);
    }
    public static async Task RepeatOrder(Order o)
    {
        foreach (var part in o.ItemsData.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var f = part.Split('|');
            if (f.Length == 3 && int.TryParse(f[0], out var id) && int.TryParse(f[2], out var qty))
                await AddToCart(id, f[1], qty);
        }
        RaiseCartChanged();
    }

    /// <summary>Бонусы: 5% от суммы заказа, 1 бонус = 1 ₽. Отменённые заказы не считаются.</summary>
    public static async Task<int> BonusBalance()
    {
        var orders = (await Orders()).Where(o => !o.Canceled).ToList();
        int earned = orders.Sum(o => o.Total * 5 / 100);
        int used = orders.Sum(o => o.BonusUsed);
        return Math.Max(0, earned - used);
    }

    // ── данные ──
    static List<Product> Seed()
    {
        Product P(string cat, string brand, string name, string img, string w, int price, int old, double rating, int reviews,
                  string flavors, string serving, int kcal, double prot, double fat, double carb, string d, string comp,
                  bool hit = false, bool isNew = false) =>
            new()
            {
                Category = cat, Brand = brand, Name = name, Image = img, Weight = w, Price = price, OldPrice = old,
                Rating = rating, Reviews = reviews, Flavors = flavors, Serving = serving, Kcal = kcal, Protein = prot,
                Fat = fat, Carbs = carb, Description = d, Composition = comp, IsHit = hit, IsNew = isNew
            };
        var list = new List<Product>
        {
            P("Протеин","ATHLET","Whey Protein Gold","p01.png","900 г",1890,2290,4.8,312,"Шоколад,Ваниль,Клубника,Печенье","30 г",120,24,1.5,2.5,
              "Сывороточный протеин для набора и поддержания мышечной массы. 24 г белка в порции, хорошо растворяется в воде и молоке.",
              "Концентрат сывороточного белка, лецитин, ароматизатор, подсластитель.",hit:true),
            P("Протеин","PowerLab","Whey Isolate","p02.png","2 кг",4590,0,4.9,187,"Шоколад,Ваниль,Без вкуса","30 г",110,27,0.5,1,
              "Изолят с минимумом жиров и лактозы, быстро усваивается после тренировки.",
              "Изолят сывороточного белка, ароматизатор, подсластитель.",isNew:true),
            P("Протеин","IronFuel","Casein Night","p03.png","900 г",2190,0,4.6,94,"Шоколад,Ванильный крем","33 г",120,24,1,3,
              "Медленный казеиновый протеин для приёма перед сном. Питает мышцы в течение ночи.",
              "Мицеллярный казеин, какао, загуститель."),
            P("Протеин","NutriPro","Vegan Protein","p04.png","1 кг",2490,2790,4.5,63,"Шоколад,Банан","35 г",130,25,2.5,3,
              "Растительный протеин из гороха и риса. Подходит вегетарианцам и людям с непереносимостью лактозы.",
              "Белок гороха, белок риса, какао, ароматизатор, стевия.",isNew:true),
            P("Протеин","NutriPro","Egg Protein","p05.png","750 г",2390,0,4.4,48,"Ваниль,Шоколад","30 г",115,25,0.5,2,
              "Яичный протеин с высокой биологической ценностью, не содержит лактозы.",
              "Яичный белок, ароматизатор, подсластитель."),
            P("Креатин","ATHLET","Creatine Monohydrate","p06.png","300 г",1190,1490,4.9,428,"Без вкуса,Лимон,Апельсин","5 г",0,0,0,0,
              "Моногидрат креатина для силы и выносливости. 5 г в порции, 60 порций в банке.",
              "Креатин моногидрат 100%.",hit:true),
            P("Креатин","PowerLab","Creatine Caps","p07.png","120 капсул",990,0,4.7,156,"Без вкуса","4 капсулы",0,0,0,0,
              "Креатин в капсулах: удобно брать с собой, не нужно ничего разводить.",
              "Креатин моногидрат, капсула (желатин)."),
            P("Креатин","IronFuel","Creatine HCl","p08.png","90 капсул",1590,0,4.5,41,"Без вкуса","2 капсулы",0,0,0,0,
              "Креатин гидрохлорид: хорошо растворяется, подходит для маленьких порций.",
              "Креатин гидрохлорид, капсула (желатин).",isNew:true),
            P("Аминокислоты","IronFuel","BCAA 2:1:1","p09.png","300 г",1390,1690,4.7,205,"Арбуз,Кола,Лимон-лайм","10 г",20,5,0,0,
              "Лейцин, изолейцин и валин для восстановления во время тренировки.",
              "L-лейцин, L-изолейцин, L-валин, лимонная кислота, ароматизатор.",hit:true),
            P("Аминокислоты","PowerLab","Glutamine","p10.png","250 г",1090,0,4.6,118,"Без вкуса,Яблоко","5 г",20,5,0,0,
              "Глютамин для восстановления после нагрузок и поддержки иммунитета.",
              "L-глютамин 100%."),
            P("Аминокислоты","NutriPro","EAA Complex","p11.png","400 г",1990,0,4.8,77,"Манго,Вишня","12 г",30,7,0,1,
              "Все девять незаменимых аминокислот в одной порции.",
              "Незаменимые аминокислоты, лимонная кислота, ароматизатор, сукралоза.",isNew:true),
            P("Аминокислоты","ATHLET","Collagen Joint","p12.png","300 г",1490,0,4.4,52,"Без вкуса,Лесные ягоды","10 г",35,9,0,0,
              "Коллаген с витамином C для суставов и связок.",
              "Гидролизат коллагена, витамин C, ароматизатор."),
            P("Гейнеры","ATHLET","Mass Gainer 3000","p13.png","3 кг",2790,3290,4.6,233,"Шоколад,Банан,Клубника","150 г",600,30,5,100,
              "Белково-углеводная смесь для набора массы. 600 ккал в порции.",
              "Мальтодекстрин, концентрат сывороточного белка, какао, витамины.",hit:true),
            P("Гейнеры","IronFuel","Hard Gainer","p14.png","1,5 кг",1890,0,4.3,88,"Шоколад,Ваниль","100 г",390,22,4,66,
              "Гейнер для людей с быстрым обменом веществ.",
              "Углеводный комплекс, белки молока, ароматизатор."),
            P("Гейнеры","PowerLab","Pro Gainer","p15.png","4,5 кг",3990,0,4.7,61,"Шоколад,Печенье","150 г",580,35,6,90,
              "Большая упаковка гейнера с повышенным содержанием белка.",
              "Мальтодекстрин, белки молока, какао, ферменты.",isNew:true),
            P("Витамины","ATHLET","Omega-3","p16.png","90 капсул",1090,1290,4.8,301,"Без вкуса","2 капсулы",20,0,2,0,
              "Рыбий жир: ЭПК и ДГК для сердца и суставов.",
              "Рыбий жир, желатин, глицерин, витамин E.",hit:true),
            P("Витамины","PowerLab","Multivitamin Sport","p17.png","60 таблеток",890,0,4.5,144,"Без вкуса","2 таблетки",5,0,0,1,
              "Витаминно-минеральный комплекс для активных людей.",
              "Витамины A, C, D, E, группа B, цинк, магний."),
            P("Витамины","NutriPro","Vitamin D3 + K2","p18.png","120 капсул",790,0,4.9,97,"Без вкуса","1 капсула",5,0,0.5,0,
              "Витамин D3 и K2 для костей и иммунитета.",
              "Холекальциферол, менахинон-7, оливковое масло, капсула.",isNew:true),
            P("Витамины","IronFuel","Magnesium B6","p19.png","60 таблеток",690,0,4.6,120,"Без вкуса","2 таблетки",5,0,0,1,
              "Магний и витамин B6 для нервной системы и мышц.",
              "Цитрат магния, пиридоксин, наполнитель."),
            P("Батончики","IronFuel","Protein Bar","p20.png","60 г",140,0,4.5,512,"Шоколад,Карамель,Арахис,Кокос","1 батончик",210,20,8,15,
              "Протеиновый батончик, 20 г белка, без добавленного сахара.",
              "Белок молока, орехи, подсластитель, какао.",hit:true),
            P("Батончики","ATHLET","Crunch Bar","p21.png","50 г",120,150,4.4,210,"Брауни,Миндаль","1 батончик",190,15,7,17,
              "Хрустящий батончик с кусочками орехов, 15 г белка.",
              "Белок молока, миндаль, рисовые криспы, какао."),
            P("Батончики","PowerLab","Low Sugar Bar Box","p22.png","12 × 40 г",1490,0,4.7,66,"Ассорти","1 батончик",150,12,5,10,
              "Коробка батончиков с низким содержанием сахара.",
              "Белок молока, коллаген, орехи, подсластитель.",isNew:true),
            P("Предтрены","PowerLab","Pre-Workout Fire","p23.png","300 г",1790,0,4.6,140,"Вишня,Голубая малина","10 г",15,0,0,3,
              "Предтренировочный комплекс с кофеином, бета-аланином и цитруллином.",
              "Цитруллин малат, бета-аланин, кофеин, таурин, ароматизатор.",hit:true),
            P("Предтрены","IronFuel","Energy Gel","p24.png","40 г",110,0,4.3,90,"Лимон,Кола","1 пакетик",100,0,0,25,
              "Энергетический гель для бега и длинных тренировок.",
              "Мальтодекстрин, фруктоза, электролиты, ароматизатор.",isNew:true),
            P("Предтрены","NutriPro","Caffeine 200","p25.png","100 таблеток",590,0,4.5,72,"Без вкуса","1 таблетка",0,0,0,0,
              "Кофеин в таблетках для бодрости перед тренировкой.",
              "Кофеин безводный, наполнитель, стеарат кальция."),
            P("Протеин","ATHLET","Whey Protein Gold XL","p26.png","2,27 кг",3990,4590,4.8,205,"Шоколад,Ваниль,Клубника","30 г",120,24,1.5,2.5,
              "Большая упаковка любимого протеина ATHLET: 75 порций по 24 г белка.",
              "Концентрат сывороточного белка, лецитин, ароматизатор, подсластитель.",hit:true),
            P("Протеин","PowerLab","Whey Hydrolysate","p27.png","900 г",3290,0,4.7,58,"Шоколад,Молочный коктейль","30 г",115,26,0.5,1.5,
              "Гидролизат сывороточного белка: усваивается особенно быстро и мягко для желудка.",
              "Гидролизат сывороточного белка, ароматизатор, подсластитель.",isNew:true),
            P("Гейнеры","NutriPro","Lean Mass","p28.png","2 кг",2190,0,4.5,70,"Ваниль,Шоколад","100 г",350,30,3,48,
              "Гейнер с умеренным количеством углеводов для чистого набора массы.",
              "Белки молока, овсяная мука, какао, ферменты.",isNew:true),
            P("Витамины","ATHLET","Zinc Complex","p29.png","90 капсул",590,0,4.6,84,"Без вкуса","1 капсула",0,0,0,0,
              "Цинк и витамин B6 для иммунитета и гормонального баланса.",
              "Цитрат цинка, пиридоксин, капсула."),
            P("Витамины","PowerLab","Vitamin C 1000","p30.png","100 таблеток",690,790,4.7,110,"Апельсин","1 таблетка",5,0,0,1,
              "Витамин C 1000 мг в удобной жевательной таблетке.",
              "Аскорбиновая кислота, наполнитель, ароматизатор."),
            P("Батончики","NutriPro","Oat Bar","p31.png","45 г",95,0,4.2,160,"Овсянка-яблоко,Овсянка-ягоды","1 батончик",170,6,5,24,
              "Овсяный батончик с ягодами для лёгкого перекуса.",
              "Овсяные хлопья, сироп, яблоко, ягоды."),
            P("Батончики","ATHLET","Protein Wafer","p32.png","40 г",110,0,4.5,230,"Шоколад,Ваниль","1 вафля",150,12,6,13,
              "Хрустящая протеиновая вафля со сливочной начинкой.",
              "Белок молока, вафельная крошка, какао, подсластитель.",isNew:true),
            P("Предтрены","ATHLET","Pump Max","p33.png","360 г",1990,2390,4.7,98,"Тропик,Виноград","12 г",20,0,0,4,
              "Комплекс для пампинга и концентрации без лишней нервозности.",
              "Цитруллин, аргинин, бета-аланин, глицерол, кофеин.",isNew:true),
            P("Жиросжигатели","PowerLab","Fat Burner Thermo","p34.png","90 капсул",1490,0,4.3,121,"Без вкуса","2 капсулы",5,0,0,0,
              "Термогенный комплекс для тех, кто сушится и тренируется кардио.",
              "Зелёный чай, кофеин, перец чили, L-карнитин.",hit:true),
            P("Жиросжигатели","IronFuel","L-Carnitine Liquid","p35.png","500 мл",990,1190,4.5,76,"Вишня,Лимон","15 мл",20,0,0,5,
              "Жидкий L-карнитин для энергии на тренировке.",
              "L-карнитин тартрат, вода, лимонная кислота, ароматизатор."),
            P("Жиросжигатели","ATHLET","CLA 1000","p36.png","90 капсул",1290,0,4.4,64,"Без вкуса","3 капсулы",25,0,3,0,
              "Конъюгированная линолевая кислота для контроля веса.",
              "КЛК, желатин, глицерин."),
            P("Жиросжигатели","NutriPro","Green Tea Extract","p37.png","60 капсул",690,0,4.4,52,"Без вкуса","1 капсула",0,0,0,0,
              "Экстракт зелёного чая с EGCG, мягкий источник энергии.",
              "Экстракт Camellia sinensis, капсула.",isNew:true),
            P("Аксессуары","ATHLET","Shaker Pro 700","p38.png","700 мл",490,590,4.8,380,"Чёрный,Оранжевый","1 шт",0,0,0,0,
              "Шейкер с шариком-венчиком и герметичной крышкой. Не пропускает запахи.",
              "Пищевой пластик Tritan, без BPA.",hit:true),
            P("Аксессуары","PowerLab","Shaker Steel 600","p39.png","600 мл",990,0,4.6,95,"Серебро,Графит","1 шт",0,0,0,0,
              "Стальной шейкер держит температуру напитка до 6 часов.",
              "Нержавеющая сталь 304, силиконовая крышка.",isNew:true),
            P("Аксессуары","IronFuel","Water Bottle 1L","p40.png","1 л",390,0,4.5,140,"Голубой,Чёрный","1 шт",0,0,0,0,
              "Спортивная бутылка с мерной шкалой и откидной крышкой.",
              "Пищевой пластик Tritan, без BPA."),
        };
        for (int i = 0; i < list.Count; i++)
            list[i].ImageUrl = $"https://loremflickr.com/640/640/{PhotoKeywords(list[i].Category)}?lock={i + 1}";
        return list;
    }

    /// <summary>Ключевые слова для подбора тематического фото в интернете.</summary>
    static string PhotoKeywords(string cat) => cat switch
    {
        "Протеин" => "protein,powder,whey",
        "Креатин" => "creatine,supplement,gym",
        "Аминокислоты" => "bcaa,supplement,fitness",
        "Гейнеры" => "bodybuilding,protein,shake",
        "Витамины" => "vitamins,capsules,supplement",
        "Батончики" => "proteinbar,energybar,granola",
        "Предтрены" => "preworkout,energy,gym",
        "Жиросжигатели" => "fatburner,supplement,pills",
        _ => "shaker,bottle,gym"
    };

    static List<Review> SeedReviews(List<Product> products)
    {
        string[] authors = { "Алексей", "Мария", "Дмитрий", "Ольга", "Иван", "Екатерина", "Сергей", "Анна" };
        string[] texts =
        {
            "Отличное качество, беру не первый раз.",
            "Хорошо растворяется, вкус приятный. Рекомендую.",
            "Доставили быстро, упаковка целая. Результатом доволен.",
            "Цена адекватная, эффект заметен уже через пару недель.",
            "Нормальный товар за свои деньги, буду заказывать ещё.",
            "Брал по совету тренера, всё подошло."
        };
        var res = new List<Review>();
        foreach (var p in products)
            for (int k = 0; k < 3; k++)
                res.Add(new Review
                {
                    ProductId = p.Id,
                    Author = authors[(p.Id + k * 3) % authors.Length],
                    Stars = k == 2 && p.Id % 3 == 0 ? 4 : 5,
                    Text = texts[(p.Id + k) % texts.Length],
                    Date = DateTime.Now.AddDays(-(3 + p.Id % 9 + k * 11))
                });
        return res;
    }
}

/// <summary>Команды для карточек товаров (открыть товар, добавить в избранное).</summary>
public static class Cmds
{
    public static readonly Command<Product> Open = new(async p =>
    {
        if (p == null) return;
        if (Shell.Current.CurrentPage is ProductPage pp) await pp.Show(p.Id);
        else await Shell.Current.GoToAsync($"product?id={p.Id}");
    });
    public static readonly Command<Product> Add = new(async p =>
    {
        if (p == null) return;
        await Store.AddToCart(p.Id, p.FlavorList.FirstOrDefault() ?? "", 1);
        p.Added = true; Store.RaiseCartChanged();
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Task.Delay(1200); p.Added = false;
    });
    public static readonly Command<Product> Fav = new(async p => { if (p != null) await Store.ToggleFav(p); });
}
