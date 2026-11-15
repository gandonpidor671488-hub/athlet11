namespace Athlet;

/// <summary>Рисует штрихкод Code 128 (набор C, только цифры) для кода получения заказа.</summary>
public class BarcodeDrawable : IDrawable
{
    // ширины полос и пробелов (по порядку) для значений 0–106 (106 = стоп)
    static readonly string[] Patterns = { "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213", "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132", "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211", "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313", "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331", "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111", "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214", "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111", "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141", "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141", "114131", "311141", "411131", "211412", "211214", "211232", "2331112" };

    public string Code { get; set; } = "";

    /// <summary>Список ширин: бар, пробел, бар… в модулях.</summary>
    public static List<int> Encode(string digits)
    {
        if (digits.Length % 2 == 1) digits = "0" + digits;
        var values = new List<int> { 105 };                       // Start C
        for (int i = 0; i < digits.Length; i += 2)
            values.Add(int.Parse(digits.Substring(i, 2)));
        int sum = values[0];
        for (int i = 1; i < values.Count; i++) sum += i * values[i];
        values.Add(sum % 103);                                    // контрольная сумма
        values.Add(106);                                          // Stop
        var widths = new List<int>();
        foreach (var v in values) foreach (var ch in Patterns[v]) widths.Add(ch - '0');
        return widths;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);
        if (string.IsNullOrEmpty(Code)) return;
        var widths = Encode(Code);
        int modules = widths.Sum();
        float quiet = 8;
        float unit = (dirtyRect.Width - quiet * 2) / modules;
        float x = dirtyRect.X + quiet;
        bool bar = true;
        canvas.FillColor = Colors.Black;
        foreach (int w in widths)
        {
            if (bar) canvas.FillRectangle(x, dirtyRect.Y, w * unit, dirtyRect.Height);
            x += w * unit;
            bar = !bar;
        }
    }
}
