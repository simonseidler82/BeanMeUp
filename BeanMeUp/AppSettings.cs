using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace BeanMeUp;

public enum ThemeMode { Manuell, Zeitplan, Sonnenstand }
public enum ThemeTarget { AppsUndSystem, NurApps, NurSystem }

/// <summary>
/// Einstellungen. Werden portabel neben der EXE gespeichert
/// (Fallback: %AppData%\BeanMeUp, falls der Ordner schreibgeschützt ist).
/// Einstellungen der Vorgängerversion "WachHalter" werden beim ersten Start übernommen.
/// </summary>
public sealed class AppSettings
{
    public bool KeepAwakeOnStart { get; set; }
    public bool KeepDisplayOn { get; set; } = true;
    /// <summary>Wie ein Maus-Jiggler: verhindert zusätzlich Sperre und "Abwesend"-Status. Standard: aus.</summary>
    public bool SimulateActivity { get; set; }

    public ThemeMode ThemeMode { get; set; } = ThemeMode.Zeitplan;
    public ThemeTarget ThemeTarget { get; set; } = ThemeTarget.AppsUndSystem;
    public string LightFrom { get; set; } = "07:00";
    public string DarkFrom { get; set; } = "19:00";
    public double Latitude { get; set; } = 51.0;
    public double Longitude { get; set; } = 10.0;
    public int SunOffsetMinutes { get; set; }

    /// <summary>Gewähltes Design im Modus "Manuell".</summary>
    public bool ManualDark { get; set; }

    /// <summary>Aktuelle Vorliebe im Automatik-Modus – gilt bis zum nächsten geplanten Wechsel.</summary>
    public bool? OverrideDark { get; set; }
    public DateTime? OverrideUntil { get; set; }

    const string FileName = "BeanMeUp.ini";
    static readonly string PortablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
    static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeanMeUp", FileName);

    // Vorgängerversion "WachHalter": wird nur gelesen, falls noch keine BeanMeUp.ini existiert
    static readonly string LegacyPortablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WachHalter.ini");
    static readonly string LegacyAppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WachHalter", "WachHalter.ini");

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly PropertyInfo[] Props = typeof(AppSettings)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite)
        .ToArray();

    // Einfaches "Name=Wert"-Format – kommt ohne JSON-Bibliothek aus.
    public static AppSettings Load()
    {
        foreach (var path in new[] { PortablePath, AppDataPath, LegacyPortablePath, LegacyAppDataPath })
        {
            if (!File.Exists(path)) continue;
            var s = new AppSettings();
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string name = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    var prop = Props.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (prop == null) continue;
                    try { prop.SetValue(s, Parse(value, prop.PropertyType)); }
                    catch { /* ungültiger Wert -> Standard behalten */ }
                }
                return s;
            }
            catch { /* unlesbare Datei -> nächste Quelle bzw. Standardwerte */ }
        }
        return new AppSettings();
    }

    static object? Parse(string value, Type type)
    {
        var t = Nullable.GetUnderlyingType(type);
        if (t != null && value.Length == 0) return null;
        t ??= type;
        if (t.IsEnum) return Enum.Parse(t, value, ignoreCase: true);
        if (t == typeof(DateTime)) return DateTime.Parse(value, Inv, DateTimeStyles.RoundtripKind);
        return Convert.ChangeType(value, t, Inv);
    }

    static string Format(object? value) => value switch
    {
        null => "",
        DateTime d => d.ToString("o", Inv),
        IFormattable f => f.ToString(null, Inv),
        _ => value.ToString() ?? "",
    };

    public void Save()
    {
        var sb = new StringBuilder();
        foreach (var p in Props)
            sb.Append(p.Name).Append('=').AppendLine(Format(p.GetValue(this)));
        string text = sb.ToString();
        try
        {
            File.WriteAllText(PortablePath, text);
        }
        catch
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AppDataPath)!);
                File.WriteAllText(AppDataPath, text);
            }
            catch { /* Speichern nicht möglich – App läuft trotzdem weiter */ }
        }
    }

    public static TimeSpan ParseTime(string value, int fallbackHour) =>
        TimeSpan.TryParse(value, out var t) && t >= TimeSpan.Zero && t < TimeSpan.FromDays(1)
            ? t
            : TimeSpan.FromHours(fallbackHour);
}

/// <summary>Autostart über HKCU\...\Run – ohne Installation, ohne Adminrechte.</summary>
static class Autostart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Name = "BeanMeUp";
    const string LegacyName = "WachHalter"; // Eintrag der Vorgängerversion
    static string Command => $"\"{Application.ExecutablePath}\"";

    /// <summary>
    /// War "Mit Windows starten" in der Vorgängerversion WachHalter aktiv, wird der alte Eintrag
    /// entfernt und durch einen für Bean me up ersetzt.
    /// </summary>
    public static void MigrateLegacy()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (key.GetValue(LegacyName) == null) return;
            key.DeleteValue(LegacyName, throwOnMissingValue: false);
            key.SetValue(Name, Command);
        }
        catch { /* Autostart bleibt dann unverändert */ }
    }

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(Name) is string v && string.Equals(v, Command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(Name, Command);
        else key.DeleteValue(Name, throwOnMissingValue: false);
    }
}
