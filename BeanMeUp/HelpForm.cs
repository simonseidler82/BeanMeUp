using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace BeanMeUp;

/// <summary>Fenster "Hilfe &amp; Info": kurze Bedienungsanleitung, Version und Credits.</summary>
[System.ComponentModel.DesignerCategory("Code")]
sealed class HelpForm : Form
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    // Abschnitte der Hilfe – Texte aus der Übersetzungstabelle (Strings.cs)
    static readonly string[] SectionKeys = ["help.awake", "help.sim", "help.theme", "help.window", "help.update", "help.portable"];

    readonly float _scale;
    readonly Font _titleFont, _sectionFont, _smallFont;

    int S(float v) => (int)Math.Round(v * _scale);

    public HelpForm()
    {
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
            _scale = g.DpiX / 96f;

        var p = Palette.Current;
        string version = typeof(HelpForm).Assembly.GetName().Version?.ToString(3) ?? "";

        Text = $"{TrayApp.AppName} – {L.T("help.title")}";
        Icon = Art.WindowIcon;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = p.Bg;
        ForeColor = p.Text;
        KeyPreview = true;
        Font = UiFonts.Create(S(12.5f));
        _titleFont = new Font("Segoe UI Semibold", S(20), GraphicsUnit.Pixel); // "Bean me up" bleibt lateinisch
        _sectionFont = UiFonts.Create(S(13), semibold: true);
        _smallFont = UiFonts.Create(S(11.5f));

        int width = S(440);
        var flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(S(22), S(18), S(22), S(18)),
        };

        // Kopf: Icon, Name, Version
        var header = new Panel { Size = new Size(width, S(64)), Margin = new Padding(0, 0, 0, S(4)) };
        header.Paint += (_, e) =>
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Art.DrawAppIcon(e.Graphics, new RectangleF(0, S(4), S(56), S(56)));
            using var textBrush = new SolidBrush(p.Text);
            using var subBrush = new SolidBrush(p.Sub);
            e.Graphics.DrawString(TrayApp.AppName, _titleFont, textBrush, S(68), S(6));
            using var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            e.Graphics.DrawString(L.T("help.subtitle", version), _smallFont, subBrush,
                new RectangleF(S(70), S(36), width - S(70), S(20)), sf);
        };
        flow.Controls.Add(header);

        foreach (var key in SectionKeys)
        {
            string title = L.T(key + ".t"), body = L.T(key + ".b");
            flow.Controls.Add(new Label
            {
                Text = title,
                UseMnemonic = false, // "&" wörtlich anzeigen
                Font = _sectionFont,
                ForeColor = p.Text,
                AutoSize = true,
                Margin = new Padding(0, S(12), 0, S(3)),
            });
            flow.Controls.Add(new Label
            {
                Text = body,
                UseMnemonic = false,
                ForeColor = p.Sub,
                AutoSize = true,
                MaximumSize = new Size(width, 0),
                Margin = new Padding(0),
            });
        }

        // Trennlinie + Credits + Schließen
        var footer = new Panel { Size = new Size(width, S(56)), Margin = new Padding(0, S(16), 0, 0) };
        var close = new Pill { Text = L.T("help.close"), Primary = true, Bounds = new Rectangle(width - S(130), S(18), S(130), S(32)) };
        close.Click += (_, _) => Close();
        footer.Controls.Add(close);
        footer.Paint += (_, e) =>
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var line = new Pen(p.Border);
            using var subBrush = new SolidBrush(p.Sub);
            e.Graphics.DrawLine(line, 0, 0, width, 0);
            e.Graphics.DrawString(L.Credits, _smallFont, subBrush, 0, S(26));
        };
        flow.Controls.Add(footer);

        Controls.Add(flow);
        AcceptButton = null;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Close();
        base.OnKeyDown(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (Palette.Current.Bg.GetBrightness() < 0.5f)
        {
            try
            {
                int on = 1; // dunkle Titelleiste passend zum Dunkel-Design
                DwmSetWindowAttribute(Handle, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref on, sizeof(int));
            }
            catch { /* ältere Windows-Versionen */ }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _sectionFont.Dispose();
            _smallFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
