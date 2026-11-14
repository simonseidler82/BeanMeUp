using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WachHalter;

/// <summary>
/// Zurückhaltende Farben im Windows-11-Stil. Hell/Dunkel folgt Windows,
/// die Akzentfarbe ist die von Windows (Einstellungen › Personalisierung › Farben).
/// </summary>
sealed class Palette
{
    public Color Bg, Card, CardHover, Border, Text, Sub, Danger;
    public Color Accent, AccentText, AccentTint, OnAccent;

    static readonly Palette LightBase = new()
    {
        Bg = Hex(0xF3F3F3), Card = Hex(0xFBFBFB), CardHover = Hex(0xF0F0F0), Border = Hex(0xE0E0E0),
        Text = Hex(0x1B1B1B), Sub = Hex(0x616161), Danger = Hex(0xC42B1C),
    };

    static readonly Palette DarkBase = new()
    {
        Bg = Hex(0x202020), Card = Hex(0x2B2B2B), CardHover = Hex(0x323232), Border = Hex(0x3A3A3A),
        Text = Hex(0xFFFFFF), Sub = Hex(0xA0A0A0), Danger = Hex(0xFF99A4),
    };

    public static Palette Current { get; private set; } = Build(dark: false);

    public static void Refresh(bool dark) => Current = Build(dark);

    static Palette Build(bool dark)
    {
        var b = dark ? DarkBase : LightBase;
        var accent = WindowsAccent();
        return new Palette
        {
            Bg = b.Bg, Card = b.Card, CardHover = b.CardHover, Border = b.Border,
            Text = b.Text, Sub = b.Sub, Danger = b.Danger,
            Accent = accent,
            AccentText = dark ? Art.Mix(accent, Color.White, 0.45f) : accent,
            AccentTint = Art.Mix(b.Card, accent, dark ? 0.22f : 0.10f),
            OnAccent = Luma(accent) > 0.6 ? Color.Black : Color.White,
        };
    }

    /// <summary>Windows-Akzentfarbe (Registry-Wert im Format 0xAABBGGRR), sonst Windows-Blau.</summary>
    public static Color WindowsAccent()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int v)
                return Color.FromArgb(v & 255, (v >> 8) & 255, (v >> 16) & 255);
        }
        catch { /* Standardfarbe verwenden */ }
        return Hex(0x0067C0);
    }

    static double Luma(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255;
    static Color Hex(int rgb) => Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
}

/// <summary>Zeichenfunktionen: App-Icon, Sonne, Mond, abgerundete Rechtecke, Tray- und .ico-Erzeugung.</summary>
static class Art
{
    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);

    public static GraphicsPath RoundRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Color Mix(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

    // ---------- App-Icon "Dämmerung" ----------

    static Color Rgb(int rgb, int alpha = 255) =>
        Color.FromArgb(alpha, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);

    static readonly Color DuskTop = Rgb(0xFFC27A);     // Abendrot
    static readonly Color DuskBottom = Rgb(0x3C4A7A);  // Nachtblau
    static readonly Color Ink = Rgb(0x2B2B33);
    static readonly Color SunColor = Rgb(0xFFB627);
    static readonly Color RayColor = Rgb(0xFFA620);
    static readonly Color MoonColor = Rgb(0xF4ECCF);
    static readonly Color CoffeeColor = Rgb(0x6B4430);
    static readonly Color BlushColor = Rgb(0xFF8FA3, 170);

    /// <summary>
    /// Zeichnet das App-Icon "Dämmerung": Verlauf von Abendrot zu Nachtblau, Sonne und Mond,
    /// davor eine lächelnde Kaffeetasse. <paramref name="awake"/> = offene Augen (Wach halten aktiv),
    /// sonst verschlafene Augen. Wird für Programm-Icon, Tray-Symbol und Fenster verwendet.
    /// </summary>
    public static void DrawAppIcon(Graphics g, RectangleF r, bool awake = true)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(r.X, r.Y);
        float s = r.Width;

        using (var tile = RoundRect(new RectangleF(0.5f, 0.5f, s - 1, s - 1), s * 0.22f))
        using (var sky = new LinearGradientBrush(new RectangleF(0, 0, s, s), DuskTop, DuskBottom, 55f))
            g.FillPath(sky, tile);

        // Ab hier im 100er-Raster
        g.ScaleTransform(s / 100f, s / 100f);

        // Sonne oben links
        using (var rays = Round(new Pen(RayColor, 2.3f)))
            for (int k = 0; k < 8; k++)
            {
                double a = k * Math.PI / 4;
                g.DrawLine(rays,
                    26 + (float)Math.Cos(a) * 8.7f, 22 + (float)Math.Sin(a) * 8.7f,
                    26 + (float)Math.Cos(a) * 12f, 22 + (float)Math.Sin(a) * 12f);
            }
        Dot(g, SunColor, 26, 22, 6);

        // Mond oben rechts: der Ausschnitt wird mit dem Himmelsverlauf gemalt
        Dot(g, MoonColor, 70, 24, 8);
        using (var sky = new LinearGradientBrush(new RectangleF(0, 0, 100, 100), DuskTop, DuskBottom, 55f))
            g.FillEllipse(sky, 70 - 8 * 0.35f, 24 - 8 * 1.35f, 8 * 1.9f, 8 * 1.9f);
        Dot(g, MoonColor, 87.25f, 41.25f, 1.25f);
        Dot(g, MoonColor, 83, 13, 1);

        // Tasse
        using (var handle = new Pen(Ink, 5))
            g.DrawArc(handle, 62, 50, 18, 20, -90, 180);
        using (var cup = new GraphicsPath())
        {
            cup.AddLine(22, 42, 72, 42);
            cup.AddLine(72, 42, 66, 80);
            cup.AddArc(56, 72, 10, 10, 0, 90);
            cup.AddLine(61, 82, 33, 82);
            cup.AddArc(28, 72, 10, 10, 90, 90);
            cup.CloseFigure();
            using var white = new SolidBrush(Color.White);
            using var outline = Round(new Pen(Ink, 2.6f));
            g.FillPath(white, cup);
            g.DrawPath(outline, cup);
        }
        using (var coffee = new SolidBrush(CoffeeColor))
            g.FillEllipse(coffee, 25, 39, 44, 7);

        // Gesicht
        if (awake)
        {
            Eye(g, 38, 58, 4.3f);
            Eye(g, 56, 58, 4.3f);
        }
        else
        {
            using var lids = Round(new Pen(Ink, 2.8f));
            g.DrawArc(lids, 33.5f, 54, 9, 7, 20, 140);
            g.DrawArc(lids, 51.5f, 54, 9, 7, 20, 140);
        }
        using (var mouth = Round(new Pen(Ink, 3)))
            g.DrawArc(mouth, 42.5f, 61.5f, 9, 7.2f, 25, 130);
        using (var blush = new SolidBrush(BlushColor))
        {
            g.FillEllipse(blush, 27, 64.6f, 8, 4.8f);
            g.FillEllipse(blush, 59, 64.6f, 8, 4.8f);
        }

        g.Restore(state);
    }

    static void Eye(Graphics g, float cx, float cy, float r)
    {
        Dot(g, Ink, cx, cy, r);
        using var shine = new SolidBrush(Color.White);
        g.FillEllipse(shine, cx - r * 0.55f, cy - r * 0.7f, r * 0.7f, r * 0.7f);
    }

    static void Dot(Graphics g, Color c, float cx, float cy, float r)
    {
        using var brush = new SolidBrush(c);
        g.FillEllipse(brush, cx - r, cy - r, 2 * r, 2 * r);
    }

    static Pen Round(Pen pen)
    {
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;
        pen.LineJoin = LineJoin.Round;
        return pen;
    }

    static Icon? _windowIcon;
    /// <summary>Icon für Fenster (Titelleiste, Alt+Tab).</summary>
    public static Icon WindowIcon => _windowIcon ??= CreateTrayIcon(active: true);

    /// <summary>
    /// Speichert das App-Icon als .ico mit allen gängigen Größen (PNG-komprimiert).
    /// Aufruf: WachHalter.exe --icon-export Pfad\WachHalter.ico
    /// </summary>
    public static void SaveIco(string path)
    {
        int[] sizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256];
        var images = sizes.Select(size =>
        {
            using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
                DrawAppIcon(g, new RectangleF(0, 0, size, size));
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }).ToArray();

        using var writer = new BinaryWriter(File.Create(path));
        writer.Write((short)0);            // reserviert
        writer.Write((short)1);            // Typ: Icon
        writer.Write((short)sizes.Length); // Anzahl Bilder
        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
            writer.Write(dim);
            writer.Write(dim);
            writer.Write((byte)0);         // keine Palette
            writer.Write((byte)0);
            writer.Write((short)1);        // Farbebenen
            writer.Write((short)32);       // Bit pro Pixel
            writer.Write(images[i].Length);
            writer.Write(offset);
            offset += images[i].Length;
        }
        foreach (var image in images)
            writer.Write(image);
    }

    public static void DrawSun(Graphics g, RectangleF r, Color fg, Color bg)
    {
        float cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2, radius = r.Width * 0.22f;
        using var brush = new SolidBrush(fg);
        using var pen = new Pen(fg, r.Width * 0.08f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);
        for (int k = 0; k < 8; k++)
        {
            double a = k * Math.PI / 4;
            float inner = radius * 1.5f, outer = r.Width * 0.48f;
            g.DrawLine(pen,
                cx + (float)Math.Cos(a) * inner, cy + (float)Math.Sin(a) * inner,
                cx + (float)Math.Cos(a) * outer, cy + (float)Math.Sin(a) * outer);
        }
    }

    public static void DrawMoon(Graphics g, RectangleF r, Color fg, Color bg)
    {
        float cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2, radius = r.Width * 0.42f;
        using var front = new SolidBrush(fg);
        using var cut = new SolidBrush(bg);
        g.FillEllipse(front, cx - radius, cy - radius, radius * 2, radius * 2);
        float cr = radius * 0.9f;
        g.FillEllipse(cut, cx - radius * 0.35f, cy - radius * 1.25f, cr * 2, cr * 2);
    }

    /// <summary>
    /// Tray-Symbol in voller Farbe (gut sichtbar auch auf dunkler Taskleiste):
    /// aktiv = wache Augen, inaktiv = verschlafene Augen.
    /// </summary>
    public static Icon CreateTrayIcon(bool active)
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
            DrawAppIcon(g, new RectangleF(0, 0, 32, 32), awake: active);

        IntPtr handle = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
