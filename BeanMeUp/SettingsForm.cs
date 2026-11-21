using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace BeanMeUp;

/// <summary>
/// Einstellungsfenster im Stil der Windows-11-Einstellungen: Abschnitte als helle Karten
/// auf grauem Grund (komplett im Code aufgebaut, kein Designer nötig).
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
sealed class SettingsForm : Form
{
    // Farben wie in den Windows-11-Einstellungen (helles Design; die Standard-Steuerelemente sind immer hell)
    static readonly Color BgColor = Color.FromArgb(243, 243, 243);
    static readonly Color CardColor = Color.FromArgb(251, 251, 251);
    static readonly Color BorderColor = Color.FromArgb(229, 229, 229);
    static readonly Color TextColor = Color.FromArgb(27, 27, 27);
    static readonly Color SubColor = Color.FromArgb(97, 97, 97);

    readonly float _scale;
    int S(float v) => (int)Math.Round(v * _scale);

    readonly AppSettings _s;
    readonly ComboBox _mode, _target, _language;
    readonly DateTimePicker _lightFrom, _darkFrom;
    readonly NumericUpDown _lat, _lon, _offset;
    readonly Label _sunInfo;
    readonly CheckBox _awakeOnStart, _display, _autostart, _updates;
    readonly Button _checkNow;
    readonly Font _headerFont, _smallFont;
    readonly Action<UpdateResult>? _onUpdateChecked;

    /// <summary>Grund, warum Hell/Dunkel gesperrt ist (z. B. Firmenrichtlinie) – null, wenn erlaubt.</summary>
    readonly string? _themeLock;

    /// <summary>Sprachcodes passend zu den Einträgen in _language ("auto" zuerst).</summary>
    readonly List<string> _languageCodes = ["auto"];

    /// <summary>true, wenn mit OK geschlossen wurde (die Einstellungen sind dann gespeichert).</summary>
    public bool Accepted { get; private set; }

    public SettingsForm(AppSettings settings, string? themeLock = null, Action<UpdateResult>? onUpdateChecked = null)
    {
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
            _scale = g.DpiX / 96f;

        _s = settings;
        _themeLock = themeLock;
        _onUpdateChecked = onUpdateChecked;
        Text = $"{TrayApp.AppName} – {L.T("set.title")}";
        Icon = Art.WindowIcon;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None; // Größen werden selbst mit S() skaliert
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = BgColor;
        ForeColor = TextColor;
        Font = UiFonts.Create(S(13));
        _headerFont = UiFonts.Create(S(14), semibold: true);
        _smallFont = UiFonts.Create(S(12));

        _mode = Combo(L.T("mode.manual"), L.T("set.modeSchedule"), L.T("set.modeSun"));
        _target = Combo(L.T("set.targetBoth"), L.T("set.targetApps"), L.T("set.targetSystem"));
        _language = Combo();
        _lightFrom = TimePicker();
        _darkFrom = TimePicker();
        _lat = Num(-90, 90, 4);
        _lon = Num(-180, 180, 4);
        _offset = Num(-180, 180, 0);
        _sunInfo = new Label
        {
            AutoSize = true,
            ForeColor = SubColor,
            BackColor = CardColor,
            Font = _smallFont,
            MaximumSize = new Size(CardInner, 0),
            Margin = new Padding(0, S(6), 0, S(2)),
            UseMnemonic = false,
        };
        _awakeOnStart = Check(L.T("set.awakeOnStart"));
        _display = Check(L.T("set.display"));
        _autostart = Check(L.T("autostart"));
        _updates = Check(L.T("set.updates"));
        _checkNow = new Button
        {
            Text = L.T("set.checkNow"),
            AutoSize = true,
            MinimumSize = new Size(S(100), S(30)),
            Padding = new Padding(S(8), 0, S(8), 0),
            Margin = new Padding(S(16), S(2), 0, S(2)),
            Anchor = AnchorStyles.Left,
            BackColor = SystemColors.Control,
            UseVisualStyleBackColor = true,
        };

        var page = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(S(24), S(16), S(24), 0),
            Margin = new Padding(0),
        };

        // Abschnitt "Hell / Dunkel"
        page.Controls.Add(Header(L.T("set.secTheme")));
        var theme = Card();
        Row(theme, L.T("set.mode"), _mode);
        Row(theme, L.T("set.target"), _target);
        Row(theme, L.T("set.lightFrom"), _lightFrom);
        Row(theme, L.T("set.darkFrom"), _darkFrom);
        Row(theme, L.T("set.lat"), _lat);
        Row(theme, L.T("set.lon"), _lon);
        Row(theme, L.T("set.offset"), _offset);
        Full(theme, _sunInfo);
        page.Controls.Add(theme);

        // Abschnitt "Wach halten"
        page.Controls.Add(Header(L.T("sec.awake")));
        var awake = Card();
        Full(awake, _awakeOnStart);
        Full(awake, _display);
        page.Controls.Add(awake);

        // Abschnitt "System"
        page.Controls.Add(Header(L.T("set.secSystem")));
        var system = Card();
        Full(system, _autostart);
        var updateRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = CardColor,
            Margin = new Padding(0),
        };
        _updates.Anchor = AnchorStyles.Left;
        updateRow.Controls.AddRange([_updates, _checkNow]);
        Full(system, updateRow);
        Row(system, L.T("set.language"), _language);
        page.Controls.Add(system);

        // Fußleiste: Credits links, OK/Abbrechen rechts
        var ok = new Button { Text = L.T("set.ok") };
        var cancel = new Button { Text = L.T("set.cancel") };
        foreach (var b in new[] { ok, cancel })
        {
            b.AutoSize = true;
            b.MinimumSize = new Size(S(92), S(32));
            b.Padding = new Padding(S(8), 0, S(8), 0);
            b.BackColor = SystemColors.Control;
            b.UseVisualStyleBackColor = true;
        }
        ok.Margin = new Padding(0, 0, S(8), 0);
        cancel.Margin = new Padding(0);
        // Das Fenster ist nicht modal (das Hauptfenster bleibt bedienbar). DialogResult an den
        // Buttons würde es deshalb NICHT schließen – darum hier ausdrücklich schließen.
        ok.Click += (_, _) =>
        {
            Apply();
            Accepted = true;
            Close();
        };
        cancel.Click += (_, _) => Close();

        int footerWidth = CardWidth + S(48);
        var footer = new Panel { Size = new Size(footerWidth, S(64)), Margin = new Padding(0, S(20), 0, 0), BackColor = BgColor };
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = BgColor,
        };
        buttons.Controls.AddRange([ok, cancel]);
        footer.Controls.Add(buttons);
        footer.Layout += (_, _) =>
            buttons.Location = new Point(footer.Width - S(24) - buttons.Width, (footer.Height - buttons.Height) / 2 + S(4));
        footer.Paint += (_, e) =>
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var line = new Pen(BorderColor);
            e.Graphics.DrawLine(line, 0, 0, footer.Width, 0);
            int textWidth = footer.Width - S(24) - buttons.Width - S(12) - S(24);
            TextRenderer.DrawText(e.Graphics, L.Credits, _smallFont,
                new Rectangle(S(24), S(4), Math.Max(S(40), textWidth), footer.Height - S(4)), SubColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        };

        var root = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
        };
        root.Controls.Add(page);
        root.Controls.Add(footer);
        Controls.Add(root);
        AcceptButton = ok;      // Enter = OK
        CancelButton = cancel;  // Esc = Abbrechen
        ActiveControl = ok;     // keine blau markierte Auswahlliste beim Öffnen

        // Sprachauswahl: "Automatisch (Systemsprache)" + alle übersetzten Sprachen in ihrer eigenen Schreibweise
        _language.Items.Add(L.T("set.langAuto"));
        foreach (var (code, name) in L.Languages)
        {
            _languageCodes.Add(code);
            _language.Items.Add(name);
        }
        int langIndex = _languageCodes.IndexOf((_s.Language ?? "auto").Trim().ToLowerInvariant());
        _language.SelectedIndex = langIndex < 0 ? 0 : langIndex;

        _mode.SelectedIndex = (int)_s.ThemeMode;
        _target.SelectedIndex = (int)_s.ThemeTarget;
        _lightFrom.Value = DateTime.Today + AppSettings.ParseTime(_s.LightFrom, 7);
        _darkFrom.Value = DateTime.Today + AppSettings.ParseTime(_s.DarkFrom, 19);
        _lat.Value = Clamp(_s.Latitude, -90, 90);
        _lon.Value = Clamp(_s.Longitude, -180, 180);
        _offset.Value = Clamp(_s.SunOffsetMinutes, -180, 180);
        _awakeOnStart.Checked = _s.KeepAwakeOnStart;
        _display.Checked = _s.KeepDisplayOn;
        _autostart.Checked = Autostart.IsEnabled;
        _updates.Checked = _s.CheckForUpdates;
        _checkNow.Click += async (_, _) => await CheckNowAsync();

        _mode.SelectedIndexChanged += (_, _) => UpdateEnabled();
        _lat.ValueChanged += (_, _) => UpdateSunInfo();
        _lon.ValueChanged += (_, _) => UpdateSunInfo();
        _offset.ValueChanged += (_, _) => UpdateSunInfo();

        // Kleine Hilfe: Tooltips an allen Feldern
        var tips = new ToolTip { AutoPopDelay = 15000, InitialDelay = 500 };
        Components.Add(tips);
        tips.SetToolTip(_mode, L.T("set.tipMode"));
        tips.SetToolTip(_target, L.T("set.tipTarget"));
        tips.SetToolTip(_lightFrom, L.T("set.tipLight"));
        tips.SetToolTip(_darkFrom, L.T("set.tipDark"));
        tips.SetToolTip(_lat, L.T("set.tipLat"));
        tips.SetToolTip(_lon, L.T("set.tipLon"));
        tips.SetToolTip(_offset, L.T("set.tipOffset"));
        tips.SetToolTip(_awakeOnStart, L.T("set.tipAwakeOnStart", TrayApp.AppName));
        tips.SetToolTip(_display, L.T("tip.display"));
        tips.SetToolTip(_autostart, L.T("tip.autostart", TrayApp.AppName));
        tips.SetToolTip(_language, L.T("set.tipLanguage"));
        tips.SetToolTip(_updates, L.T("set.tipUpdates"));
        tips.SetToolTip(_checkNow, L.T("set.tipUpdates"));

        UpdateEnabled();
        UpdateSunInfo();
    }

    readonly System.ComponentModel.Container Components = new();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Components.Dispose();
            _headerFont.Dispose();
            _smallFont.Dispose();
        }
        base.Dispose(disposing);
    }

    // ---------- Aufbau ----------

    int CardWidth => S(480);
    int CardPad => S(18);
    int CardInner => CardWidth - 2 * CardPad;
    int LabelWidth => S(190);

    /// <summary>Helle Karte mit abgerundetem Rahmen; zwei Spalten (Beschriftung | Steuerelement).</summary>
    TableLayoutPanel Card()
    {
        var card = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(CardWidth, 0),
            MaximumSize = new Size(CardWidth, 0),
            Padding = new Padding(CardPad, S(10), CardPad, S(10)),
            Margin = new Padding(0, 0, 0, S(8)),
            BackColor = BgColor,
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelWidth));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, card.Width - 1.5f, card.Height - 1.5f);
            using var path = Art.RoundRect(r, S(7));
            using var fill = new SolidBrush(CardColor);
            using var pen = new Pen(BorderColor);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        };
        return card;
    }

    void Row(TableLayoutPanel card, string label, Control control)
    {
        int row = card.RowCount++;
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            MaximumSize = new Size(LabelWidth - S(12), 0),
            Anchor = AnchorStyles.Left,
            BackColor = CardColor,
            UseMnemonic = false,
            Margin = new Padding(0, S(7), S(12), S(7)),
        }, 0, row);
        control.Anchor = AnchorStyles.Left;
        control.Margin = new Padding(0, S(5), 0, S(5));
        card.Controls.Add(control, 1, row);
    }

    static void Full(TableLayoutPanel card, Control control)
    {
        int row = card.RowCount++;
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.Controls.Add(control, 0, row);
        card.SetColumnSpan(control, 2);
    }

    Label Header(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = _headerFont,
        UseMnemonic = false,
        Margin = new Padding(S(2), S(12), 0, S(8)),
    };

    ComboBox Combo(params string[] items)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = S(230) };
        c.Items.AddRange(items);
        return c;
    }

    DateTimePicker TimePicker() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "HH:mm",
        ShowUpDown = true,
        Width = S(96),
    };

    NumericUpDown Num(int min, int max, int decimals) => new()
    {
        Minimum = min,
        Maximum = max,
        DecimalPlaces = decimals,
        Increment = decimals > 0 ? 0.1m : 5,
        Width = S(96),
        TextAlign = HorizontalAlignment.Right,
    };

    CheckBox Check(string text) => new()
    {
        Text = text,
        AutoSize = true,
        BackColor = CardColor,
        UseMnemonic = false,
        Margin = new Padding(0, S(6), 0, S(6)),
        Padding = new Padding(S(2), 0, 0, 0),
    };

    static decimal Clamp(double value, int min, int max) => (decimal)Math.Max(min, Math.Min(max, value));

    // ---------- Verhalten ----------

    void UpdateEnabled()
    {
        bool free = _themeLock == null;
        bool time = free && _mode.SelectedIndex == (int)ThemeMode.Zeitplan;
        bool sun = free && _mode.SelectedIndex == (int)ThemeMode.Sonnenstand;
        _mode.Enabled = _target.Enabled = free;
        _lightFrom.Enabled = _darkFrom.Enabled = time;
        _lat.Enabled = _lon.Enabled = _offset.Enabled = sun;
        _sunInfo.Enabled = sun || !free; // Sperrhinweis bleibt gut lesbar
    }

    void UpdateSunInfo()
    {
        if (_themeLock != null)
        {
            _sunInfo.Text = $"{L.T("set.locked")}\n({_themeLock})";
            return;
        }
        var (rise, set) = SunCalc.Get(DateTime.Today, (double)_lat.Value, (double)_lon.Value);
        int offset = (int)_offset.Value;
        _sunInfo.Text = rise is { } r && set is { } st
            ? L.T("set.sunToday", r.AddMinutes(offset).ToString("t"), st.AddMinutes(offset).ToString("t"))
            : L.T("set.noSun");
    }

    /// <summary>"Jetzt prüfen": sofort bei GitHub nachfragen und das Ergebnis anzeigen.</summary>
    async System.Threading.Tasks.Task CheckNowAsync()
    {
        _checkNow.Enabled = false;
        var result = await UpdateChecker.CheckAsync();
        if (IsDisposed) return;
        _checkNow.Enabled = true;

        _onUpdateChecked?.Invoke(result);
        if (result.Error != null)
        {
            MessageBox.Show(this, L.T("upd.failed", result.Error), TrayApp.AppName, MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        else if (result.IsNewer && result.Latest != null)
        {
            if (MessageBox.Show(this, L.T("upd.newer", result.Latest.ToString(3)), TrayApp.AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.None) == DialogResult.Yes)
                UpdateChecker.OpenPage(result.Url);
        }
        else
        {
            MessageBox.Show(this, L.T("upd.current", UpdateChecker.Current.ToString(3)), TrayApp.AppName, MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }

    void Apply()
    {
        _s.ThemeMode = (ThemeMode)_mode.SelectedIndex;
        _s.ThemeTarget = (ThemeTarget)_target.SelectedIndex;
        _s.LightFrom = _lightFrom.Value.ToString("HH:mm");
        _s.DarkFrom = _darkFrom.Value.ToString("HH:mm");
        _s.Latitude = (double)_lat.Value;
        _s.Longitude = (double)_lon.Value;
        _s.SunOffsetMinutes = (int)_offset.Value;
        _s.KeepAwakeOnStart = _awakeOnStart.Checked;
        _s.KeepDisplayOn = _display.Checked;
        _s.CheckForUpdates = _updates.Checked;

        string newLanguage = _languageCodes[Math.Max(0, _language.SelectedIndex)];
        bool languageChanged = !newLanguage.Equals(_s.Language, StringComparison.OrdinalIgnoreCase);
        _s.Language = newLanguage;
        _s.Save();

        try { Autostart.Set(_autostart.Checked); }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, $"{TrayApp.AppName} – {L.T("err.autostart")}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        if (languageChanged)
            MessageBox.Show(this, L.T("set.langRestart", TrayApp.AppName), TrayApp.AppName, MessageBoxButtons.OK, MessageBoxIcon.None);
    }
}
