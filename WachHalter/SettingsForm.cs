namespace WachHalter;

/// <summary>Kleines Einstellungsfenster (komplett im Code aufgebaut, kein Designer nötig).</summary>
[System.ComponentModel.DesignerCategory("Code")]
sealed class SettingsForm : Form
{
    readonly AppSettings _s;
    readonly ComboBox _mode = Combo("Manuell", "Automatisch: Zeitplan", "Automatisch: Sonnenstand");
    readonly ComboBox _target = Combo("Apps und System", "Nur Apps", "Nur System (Taskleiste, Start)");
    readonly DateTimePicker _lightFrom = TimePicker();
    readonly DateTimePicker _darkFrom = TimePicker();
    readonly NumericUpDown _lat = Num(-90, 90, 4);
    readonly NumericUpDown _lon = Num(-180, 180, 4);
    readonly NumericUpDown _offset = Num(-180, 180, 0);
    readonly Label _sunInfo = new() { AutoSize = true, Margin = new Padding(3, 3, 3, 6) };
    readonly CheckBox _awakeOnStart = Check("Beim Start der App sofort wach halten");
    readonly CheckBox _display = Check("Bildschirm anlassen");
    readonly CheckBox _autostart = Check("Mit Windows starten");

    public SettingsForm(AppSettings settings)
    {
        _s = settings;
        Text = "WachHalter – Einstellungen";
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

        Full(Header("Hell / Dunkel"));
        Row("Modus:", _mode);
        Row("Gilt für:", _target);
        Row("Hell ab:", _lightFrom);
        Row("Dunkel ab:", _darkFrom);
        Row("Breitengrad:", _lat);
        Row("Längengrad:", _lon);
        Row("Versatz (Min.):", _offset);
        Full(_sunInfo);
        Full(Header("Wach halten"));
        Full(_awakeOnStart);
        Full(_display);
        Full(Header("System"));
        Full(_autostart);

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true };
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

        _mode.SelectedIndexChanged += (_, _) => UpdateEnabled();
        _lat.ValueChanged += (_, _) => UpdateSunInfo();
        _lon.ValueChanged += (_, _) => UpdateSunInfo();
        _offset.ValueChanged += (_, _) => UpdateSunInfo();
        FormClosing += (_, _) => { if (DialogResult == DialogResult.OK) Apply(); };

        UpdateEnabled();
        UpdateSunInfo();
    }

    void UpdateEnabled()
    {
        bool time = _mode.SelectedIndex == (int)ThemeMode.Zeitplan;
        bool sun = _mode.SelectedIndex == (int)ThemeMode.Sonnenstand;
        _lightFrom.Enabled = _darkFrom.Enabled = time;
        _lat.Enabled = _lon.Enabled = _offset.Enabled = _sunInfo.Enabled = sun;
    }

    void UpdateSunInfo()
    {
        var (rise, set) = SunCalc.Get(DateTime.Today, (double)_lat.Value, (double)_lon.Value);
        int offset = (int)_offset.Value;
        _sunInfo.Text = rise is { } r && set is { } st
            ? $"Heute: Hell ab {r.AddMinutes(offset):HH:mm}, Dunkel ab {st.AddMinutes(offset):HH:mm}"
            : "Heute kein Sonnenauf-/untergang – es gilt der Zeitplan.";
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
        _s.Save();
        try { Autostart.Set(_autostart.Checked); }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "WachHalter – Autostart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    static ComboBox Combo(params string[] items)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
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
