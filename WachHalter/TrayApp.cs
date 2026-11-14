using System.Reflection;
using Microsoft.Win32;

namespace WachHalter;

/// <summary>
/// Die App: Tray-Icon (Menü nur "Anzeigen / Verbergen") plus Hauptfenster mit allen Einstellungen.
/// </summary>
sealed class TrayApp : ApplicationContext
{
    internal readonly AppSettings Settings = AppSettings.Load();
    internal readonly KeepAwake Awake = new();
    internal readonly ThemeController Theme;
    /// <summary>Zuletzt gewählte Dauer (null = unbegrenzt).</summary>
    internal TimeSpan? LastDuration { get; private set; }

    readonly NotifyIcon _tray;
    readonly MainWindow _window;
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 30_000 };
    readonly Icon _iconOn = Art.CreateTrayIcon(active: true);
    readonly Icon _iconOff = Art.CreateTrayIcon(active: false);
    readonly SynchronizationContext _ui;
    readonly ToolStripMenuItem _miShow = new("Anzeigen");
    readonly ToolStripMenuItem _miHide = new("Verbergen");
    SettingsForm? _settingsForm;

    public TrayApp()
    {
        Theme = new ThemeController(Settings);
        if (Settings.KeepAwakeOnStart)
            Awake.Start(null, Settings.KeepDisplayOn);
        Theme.Tick();

        var menu = new ContextMenuStrip();
        _miShow.Font = new Font(_miShow.Font, FontStyle.Bold);
        menu.Items.AddRange([_miShow, _miHide]);
        // ContextMenuStrip hat den WinForms-SynchronizationContext installiert
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        _window = new MainWindow(this);
        _miShow.Click += (_, _) => _window.ShowWindow(autoHide: false);
        _miHide.Click += (_, _) => _window.HideWindow();
        menu.Opening += (_, _) =>
        {
            _miShow.Enabled = !_window.Visible;
            _miHide.Enabled = _window.Visible;
        };

        _tray = new NotifyIcon { ContextMenuStrip = menu, Icon = _iconOff, Text = "WachHalter", Visible = true };
        // Auch Linksklick öffnet das kleine Menü
        _tray.MouseUp += (_, e) => { if (e.Button == MouseButtons.Left) ShowTrayMenu(); };

        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        UpdateUi();

        _window.ShowWindow(autoHide: true);
    }

    void ShowTrayMenu() =>
        typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(_tray, null);

    // ---------- Aktionen (vom Hauptfenster aufgerufen) ----------

    internal void ToggleAwake()
    {
        if (Awake.Active) Awake.Stop();
        else Awake.Start(LastDuration, Settings.KeepDisplayOn);
        UpdateUi();
    }

    internal void StartAwake(TimeSpan? duration)
    {
        LastDuration = duration;
        Awake.Start(duration, Settings.KeepDisplayOn);
        UpdateUi();
    }

    internal void ToggleDisplay()
    {
        Settings.KeepDisplayOn = !Settings.KeepDisplayOn;
        Awake.SetKeepDisplayOn(Settings.KeepDisplayOn);
        Settings.Save();
        UpdateUi();
    }

    internal void ToggleSimulateActivity()
    {
        Settings.SimulateActivity = !Settings.SimulateActivity;
        Settings.Save();
        UpdateUi();
    }

    internal void ChooseTheme(bool dark)
    {
        try { Theme.Choose(dark); }
        catch (Exception ex) { ShowError("Design konnte nicht geändert werden", ex); }
        UpdateUi();
    }

    internal void SetMode(ThemeMode mode)
    {
        if (mode == ThemeMode.Manuell)
            Settings.ManualDark = Theme.IsDarkNow; // aktuelles Design beibehalten
        Settings.ThemeMode = mode;
        Theme.Reset();
        UpdateUi();
    }

    internal void ToggleAutostart()
    {
        try { Autostart.Set(!Autostart.IsEnabled); }
        catch (Exception ex) { ShowError("Autostart konnte nicht geändert werden", ex); }
        UpdateUi();
    }

    internal void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }
        var form = new SettingsForm(Settings);
        form.FormClosed += (_, _) =>
        {
            if (form.DialogResult == DialogResult.OK)
            {
                Awake.SetKeepDisplayOn(Settings.KeepDisplayOn);
                Theme.Reset();
                UpdateUi();
            }
            _settingsForm = null;
        };
        _settingsForm = form;
        form.Show();
    }

    internal void Quit() => ExitThread();

    internal string ThemeStatus()
    {
        if (Settings.ThemeMode == ThemeMode.Manuell)
            return "Automatik aus – Design bleibt, wie gewählt";
        if (Settings.OverrideDark is { } o && Settings.OverrideUntil is { } until)
            return $"Deine Wahl ({(o ? "Dunkel" : "Hell")}) gilt bis {until:HH:mm} Uhr";
        return Schedule.Next(Settings, DateTime.Now) is { } next
            ? $"Nächster Wechsel um {next.At:HH:mm} Uhr → {(next.Dark ? "Dunkel" : "Hell")}"
            : "Automatik aktiv";
    }

    internal static string Remaining(DateTime until)
    {
        var t = until - DateTime.Now;
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00} h"
            : $"{Math.Max(1, (int)Math.Ceiling(t.TotalMinutes))} min";
    }

    // ---------- Hintergrund ----------

    void OnTick()
    {
        Awake.Tick();
        // Nur während "Wach halten" aktiv ist; greift erst nach 25 s ohne echte Eingabe
        if (Awake.Active && Settings.SimulateActivity)
            ActivitySimulator.NudgeIfIdle(TimeSpan.FromSeconds(25));
        try { Theme.Tick(); } catch { /* beim nächsten Tick erneut versuchen */ }
        UpdateUi();
    }

    void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            _ui.Post(_ => OnTick(), null);
    }

    void UpdateUi()
    {
        bool dark = Theme.IsDarkNow;
        _tray.Icon = Awake.Active ? _iconOn : _iconOff;

        string awake = !Awake.Active ? "aus"
            : Awake.Until is { } until ? $"noch {Remaining(until)}"
            : "unbegrenzt";
        string tip = $"WachHalter\nWach halten: {awake}\nDesign: {(dark ? "Dunkel" : "Hell")}";
        _tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip; // .NET Framework: max. 63 Zeichen

        _window.RefreshState();
    }

    static void ShowError(string title, Exception ex) =>
        MessageBox.Show(ex.Message, $"WachHalter – {title}", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        Awake.Stop();
        _settingsForm?.Close();
        _window.AllowClose();
        _window.Close();
        _tray.Visible = false;
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
