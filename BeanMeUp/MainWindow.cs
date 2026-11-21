using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace BeanMeUp;

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

    // Texte kommen aus der Übersetzungstabelle (Strings.cs)
    static readonly (string Text, TimeSpan? Duration, string Tip)[] Durations =
    [
        ("∞", null, L.T("tip.durInf")),
        (L.T("dur.30"), TimeSpan.FromMinutes(30), L.T("tip.dur30")),
        (L.T("dur.60"), TimeSpan.FromHours(1), L.T("tip.dur60")),
        (L.T("dur.120"), TimeSpan.FromHours(2), L.T("tip.dur120")),
        (L.T("dur.240"), TimeSpan.FromHours(4), L.T("tip.dur240")),
    ];

    static readonly (string Text, string Tip)[] Modes =
    [
        (L.T("mode.manual"), L.T("tip.manual")),
        (L.T("mode.schedule"), L.T("tip.schedule")),
        (L.T("mode.sun"), L.T("tip.sun")),
    ];

    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    readonly TrayApp _app;
    readonly float _scale;
    readonly System.Windows.Forms.Timer _anim = new() { Interval = 33 };
    readonly Stopwatch _clock = Stopwatch.StartNew();
    readonly ToolTip _tips = new() { AutoPopDelay = 15000, InitialDelay = 600 };

    readonly Pill _awakeButton;
    readonly Pill[] _durationChips;
    readonly ToggleSwitch _displaySwitch, _simulateSwitch;
    readonly Pill _lightTile, _darkTile;
    readonly Pill[] _modeChips;
    readonly ToggleSwitch _autostartSwitch;
    readonly Font _titleFont, _sectionFont, _smallFont, _helpFont;
    readonly Rectangle _header, _closeRect, _helpRect;

    bool _closeHover, _helpHover, _autoHide, _fadingOut, _allowClose, _mouseMovedSinceShow;
    Point _cursorAtShow;
    double _autoHideLeft, _lastTick;
    string _themeStatus = "";

    int S(float v) => (int)Math.Round(v * _scale);

    public MainWindow(TrayApp app)
    {
        _app = app;
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
            _scale = g.DpiX / 96f;

        Text = TrayApp.AppName;
        Icon = Art.WindowIcon;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        DoubleBuffered = true;
        ClientSize = new Size(S(W), S(530));
        Font = UiFonts.Create(S(12.5f));
        _titleFont = new Font("Segoe UI Semibold", S(17), GraphicsUnit.Pixel); // "Bean me up" bleibt lateinisch
        _sectionFont = UiFonts.Create(S(11), semibold: true);
        _smallFont = UiFonts.Create(S(11.5f));
        _helpFont = new Font("Segoe UI Semibold", S(14), GraphicsUnit.Pixel);
        _header = new Rectangle(0, 0, S(W), S(64));
        _closeRect = new Rectangle(S(W - 40), S(14), S(28), S(28));
        _helpRect = new Rectangle(S(W - 72), S(14), S(28), S(28));

        // --- Wach halten ---
        _awakeButton = Add(new Pill(), M, 106, Content, 34, () => _app.ToggleAwake(), L.T("tip.awake"));

        _durationChips = new Pill[Durations.Length];
        float chipW = (Content - 4 * 4) / 5f;
        var chipFont = UiFonts.Create(S(11.5f));
        for (int i = 0; i < Durations.Length; i++)
        {
            var duration = Durations[i].Duration;
            _durationChips[i] = Add(new Pill { Text = Durations[i].Text, Font = chipFont },
                M + i * (chipW + 4), 146, chipW, 28, () => _app.StartAwake(duration), Durations[i].Tip);
        }

        _displaySwitch = Add(new ToggleSwitch { Text = L.T("display") }, M, 182, Content, 26,
            () => _app.ToggleDisplay(), L.T("tip.display"));
        _simulateSwitch = Add(new ToggleSwitch { Text = L.T("simulate") }, M, 212, Content, 26,
            () => _app.ToggleSimulateActivity(), L.T("tip.simulate"));

        // --- Design ---
        float half = (Content - 4) / 2f;
        string themeTip = L.T("tip.theme");
        _lightTile = Add(new Pill { Text = L.T("light"), Icon = Art.DrawSun }, M, 286, half, 34, () => _app.ChooseTheme(false), themeTip);
        _darkTile = Add(new Pill { Text = L.T("dark"), Icon = Art.DrawMoon }, M + half + 4, 286, half, 34, () => _app.ChooseTheme(true), themeTip);

        _modeChips = new Pill[Modes.Length];
        float modeW = (Content - 2 * 4) / 3f;
        for (int i = 0; i < Modes.Length; i++)
        {
            var mode = (ThemeMode)i;
            _modeChips[i] = Add(new Pill { Text = Modes[i].Text, Font = chipFont },
                M + i * (modeW + 4), 326, modeW, 28, () => _app.SetMode(mode), Modes[i].Tip);
        }

        // --- Allgemein ---
        _autostartSwitch = Add(new ToggleSwitch { Text = L.T("autostart") }, M, 414, Content, 26,
            () => _app.ToggleAutostart(), L.T("tip.autostart", TrayApp.AppName));
        Add(new Pill { Text = L.T("more") }, M, 450, Content - 104, 30,
            () => _app.ShowSettings(), L.T("tip.more"));
        Add(new Pill { Text = L.T("quit"), Danger = true }, W - M - 100, 450, 100, 30,
            () => _app.Quit(), L.T("tip.quit", TrayApp.AppName));

        _anim.Tick += (_, _) => Animate();
        RefreshState();
    }

    T Add<T>(T control, float x, float y, float w, float h, Action onClick, string tip) where T : Control
    {
        control.Bounds = new Rectangle(S(x), S(y), S(w), S(h));
        control.Click += (_, _) =>
        {
            StopAutoHide();
            onClick();
        };
        _tips.SetToolTip(control, tip);
        Controls.Add(control);
        return control;
    }

    // ---------- Anzeigen / Verbergen ----------

    public void ShowWindow(bool autoHide)
    {
        bool wasHidden = !Visible || _fadingOut;
        _fadingOut = false;
        _autoHide = autoHide;
        _autoHideLeft = AutoHideSeconds;
        // Merken, wo die Maus beim Öffnen steht: Liegt das Fenster zufällig darunter (z. B. Klick im
        // ausgeklappten Infobereich), soll das den Countdown nicht anhalten.
        _cursorAtShow = Cursor.Position;
        _mouseMovedSinceShow = false;

        RefreshState();
        if (wasHidden)
        {
            // Unten rechts über der Taskleiste einblenden (ein verschobenes, offenes Fenster bleibt, wo es ist)
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(area.Right - Width - S(12), Math.Max(area.Top, area.Bottom - Height - S(12)));
        }
        if (!Visible)
        {
            Opacity = 0;
            Show();
        }
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
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

    /// <summary>Countdown pausiert nur, wenn die Maus seit dem Öffnen bewegt wurde und über dem Fenster ist.</summary>
    bool PausedByMouse()
    {
        var pos = Cursor.Position;
        if (!_mouseMovedSinceShow && pos != _cursorAtShow) _mouseMovedSinceShow = true;
        return _mouseMovedSinceShow && Bounds.Contains(pos);
    }

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
        _awakeButton.Text = L.T(awake.Active ? "awake.off" : "awake.on");
        for (int i = 0; i < Durations.Length; i++)
            _durationChips[i].Selected = awake.Active && _app.LastDuration == Durations[i].Duration;
        _displaySwitch.Checked = _app.Settings.KeepDisplayOn;
        _simulateSwitch.Checked = _app.Settings.SimulateActivity;

        bool dark = _app.Theme.IsDarkNow;
        bool themeFree = !_app.Theme.IsLocked; // gesperrt -> Design-Bereich ausgrauen
        _lightTile.Selected = !dark;
        _darkTile.Selected = dark;
        _lightTile.Enabled = _darkTile.Enabled = themeFree;
        for (int i = 0; i < _modeChips.Length; i++)
        {
            _modeChips[i].Selected = _app.Settings.ThemeMode == (ThemeMode)i;
            _modeChips[i].Enabled = themeFree;
        }
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
            if (!PausedByMouse()) _autoHideLeft -= dt;
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

        // Kopfzeile: App-Icon, Titel, Status
        Art.DrawAppIcon(g, new RectangleF(S(M), S(12), S(38), S(38)), awake: active);
        using (var textBrush = new SolidBrush(p.Text))
            g.DrawString(TrayApp.AppName, _titleFont, textBrush, S(62), S(8));

        using (var dot = new SolidBrush(active ? Color.FromArgb(16, 160, 80) : p.Sub))
            g.FillEllipse(dot, S(64), S(39), S(7), S(7));
        using (var subBrush = new SolidBrush(p.Sub))
            Line(g, AwakeStatus(), _smallFont, subBrush, S(74), S(34), _helpRect.Left - S(78));

        // Hilfe (?) und Schließen (✕ – blendet nur ins Tray aus)
        DrawHeaderButtonBackground(g, _helpRect, _helpHover);
        DrawHeaderButtonBackground(g, _closeRect, _closeHover);
        using (var helpBrush = new SolidBrush(p.Sub))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            g.DrawString("?", _helpFont, helpBrush, _helpRect, sf);
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
        int width = S(Content + 2);
        Line(g, L.T("sec.awake"), _sectionFont, secBrush, S(M - 1), S(80), width);
        Line(g, L.T("sec.design"), _sectionFont, secBrush, S(M - 1), S(262), width);
        Line(g, _themeStatus, _smallFont, secBrush, S(M - 1), S(360), width);
        Line(g, L.T("sec.general"), _sectionFont, secBrush, S(M - 1), S(394), width);

        // Fußzeile: Countdown bis zum Ausblenden – sonst Credits
        if (_autoHide)
        {
            var bar = new RectangleF(S(M), S(496), S(Content), Math.Max(1, S(2)));
            using (var track = new SolidBrush(p.Border)) g.FillRectangle(track, bar);
            using (var fill = new SolidBrush(p.Accent))
                g.FillRectangle(fill, bar.X, bar.Y, bar.Width * (float)Math.Max(0, _autoHideLeft / AutoHideSeconds), bar.Height);

            string hint = _mouseMovedSinceShow && Bounds.Contains(Cursor.Position)
                ? L.T("hide.paused")
                : L.T("hide.in", Math.Ceiling(Math.Max(0, _autoHideLeft)));
            Line(g, hint, _smallFont, secBrush, S(M - 1), S(502), width);
        }
        else
        {
            Line(g, L.Credits, _smallFont, secBrush, S(M - 1), S(500), width);
        }
    }

    /// <summary>Einzeilige Beschriftung; zu lange Texte (andere Sprachen) enden mit "…".</summary>
    static void Line(Graphics g, string text, Font font, Brush brush, float x, float y, float width)
    {
        using var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(text, font, brush, new RectangleF(x, y, width, font.GetHeight(g) * 1.4f), sf);
    }

    void DrawHeaderButtonBackground(Graphics g, Rectangle r, bool hover)
    {
        if (!hover) return;
        using var brush = new SolidBrush(Palette.Current.CardHover);
        using var path = Art.RoundRect(r, S(4));
        g.FillPath(brush, path);
    }

    string AwakeStatus() =>
        !_app.Awake.Active ? L.T("st.inactive")
        : _app.Awake.Until is { } until ? L.T("st.left", TrayApp.Remaining(until))
        : L.T("st.unlimited");

    // ---------- Maus & Tastatur ----------

    protected override void OnMouseMove(MouseEventArgs e)
    {
        bool close = _closeRect.Contains(e.Location), help = _helpRect.Contains(e.Location);
        if (close != _closeHover || help != _helpHover)
        {
            _closeHover = close;
            _helpHover = help;
            _tips.SetToolTip(this, help ? L.T("tip.help") : close ? L.T("tip.close") : "");
            Invalidate(_closeRect);
            Invalidate(_helpRect);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_closeHover || _helpHover)
        {
            _closeHover = _helpHover = false;
            Invalidate(_closeRect);
            Invalidate(_helpRect);
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
        else if (_helpRect.Contains(e.Location))
        {
            _app.ShowHelp();
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
        else if (e.KeyCode == Keys.F1) _app.ShowHelp();
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
            _tips.Dispose();
            _titleFont.Dispose();
            _sectionFont.Dispose();
            _smallFont.Dispose();
            _helpFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
