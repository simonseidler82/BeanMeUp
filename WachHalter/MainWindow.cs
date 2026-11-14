using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace WachHalter;

/// <summary>
/// Hauptfenster im schlichten Windows-11-Stil: erscheint beim Start unten rechts und blendet sich
/// nach 12 s ins Tray aus (pausiert, solange die Maus darüber ist; jeder Klick hält es offen).
/// Komplett im Code aufgebaut – Visual Studio öffnet die Datei deshalb in der Codeansicht.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
sealed class MainWindow : Form
{
    const double AutoHideSeconds = 12;
    const float W = 340, M = 16, Content = W - 2 * M;

    static readonly (string Text, TimeSpan? Duration)[] Durations =
    [
        ("∞", null),
        ("30 Min.", TimeSpan.FromMinutes(30)),
        ("1 Std.", TimeSpan.FromHours(1)),
        ("2 Std.", TimeSpan.FromHours(2)),
        ("4 Std.", TimeSpan.FromHours(4)),
    ];

    static readonly string[] ModeNames = ["Manuell", "Zeitplan", "Sonnenstand"];

    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    readonly TrayApp _app;
    readonly float _scale;
    readonly System.Windows.Forms.Timer _anim = new() { Interval = 33 };
    readonly Stopwatch _clock = Stopwatch.StartNew();

    readonly Pill _awakeButton;
    readonly Pill[] _durationChips;
    readonly ToggleSwitch _displaySwitch, _simulateSwitch;
    readonly Pill _lightTile, _darkTile;
    readonly Pill[] _modeChips;
    readonly ToggleSwitch _autostartSwitch;
    readonly Font _titleFont, _sectionFont, _smallFont;
    readonly Rectangle _header, _closeRect;

    bool _closeHover, _autoHide, _fadingOut, _allowClose;
    double _autoHideLeft, _lastTick;
    string _themeStatus = "";

    int S(float v) => (int)Math.Round(v * _scale);

    public MainWindow(TrayApp app)
    {
        _app = app;
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
            _scale = g.DpiX / 96f;

        Text = "WachHalter";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        DoubleBuffered = true;
        ClientSize = new Size(S(W), S(530));
        Font = new Font("Segoe UI", S(12.5f), GraphicsUnit.Pixel);
        _titleFont = new Font("Segoe UI Semibold", S(15), GraphicsUnit.Pixel);
        _sectionFont = new Font("Segoe UI Semibold", S(11), GraphicsUnit.Pixel);
        _smallFont = new Font("Segoe UI", S(11.5f), GraphicsUnit.Pixel);
        _header = new Rectangle(0, 0, S(W), S(64));
        _closeRect = new Rectangle(S(W - 40), S(14), S(28), S(28));

        // --- Wach halten ---
        _awakeButton = Add(new Pill(), M, 106, Content, 34, () => _app.ToggleAwake());

        _durationChips = new Pill[Durations.Length];
        float chipW = (Content - 4 * 4) / 5f;
        var chipFont = new Font("Segoe UI", S(11.5f), GraphicsUnit.Pixel);
        for (int i = 0; i < Durations.Length; i++)
        {
            var duration = Durations[i].Duration;
            _durationChips[i] = Add(new Pill { Text = Durations[i].Text, Font = chipFont },
                M + i * (chipW + 4), 146, chipW, 28, () => _app.StartAwake(duration));
        }

        _displaySwitch = Add(new ToggleSwitch { Text = "Bildschirm eingeschaltet lassen" }, M, 182, Content, 26, () => _app.ToggleDisplay());
        _simulateSwitch = Add(new ToggleSwitch { Text = "Aktivität simulieren" }, M, 212, Content, 26, () => _app.ToggleSimulateActivity());

        // --- Design ---
        float half = (Content - 4) / 2f;
        _lightTile = Add(new Pill { Text = "Hell", Icon = Art.DrawSun }, M, 286, half, 34, () => _app.ChooseTheme(false));
        _darkTile = Add(new Pill { Text = "Dunkel", Icon = Art.DrawMoon }, M + half + 4, 286, half, 34, () => _app.ChooseTheme(true));

        _modeChips = new Pill[ModeNames.Length];
        float modeW = (Content - 2 * 4) / 3f;
        for (int i = 0; i < ModeNames.Length; i++)
        {
            var mode = (ThemeMode)i;
            _modeChips[i] = Add(new Pill { Text = ModeNames[i], Font = chipFont },
                M + i * (modeW + 4), 326, modeW, 28, () => _app.SetMode(mode));
        }

        // --- Allgemein ---
        _autostartSwitch = Add(new ToggleSwitch { Text = "Mit Windows starten" }, M, 414, Content, 26, () => _app.ToggleAutostart());
        Add(new Pill { Text = "Weitere Einstellungen …" }, M, 450, Content - 104, 30, () => _app.ShowSettings());
        Add(new Pill { Text = "Beenden", Danger = true }, W - M - 100, 450, 100, 30, () => _app.Quit());

        _anim.Tick += (_, _) => Animate();
        RefreshState();
    }

    T Add<T>(T control, float x, float y, float w, float h, Action onClick) where T : Control
    {
        control.Bounds = new Rectangle(S(x), S(y), S(w), S(h));
        control.Click += (_, _) =>
        {
            StopAutoHide();
            onClick();
        };
        Controls.Add(control);
        return control;
    }

    // ---------- Anzeigen / Verbergen ----------

    public void ShowWindow(bool autoHide)
    {
        _fadingOut = false;
        _autoHide = autoHide;
        _autoHideLeft = AutoHideSeconds;

        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(area.Right - Width - S(12), Math.Max(area.Top, area.Bottom - Height - S(12)));

        RefreshState();
        if (!Visible)
        {
            Opacity = 0;
            Show();
        }
        Activate();
        _lastTick = _clock.Elapsed.TotalSeconds;
        _anim.Start();
    }

    public void HideWindow()
    {
        if (!Visible) return;
        _autoHide = false;
        _fadingOut = true;
        _anim.Start();
    }

    public void AllowClose() => _allowClose = true;

    void StopAutoHide()
    {
        if (!_autoHide) return;
        _autoHide = false;
        Invalidate();
    }

    /// <summary>Alle Anzeigen an den aktuellen Zustand anpassen.</summary>
    public void RefreshState()
    {
        Palette.Refresh(ThemeRegistry.IsDark(ThemeTarget.NurApps));
        BackColor = Palette.Current.Bg;

        var awake = _app.Awake;
        _awakeButton.Primary = !awake.Active;
        _awakeButton.Text = awake.Active ? "Wach halten beenden" : "Wach halten aktivieren";
        for (int i = 0; i < Durations.Length; i++)
            _durationChips[i].Selected = awake.Active && _app.LastDuration == Durations[i].Duration;
        _displaySwitch.Checked = _app.Settings.KeepDisplayOn;
        _simulateSwitch.Checked = _app.Settings.SimulateActivity;

        bool dark = _app.Theme.IsDarkNow;
        _lightTile.Selected = !dark;
        _darkTile.Selected = dark;
        for (int i = 0; i < _modeChips.Length; i++)
            _modeChips[i].Selected = _app.Settings.ThemeMode == (ThemeMode)i;
        _themeStatus = _app.ThemeStatus();
        _autostartSwitch.Checked = Autostart.IsEnabled;

        foreach (Control c in Controls) c.Invalidate();
        Invalidate();
    }

    // ---------- Ein-/Ausblenden und Countdown (Timer läuft nur dafür) ----------

    void Animate()
    {
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Min(0.2, now - _lastTick);
        _lastTick = now;

        if (_fadingOut)
        {
            Opacity = Math.Max(0, Opacity - 0.12);
            if (Opacity <= 0)
            {
                _fadingOut = false;
                _anim.Stop();
                Hide();
            }
            return;
        }

        if (Opacity < 1) Opacity = Math.Min(1, Opacity + 0.15);

        if (_autoHide)
        {
            if (!Bounds.Contains(Cursor.Position)) _autoHideLeft -= dt;
            Invalidate(new Rectangle(0, S(492), ClientSize.Width, S(38)));
            if (_autoHideLeft <= 0) HideWindow();
        }
        else if (Opacity >= 1)
        {
            _anim.Stop(); // nichts mehr zu tun -> keine CPU-Last
        }
    }

    // ---------- Zeichnen ----------

    protected override void OnPaint(PaintEventArgs e)
    {
        var p = Palette.Current;
        var g = e.Graphics;
        g.Clear(p.Bg);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        bool active = _app.Awake.Active;

        // Kopfzeile: Symbol, Titel, Status
        Art.DrawCup(g, new RectangleF(S(M), S(16), S(30), S(30)), active ? p.Accent : p.Sub, active, 0.6f);
        using (var textBrush = new SolidBrush(p.Text))
            g.DrawString("WachHalter", _titleFont, textBrush, S(56), S(12));

        using (var dot = new SolidBrush(active ? Color.FromArgb(16, 160, 80) : p.Sub))
            g.FillEllipse(dot, S(58), S(39), S(7), S(7));
        using (var subBrush = new SolidBrush(p.Sub))
            g.DrawString(AwakeStatus(), _smallFont, subBrush, S(68), S(34));

        // Schließen (blendet nur ins Tray aus)
        if (_closeHover)
        {
            using var hoverBrush = new SolidBrush(p.CardHover);
            using var path = Art.RoundRect(_closeRect, S(4));
            g.FillPath(hoverBrush, path);
        }
        using (var xPen = new Pen(p.Sub, 1.2f * _scale))
        {
            int m = S(10);
            g.DrawLine(xPen, _closeRect.Left + m, _closeRect.Top + m, _closeRect.Right - m, _closeRect.Bottom - m);
            g.DrawLine(xPen, _closeRect.Right - m, _closeRect.Top + m, _closeRect.Left + m, _closeRect.Bottom - m);
        }

        // Trennlinien und Abschnittstitel
        using (var line = new Pen(p.Border))
        {
            g.DrawLine(line, S(M), S(64), S(W - M), S(64));
            g.DrawLine(line, S(M), S(252), S(W - M), S(252));
            g.DrawLine(line, S(M), S(384), S(W - M), S(384));
        }

        using var secBrush = new SolidBrush(p.Sub);
        g.DrawString("Wach halten", _sectionFont, secBrush, S(M - 1), S(80));
        g.DrawString("Design", _sectionFont, secBrush, S(M - 1), S(262));
        g.DrawString(_themeStatus, _smallFont, secBrush, S(M - 1), S(360));
        g.DrawString("Allgemein", _sectionFont, secBrush, S(M - 1), S(394));

        // Countdown bis zum automatischen Ausblenden
        if (_autoHide)
        {
            var bar = new RectangleF(S(M), S(496), S(Content), Math.Max(1, S(2)));
            using (var track = new SolidBrush(p.Border)) g.FillRectangle(track, bar);
            using (var fill = new SolidBrush(p.Accent))
                g.FillRectangle(fill, bar.X, bar.Y, bar.Width * (float)Math.Max(0, _autoHideLeft / AutoHideSeconds), bar.Height);

            string hint = Bounds.Contains(Cursor.Position)
                ? "Ausblenden pausiert"
                : $"Wird in {Math.Ceiling(Math.Max(0, _autoHideLeft))} s in den Infobereich verschoben";
            g.DrawString(hint, _smallFont, secBrush, S(M - 1), S(502));
        }
    }

    string AwakeStatus() =>
        !_app.Awake.Active ? "Inaktiv – Energiesparmodus erlaubt"
        : _app.Awake.Until is { } until ? $"Aktiv – noch {TrayApp.Remaining(until)}"
        : "Aktiv – ohne Zeitlimit";

    // ---------- Maus & Tastatur ----------

    protected override void OnMouseMove(MouseEventArgs e)
    {
        bool hover = _closeRect.Contains(e.Location);
        if (hover != _closeHover)
        {
            _closeHover = hover;
            Invalidate(_closeRect);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_closeHover)
        {
            _closeHover = false;
            Invalidate(_closeRect);
        }
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        StopAutoHide();
        if (_closeRect.Contains(e.Location))
        {
            HideWindow();
        }
        else if (_header.Contains(e.Location))
        {
            // Fenster an der Kopfzeile verschieben
            ReleaseCapture();
            SendMessage(Handle, 0xA1 /* WM_NCLBUTTONDOWN */, (IntPtr)2 /* HTCAPTION */, IntPtr.Zero);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) HideWindow();
        base.OnKeyDown(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true; // Alt+F4 -> nur ausblenden
            HideWindow();
        }
        base.OnFormClosing(e);
    }

    // ---------- Fensterstil: Schatten + runde Ecken (Windows 11) ----------

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int round = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(Handle, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref round, sizeof(int));
        }
        catch { /* ältere Windows-Versionen: eckig */ }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _anim.Dispose();
            _titleFont.Dispose();
            _sectionFont.Dispose();
            _smallFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
