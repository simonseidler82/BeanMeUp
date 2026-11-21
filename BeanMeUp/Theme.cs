using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BeanMeUp;

/// <summary>Liest/setzt das Windows-Design (Hell/Dunkel) für den aktuellen Benutzer.</summary>
static class ThemeRegistry
{
    const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam,
        uint flags, uint timeout, out UIntPtr result);

    const uint WM_SETTINGCHANGE = 0x001A;
    const uint SMTO_ABORTIFHUNG = 0x0002;
    static readonly IntPtr HWND_BROADCAST = new(0xFFFF);

    // Windows-Richtlinien (Gruppenrichtlinie/Intune), mit denen Firmen das Ändern des Designs verbieten.
    // Werden unter HKCU und HKLM geprüft.
    static readonly (string Path, string Name, string Text)[] Policies =
    [
        (@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoThemesTab", "Richtlinie „Ändern des Designs verhindern“"),
        (@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispAppearancePage", "Richtlinie „Ändern von Farbe und Darstellung verhindern“"),
        (@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoColorChoice", "Richtlinie „Ändern des Farbschemas verhindern“"),
    ];

    /// <summary>
    /// Prüft, ob das Ändern von Hell/Dunkel gesperrt ist (Richtlinie der Organisation oder fehlende
    /// Schreibrechte). Gibt den Grund zurück, sonst null.
    /// </summary>
    public static string? LockReason()
    {
        // Für Tests ohne echte Richtlinie: Umgebungsvariable BEANMEUP_TEST_THEME_LOCK=1
        if (Environment.GetEnvironmentVariable("BEANMEUP_TEST_THEME_LOCK") == "1")
            return "Testmodus (BEANMEUP_TEST_THEME_LOCK)";

        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            foreach (var (path, name, text) in Policies)
            {
                try
                {
                    using var key = hive.OpenSubKey(path);
                    if (key?.GetValue(name) is int v && v != 0) return text;
                }
                catch { /* nicht lesbar -> ignorieren */ }
            }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return "Keine Berechtigung zum Ändern des Designs";
        }
        return null;
    }

    public static bool IsDark(ThemeTarget target)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        string name = target == ThemeTarget.NurSystem ? "SystemUsesLightTheme" : "AppsUseLightTheme";
        return key?.GetValue(name) is int v && v == 0;
    }

    public static void Apply(bool dark, ThemeTarget target)
    {
        int light = dark ? 0 : 1;
        using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
        {
            if (target != ThemeTarget.NurSystem)
                key.SetValue("AppsUseLightTheme", light, RegistryValueKind.DWord);
            if (target != ThemeTarget.NurApps)
                key.SetValue("SystemUsesLightTheme", light, RegistryValueKind.DWord);
        }
        // Explorer, Taskleiste und Apps über die Änderung informieren
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, "ImmersiveColorSet",
            SMTO_ABORTIFHUNG, 200, out _);
    }
}

/// <summary>Berechnet aus Zeitplan bzw. Sonnenstand, wann hell und wann dunkel sein soll.</summary>
static class Schedule
{
    static IEnumerable<(DateTime At, bool Dark)> EventsOn(AppSettings s, DateTime day)
    {
        if (s.ThemeMode == ThemeMode.Sonnenstand)
        {
            var (rise, set) = SunCalc.Get(day, s.Latitude, s.Longitude);
            if (rise is { } r && set is { } st)
            {
                yield return (r.AddMinutes(s.SunOffsetMinutes), false);
                yield return (st.AddMinutes(s.SunOffsetMinutes), true);
                yield break;
            }
            // Polartag/-nacht: auf festen Zeitplan zurückfallen
        }
        yield return (day.Date + AppSettings.ParseTime(s.LightFrom, 7), false);
        yield return (day.Date + AppSettings.ParseTime(s.DarkFrom, 19), true);
    }

    static IEnumerable<(DateTime At, bool Dark)> Events(AppSettings s, DateTime now) =>
        new[] { -1, 0, 1, 2 }.SelectMany(d => EventsOn(s, now.Date.AddDays(d))).OrderBy(e => e.At);

    public static bool IsDark(AppSettings s, DateTime now)
    {
        var past = Events(s, now).Where(e => e.At <= now).ToList();
        return past.Count > 0 && past[past.Count - 1].Dark;
    }

    public static (DateTime At, bool Dark)? Next(AppSettings s, DateTime now)
    {
        foreach (var e in Events(s, now))
            if (e.At > now) return e;
        return null;
    }
}

/// <summary>Entscheidet, welches Design gelten soll, und respektiert die aktuelle Vorliebe des Benutzers.</summary>
sealed class ThemeController(AppSettings s)
{
    bool? _lastApplied;
    string? _applyRefused;

    public bool IsDarkNow => ThemeRegistry.IsDark(s.ThemeTarget);

    /// <summary>Grund, warum Hell/Dunkel nicht geändert werden darf – null, wenn alles erlaubt ist.</summary>
    public string? LockReason { get; private set; }
    public bool IsLocked => LockReason != null;

    public void Tick()
    {
        // Gesperrt (z. B. Firmenrichtlinie)? Dann nur noch anzeigen, nichts ändern.
        LockReason = _applyRefused ?? ThemeRegistry.LockReason();
        if (IsLocked)
        {
            _lastApplied = null;
            return;
        }

        var now = DateTime.Now;
        bool current = ThemeRegistry.IsDark(s.ThemeTarget);

        // Design außerhalb der App geändert (z. B. Windows-Einstellungen)?
        // Gilt als aktuelle Vorliebe bis zum nächsten geplanten Wechsel.
        if (_lastApplied is { } last && current != last)
            SetPreference(current);

        if (s.OverrideUntil is { } until && now >= until)
        {
            s.OverrideDark = null;
            s.OverrideUntil = null;
            s.Save();
        }

        bool desired = Desired(now);
        if (desired != current)
        {
            try
            {
                ThemeRegistry.Apply(desired, s.ThemeTarget);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
            {
                // Windows verweigert das Schreiben -> wie eine Sperre behandeln (bis zum Neustart der App)
                _applyRefused = "Windows hat das Ändern des Designs verweigert";
                LockReason = _applyRefused;
                _lastApplied = null;
                return;
            }
        }
        _lastApplied = desired;
    }

    /// <summary>Benutzer wählt Hell/Dunkel direkt im Menü.</summary>
    public void Choose(bool dark)
    {
        if (IsLocked) return;
        SetPreference(dark);
        Tick();
    }

    /// <summary>Nach geänderten Einstellungen neu bewerten (Vorliebe verwerfen).</summary>
    public void Reset()
    {
        _lastApplied = null;
        s.OverrideDark = null;
        s.OverrideUntil = null;
        s.Save();
        Tick();
    }

    bool Desired(DateTime now) => s.ThemeMode switch
    {
        ThemeMode.Manuell => s.ManualDark,
        _ when s.OverrideDark is { } o => o,
        _ => Schedule.IsDark(s, now),
    };

    void SetPreference(bool dark)
    {
        if (s.ThemeMode == ThemeMode.Manuell)
        {
            s.ManualDark = dark;
        }
        else if (dark == Schedule.IsDark(s, DateTime.Now))
        {
            // Entspricht ohnehin dem Plan -> keine Ausnahme nötig
            s.OverrideDark = null;
            s.OverrideUntil = null;
        }
        else
        {
            s.OverrideDark = dark;
            s.OverrideUntil = Schedule.Next(s, DateTime.Now)?.At;
        }
        s.Save();
    }
}
