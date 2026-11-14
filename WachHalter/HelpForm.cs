using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace WachHalter;

/// <summary>Fenster "Hilfe &amp; Info": kurze Bedienungsanleitung, Version und Credits.</summary>
[System.ComponentModel.DesignerCategory("Code")]
sealed class HelpForm : Form
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    static readonly (string Title, string Body)[] Sections =
    [
        ("Wach halten",
         "Verhindert, dass Windows in den Energiesparmodus geht – ohne Zeitlimit (∞) oder für 30 Minuten " +
         "bis 4 Stunden. Mit „Bildschirm eingeschaltet lassen“ bleibt auch der Monitor an. " +
         "Sobald WachHalter beendet wird, gilt wieder das normale Energieverhalten."),
        ("Aktivität simulieren",
         "Bewegt die Maus nach 25 Sekunden ohne Eingabe unsichtbar um 1 Pixel hin und zurück. Das verhindert " +
         "zusätzlich Bildschirmschoner, automatische Sperre und den Status „Abwesend“ (z. B. in Teams). " +
         "Standardmäßig aus – bitte die Richtlinien deines Arbeitgebers beachten."),
        ("Hell / Dunkel",
         "„Hell“ und „Dunkel“ stellen Windows sofort um. In den Modi „Zeitplan“ und „Sonnenstand“ wechselt " +
         "WachHalter automatisch; eine eigene Wahl gilt dann bis zum nächsten geplanten Wechsel. Uhrzeiten, " +
         "Standort und ob Apps und/oder Taskleiste umgestellt werden: „Weitere Einstellungen“."),
        ("Fenster & Infobereich",
         "Beim Start erscheint das Fenster und verschwindet nach 12 Sekunden in den Infobereich der Taskleiste " +
         "(Maus darüber hält es offen). Ein Klick auf das Tassen-Symbol zeigt „Anzeigen“ und „Verbergen“. " +
         "Esc oder ✕ blendet aus, F1 öffnet diese Hilfe, „Beenden“ schließt WachHalter."),
        ("Portabel & Datenschutz",
         "Keine Installation, keine Adminrechte, keine Internetverbindung. Die Einstellungen liegen in " +
         "„WachHalter.ini“ neben der EXE. Zum Entfernen „Mit Windows starten“ ausschalten und die EXE löschen."),
    ];

    readonly float _scale;
    readonly Font _titleFont, _sectionFont, _smallFont;

    int S(float v) => (int)Math.Round(v * _scale);

    public HelpForm()
    {
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
            _scale = g.DpiX / 96f;

        var p = Palette.Current;
        string version = typeof(HelpForm).Assembly.GetName().Version?.ToString(3) ?? "";

        Text = "WachHalter – Hilfe & Info";
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
        Font = new Font("Segoe UI", S(12.5f), GraphicsUnit.Pixel);
        _titleFont = new Font("Segoe UI Semibold", S(18), GraphicsUnit.Pixel);
        _sectionFont = new Font("Segoe UI Semibold", S(13), GraphicsUnit.Pixel);
        _smallFont = new Font("Segoe UI", S(11.5f), GraphicsUnit.Pixel);

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
        var header = new Panel { Size = new Size(width, S(56)), Margin = new Padding(0, 0, 0, S(4)) };
        header.Paint += (_, e) =>
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Art.DrawAppIcon(e.Graphics, new RectangleF(0, S(4), S(48), S(48)));
            using var textBrush = new SolidBrush(p.Text);
            using var subBrush = new SolidBrush(p.Sub);
            e.Graphics.DrawString("WachHalter", _titleFont, textBrush, S(60), S(6));
            e.Graphics.DrawString($"Version {version} · Wach halten und Hell/Dunkel für Windows", _smallFont, subBrush, S(62), S(32));
        };
        flow.Controls.Add(header);

        foreach (var (title, body) in Sections)
        {
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
        var close = new Pill { Text = "Schließen", Primary = true, Bounds = new Rectangle(width - S(110), S(18), S(110), S(32)) };
        close.Click += (_, _) => Close();
        footer.Controls.Add(close);
        footer.Paint += (_, e) =>
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var line = new Pen(p.Border);
            using var subBrush = new SolidBrush(p.Sub);
            e.Graphics.DrawLine(line, 0, 0, width, 0);
            e.Graphics.DrawString(MainWindow.Credits, _smallFont, subBrush, 0, S(18));
            e.Graphics.DrawString($"© {DateTime.Now.Year} Simon Seidler", _smallFont, subBrush, 0, S(36));
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
