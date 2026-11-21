using Microsoft.Win32;

namespace BeanMeUp;

/// <summary>
/// Die App: Tray-Icon (ein Klick öffnet das Hauptfenster) plus Hauptfenster mit allen Einstellungen.
/// </summary>
sealed class TrayApp : ApplicationContext
{
    /// <summary>Anzeigename der App – überall in der Oberfläche verwendet.</summary>
    internal const string AppName = "Bean me up";

    internal readonly AppSettings Settings = AppSettings.Load();
    internal readonly KeepAwake Awake = new();
    internal readonly ThemeController Theme;
    /// <summary>Zuletzt gewählte Dauer (null = unbegrenzt).</summary>
    internal TimeSpan? LastDuration { get; private set; }

    /// <summary>Neuere Version auf GitHub (null = keine bekannt) und ihre Download-Seite.</summary>
    internal string? UpdateVersion { get; private set; }
    internal string UpdateUrl { get; private set; } = UpdateChecker.ReleasesPage;
    bool _updateCheckRunning;
    readonly System.Windows.Forms.Timer _firstUpdateCheck = new() { Interval = 15_000 };

    readonly NotifyIcon _tray;
    readonly MainWindow _window;
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 30_000 };
    readonly Icon _iconOn = Art.CreateTrayIcon(active: true);
    readonly Icon _iconOff = Art.CreateTrayIcon(active: false);
    readonly SynchronizationContext _ui;
    SettingsForm? _settingsForm;
    HelpForm? _helpForm;

    public TrayApp()
    {
        Theme = new ThemeController(Settings);
        Autostart.MigrateLegacy();
        if (Settings.KeepAwakeOnStart)
            Awake.Start(null, Settings.KeepDisplayOn);
        // Ein Fehler beim Design (z. B. von der Firma gesperrt) darf den Start nie verhindern
        try { Theme.Tick(); } catch { /* wird beim nächsten Tick erneut versucht */ }

        _window = new MainWindow(this);
        // Das Fenster hat den WinForms-SynchronizationContext installiert
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        // Kein Menü: Links- oder Rechtsklick auf das Tray-Symbol öffnet direkt das Fenster.
        // Es blendet sich wie beim Start nach 12 s wieder aus. Beenden gibt es im Fenster selbst.
        _tray = new NotifyIcon { Icon = _iconOff, Text = AppName, Visible = true };
        _tray.MouseUp += (_, _) => _window.ShowWindow(autoHide: true);
        _tray.BalloonTipClicked += (_, _) => UpdateChecker.OpenPage(UpdateUrl);

        // Bereits bekannte neuere Version (aus einer früheren Prüfung) gleich anzeigen
        if (Settings.CheckForUpdates && Version.TryParse(Settings.AvailableVersion, out var known) && UpdateChecker.IsNewer(known))
            UpdateVersion = Settings.AvailableVersion;

        // Erste Update-Prüfung 15 s nach dem Start, danach über den 30-s-Takt höchstens 1× täglich
        _firstUpdateCheck.Tick += (_, _) => { _firstUpdateCheck.Stop(); MaybeCheckForUpdates(); };
        _firstUpdateCheck.Start();

        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        UpdateUi();

        _window.ShowWindow(autoHide: true);
    }

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
        catch (Exception ex) { ShowError(L.T("err.theme"), ex); }
        UpdateUi();
    }

    internal void SetMode(ThemeMode mode)
    {
        if (Theme.IsLocked) return;
        if (mode == ThemeMode.Manuell)
            Settings.ManualDark = Theme.IsDarkNow; // aktuelles Design beibehalten
        Settings.ThemeMode = mode;
        try { Theme.Reset(); } catch { /* beim nächsten Tick erneut versuchen */ }
        UpdateUi();
    }

    internal void ToggleAutostart()
    {
        try { Autostart.Set(!Autostart.IsEnabled); }
        catch (Exception ex) { ShowError(L.T("err.autostart"), ex); }
        UpdateUi();
    }

    internal void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }
        var form = new SettingsForm(Settings, Theme.LockReason, result => ApplyUpdateResult(result, fromUser: true));
        form.FormClosed += (_, _) =>
        {
            if (form.DialogResult == DialogResult.OK)
            {
                Awake.SetKeepDisplayOn(Settings.KeepDisplayOn);
                try { Theme.Reset(); } catch { /* beim nächsten Tick erneut versuchen */ }
                UpdateUi();
            }
            _settingsForm = null;
        };
        _settingsForm = form;
        form.Show();
    }

    internal void ShowHelp()
    {
        if (_helpForm is { IsDisposed: false })
        {
            _helpForm.Activate();
            return;
        }
        var form = new HelpForm();
        form.FormClosed += (_, _) => _helpForm = null;
        _helpForm = form;
        form.Show();
    }

    internal void Quit() => ExitThread();

    // ---------- Update-Prüfung (1× täglich, abschaltbar) ----------

    void MaybeCheckForUpdates()
    {
        if (!Settings.CheckForUpdates || _updateCheckRunning) return;
        if (Settings.LastUpdateCheck is { } last && DateTime.Now - last < TimeSpan.FromHours(24)) return;

        _updateCheckRunning = true;
        UpdateChecker.CheckAsync().ContinueWith(t => _ui.Post(_ =>
        {
            _updateCheckRunning = false;
            ApplyUpdateResult(t.Result, fromUser: false);
        }, null));
    }

    /// <summary>Ergebnis einer Prüfung übernehmen (automatisch oder über "Jetzt prüfen").</summary>
    internal void ApplyUpdateResult(UpdateResult result, bool fromUser)
    {
        if (result.Error != null)
        {
            // Kein Netz o. Ä.: in etwa einer Stunde erneut versuchen
            Settings.LastUpdateCheck = DateTime.Now - TimeSpan.FromHours(23);
            Settings.Save();
            return;
        }

        Settings.LastUpdateCheck = DateTime.Now;
        if (result.IsNewer && result.Latest != null)
        {
            string version = result.Latest.ToString(3);
            UpdateVersion = version;
            UpdateUrl = result.Url;
            Settings.AvailableVersion = version;
            if (!fromUser && Settings.NotifiedVersion != version)
            {
                // Pro neuer Version nur einmal benachrichtigen
                Settings.NotifiedVersion = version;
                _tray.ShowBalloonTip(10_000, L.T("upd.balloonTitle", AppName, version), L.T("upd.balloonText"), ToolTipIcon.Info);
            }
        }
        else
        {
            UpdateVersion = null;
            Settings.AvailableVersion = "";
        }
        Settings.Save();
        UpdateUi();
    }

    internal string ThemeStatus()
    {
        if (Theme.IsLocked)
            return L.T("ts.locked");
        if (Settings.ThemeMode == ThemeMode.Manuell)
            return L.T("ts.manual");
        if (Settings.OverrideDark is { } o && Settings.OverrideUntil is { } until)
            return L.T("ts.override", L.T(o ? "dark" : "light"), until.ToString("t"));
        return Schedule.Next(Settings, DateTime.Now) is { } next
            ? L.T("ts.next", next.At.ToString("t"), L.T(next.Dark ? "dark" : "light"))
            : L.T("ts.auto");
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
        MaybeCheckForUpdates();
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

        string awake = !Awake.Active ? L.T("tray.off")
            : Awake.Until is { } until ? L.T("tray.left", Remaining(until))
            : L.T("tray.unlimited");
        string tip = $"{AppName}\n{L.T("tray.awake", awake)}\n{L.T("tray.design", L.T(dark ? "dark" : "light"))}";
        _tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip; // .NET Framework: max. 63 Zeichen

        _window.RefreshState();
    }

    static void ShowError(string title, Exception ex) =>
        MessageBox.Show(ex.Message, $"{AppName} – {title}", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _firstUpdateCheck.Stop();
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        Awake.Stop();
        _settingsForm?.Close();
        _helpForm?.Close();
        _window.AllowClose();
        _window.Close();
        _tray.Visible = false;
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
