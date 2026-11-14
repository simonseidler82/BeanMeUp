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

    // ---------- App-Icon "Tasse Hell/Dunkel" (Variante C2) ----------

    static readonly PointF[] CupBody = { new(4, 13), new(23, 13), new(20, 28), new(7, 28) };
    static readonly Color TileTop = Color.FromArgb(0x33, 0x3D, 0x4D);
    static readonly Color TileBottom = Color.FromArgb(0x14, 0x1A, 0x23);
    static readonly Color CupShade = Color.FromArgb(0x55, 0x60, 0x70);
    static readonly Color SteamColor = Color.FromArgb(0xF0, 0x8A, 0x24);

    /// <summary>
    /// Zeichnet das App-Icon: dunkle Kachel, Tasse links weiß / rechts grau (Hell/Dunkel),
    /// oranger Dampf (= wach). Wird für Programm-Icon, Tray-Symbol und Fenster verwendet.
    /// </summary>
    public static void DrawAppIcon(Graphics g, RectangleF r, bool steaming = true)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(r.X, r.Y);
        float s = r.Width;

        using (var tile = RoundRect(new RectangleF(0.5f, 0.5f, s - 1, s - 1), s * 0.22f))
        using (var brush = new LinearGradientBrush(new RectangleF(0, 0, s, s), TileTop, TileBottom, 90f))
            g.FillPath(brush, tile);

        // Tasse im 32er-Raster, Rumpfmitte (x = 13,5) auf der Kachelmitte
        float k = s * 0.7f / 32f;
        g.TranslateTransform(s / 2 - 13.5f * k, s * 0.12f);
        g.ScaleTransform(k, k);

        var outer = g.Clip;
        g.SetClip(new RectangleF(-20, -20, 33.5f, 80), CombineMode.Intersect);
        CupShape(g, Color.White);
        g.Clip = outer;
        g.SetClip(new RectangleF(13.5f, -20, 60, 80), CombineMode.Intersect);
        CupShape(g, CupShade);
        g.Clip = outer;

        using (var outline = new Pen(Color.White, 1.4f) { LineJoin = LineJoin.Round })
            g.DrawPolygon(outline, CupBody);
        using (var handle = new Pen(Color.White, 3f))
            g.DrawArc(handle, 18, 15, 10, 9, -90, 180);

        if (steaming)
        {
            using var steam = new Pen(SteamColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            foreach (float x in new[] { 9f, 14f, 19f })
                g.DrawBezier(steam, x, 10.5f, x - 2.6f, 7.8f, x + 2.6f, 5.2f, x, 2.5f);
        }
        g.Restore(state);
    }

    static void CupShape(Graphics g, Color c)
    {
        using var brush = new SolidBrush(c);
        using var handle = new Pen(c, 3f);
        using var saucer = new Pen(c, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.FillPolygon(brush, CupBody);
        g.DrawArc(handle, 18, 15, 10, 9, -90, 180);
        g.DrawLine(saucer, 3, 30.2f, 24, 30.2f);
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

    /// <summary>Tray-Symbol: aktiv = App-Icon mit Dampf; inaktiv = ohne Dampf und halb transparent.</summary>
    public static Icon CreateTrayIcon(bool active)
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            if (active)
            {
                DrawAppIcon(g, new RectangleF(0, 0, 32, 32));
            }
            else
            {
                using var plain = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                using (var pg = Graphics.FromImage(plain))
                    DrawAppIcon(pg, new RectangleF(0, 0, 32, 32), steaming: false);
                using var faded = new ImageAttributes();
                faded.SetColorMatrix(new ColorMatrix { Matrix33 = 0.55f });
                g.DrawImage(plain, new Rectangle(0, 0, 32, 32), 0, 0, 32, 32, GraphicsUnit.Pixel, faded);
            }
        }

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
