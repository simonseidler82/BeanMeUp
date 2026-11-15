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

    public bool IsDarkNow => ThemeRegistry.IsDark(s.ThemeTarget);

    public void Tick()
    {
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
            ThemeRegistry.Apply(desired, s.ThemeTarget);
        _lastApplied = desired;
    }

    /// <summary>Benutzer wählt Hell/Dunkel direkt im Menü.</summary>
    public void Choose(bool dark)
    {
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
