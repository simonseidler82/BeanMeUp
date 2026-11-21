namespace BeanMeUp;

/// <summary>Kleines Einstellungsfenster (komplett im Code aufgebaut, kein Designer nötig).</summary>
[System.ComponentModel.DesignerCategory("Code")]
sealed class SettingsForm : Form
{
    readonly AppSettings _s;
    readonly ComboBox _mode = Combo(L.T("mode.manual"), L.T("set.modeSchedule"), L.T("set.modeSun"));
    readonly ComboBox _target = Combo(L.T("set.targetBoth"), L.T("set.targetApps"), L.T("set.targetSystem"));
    readonly ComboBox _language = Combo();
    readonly DateTimePicker _lightFrom = TimePicker();
    readonly DateTimePicker _darkFrom = TimePicker();
    readonly NumericUpDown _lat = Num(-90, 90, 4);
    readonly NumericUpDown _lon = Num(-180, 180, 4);
    readonly NumericUpDown _offset = Num(-180, 180, 0);
    readonly Label _sunInfo = new() { AutoSize = true, Margin = new Padding(3, 3, 3, 6) };
    readonly CheckBox _awakeOnStart = Check(L.T("set.awakeOnStart"));
    readonly CheckBox _display = Check(L.T("set.display"));
    readonly CheckBox _autostart = Check(L.T("autostart"));
    readonly CheckBox _updates = Check(L.T("set.updates"));
    readonly Button _checkNow = new() { Text = L.T("set.checkNow"), AutoSize = true, Margin = new Padding(12, 0, 3, 0) };
    readonly Action<UpdateResult>? _onUpdateChecked;

    /// <summary>Grund, warum Hell/Dunkel gesperrt ist (z. B. Firmenrichtlinie) – null, wenn erlaubt.</summary>
    readonly string? _themeLock;

    /// <summary>Sprachcodes passend zu den Einträgen in _language ("auto" zuerst).</summary>
    readonly List<string> _languageCodes = ["auto"];

    public SettingsForm(AppSettings settings, string? themeLock = null, Action<UpdateResult>? onUpdateChecked = null)
    {
        _s = settings;
        _themeLock = themeLock;
        _onUpdateChecked = onUpdateChecked;
        Text = $"{TrayApp.AppName} – {L.T("set.title")}";
        Icon = Art.WindowIcon;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        int row = 0;
        void Row(string label, Control c)
        {
            grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 12, 3) }, 0, row);
            grid.Controls.Add(c, 1, row++);
        }
        void Full(Control c)
        {
            grid.Controls.Add(c, 0, row++);
            grid.SetColumnSpan(c, 2);
        }

        Full(Header(L.T("set.secTheme")));
        Row(L.T("set.mode"), _mode);
        Row(L.T("set.target"), _target);
        Row(L.T("set.lightFrom"), _lightFrom);
        Row(L.T("set.darkFrom"), _darkFrom);
        Row(L.T("set.lat"), _lat);
        Row(L.T("set.lon"), _lon);
        Row(L.T("set.offset"), _offset);
        Full(_sunInfo);
        Full(Header(L.T("sec.awake")));
        Full(_awakeOnStart);
        Full(_display);
        Full(Header(L.T("set.secSystem")));
        Full(_autostart);
        var updateRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        updateRow.Controls.AddRange([_updates, _checkNow]);
        Full(updateRow);
        Row(L.T("set.language"), _language);
        Full(new Label
        {
            Text = L.Credits,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 14, 3, 0),
        });

        var ok = new Button { Text = L.T("set.ok"), DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = L.T("set.cancel"), DialogResult = DialogResult.Cancel, AutoSize = true };
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 12, 0, 0),
        };
        buttons.Controls.AddRange([cancel, ok]);
        Full(buttons);

        Controls.Add(grid);
        AcceptButton = ok;
        CancelButton = cancel;

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
        FormClosing += (_, _) => { if (DialogResult == DialogResult.OK) Apply(); };

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
        if (disposing) Components.Dispose();
        base.Dispose(disposing);
    }

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
        _checkNow.Enabled = true;
        if (IsDisposed) return;

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
            MessageBox.Show(ex.Message, $"{TrayApp.AppName} – {L.T("err.autostart")}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        if (languageChanged)
            MessageBox.Show(L.T("set.langRestart", TrayApp.AppName), TrayApp.AppName, MessageBoxButtons.OK, MessageBoxIcon.None);
    }

    static ComboBox Combo(params string[] items)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
        c.Items.AddRange(items);
        return c;
    }

    static DateTimePicker TimePicker() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "HH:mm",
        ShowUpDown = true,
        Width = 90,
    };

    static NumericUpDown Num(int min, int max, int decimals) => new()
    {
        Minimum = min,
        Maximum = max,
        DecimalPlaces = decimals,
        Increment = decimals > 0 ? 0.1m : 5,
        Width = 90,
    };

    static decimal Clamp(double value, int min, int max) => (decimal)Math.Max(min, Math.Min(max, value));

    static CheckBox Check(string text) => new() { Text = text, AutoSize = true };

    static Label Header(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font(DefaultFont, FontStyle.Bold),
        Margin = new Padding(0, 10, 0, 4),
    };
}
