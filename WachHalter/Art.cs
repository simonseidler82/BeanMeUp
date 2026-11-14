using System.Drawing.Drawing2D;
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

/// <summary>Zeichenfunktionen: Tasse, Sonne, Mond, abgerundete Rechtecke, Tray-Icon.</summary>
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

    /// <summary>Kaffeetasse im 32er-Raster, auf <paramref name="r"/> skaliert. Dampf animiert über <paramref name="phase"/>.</summary>
    public static void DrawCup(Graphics g, RectangleF r, Color color, bool steaming, float phase)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(r.X, r.Y);
        g.ScaleTransform(r.Width / 32f, r.Height / 32f);

        using var brush = new SolidBrush(color);
        using var pen = new Pen(color, 3f);
        using var saucer = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.FillPolygon(brush, new PointF[] { new(4, 13), new(23, 13), new(20, 28), new(7, 28) });
        g.DrawArc(pen, 18, 15, 10, 9, -90, 180);
        g.DrawLine(saucer, 3, 30.2f, 24, 30.2f);

        if (steaming)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = 9 + i * 5;
                float w = (float)Math.Sin(phase + i * 1.3) * 1.6f;
                int alpha = Math.Max(60, Math.Min(255, (int)(170 + 85 * Math.Sin(phase * 0.8 + i))));
                using var steam = new Pen(Color.FromArgb(alpha, color), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawBezier(steam, x, 11, x - 3 + w, 8, x + 3 - w, 5, x + w * 0.5f, 2);
            }
        }
        g.Restore(state);
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

    public static Icon CreateTrayIcon(bool active)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
            DrawCup(g, new RectangleF(0, 0, 32, 32),
                active ? Palette.WindowsAccent() : Color.FromArgb(150, 150, 150), active, 0.6f);

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
