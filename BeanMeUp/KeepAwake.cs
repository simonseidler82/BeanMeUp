using System.Runtime.InteropServices;

namespace BeanMeUp;

/// <summary>Verhindert Standby (und optional das Abschalten des Bildschirms).</summary>
sealed class KeepAwake
{
    [DllImport("kernel32.dll")]
    static extern uint SetThreadExecutionState(uint flags);

    const uint ES_CONTINUOUS = 0x80000000;
    const uint ES_SYSTEM_REQUIRED = 0x00000001;
    const uint ES_DISPLAY_REQUIRED = 0x00000002;

    public bool Active { get; private set; }
    public DateTime? Until { get; private set; }
    public bool KeepDisplayOn { get; private set; } = true;

    public void Start(TimeSpan? duration, bool keepDisplayOn)
    {
        Active = true;
        Until = duration is { } d ? DateTime.Now + d : null;
        KeepDisplayOn = keepDisplayOn;
        Apply();
    }

    public void SetKeepDisplayOn(bool value)
    {
        KeepDisplayOn = value;
        if (Active) Apply();
    }

    public void Stop()
    {
        Active = false;
        Until = null;
        SetThreadExecutionState(ES_CONTINUOUS);
    }

    /// <summary>Beendet einen zeitlich begrenzten Wachmodus, sobald er abgelaufen ist.</summary>
    public void Tick()
    {
        if (Active && Until is { } u && DateTime.Now >= u)
            Stop();
    }

    // Muss vom UI-Thread aufgerufen werden: der Zustand gilt pro Thread.
    void Apply() => SetThreadExecutionState(
        ES_CONTINUOUS | ES_SYSTEM_REQUIRED | (KeepDisplayOn ? ES_DISPLAY_REQUIRED : 0));
}

/// <summary>
/// "Aktivität simulieren" (wie ein Maus-Jiggler): Ist der Benutzer eine Weile untätig,
/// wird die Maus um 1 Pixel hin und sofort zurück bewegt – unsichtbar, setzt aber den
/// Leerlauf-Zähler zurück. Verhindert so Bildschirmschoner, automatische Sperre und
/// "Abwesend" in Teams/Slack.
/// </summary>
static class ActivitySimulator
{
    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public MOUSEINPUT mi; }

    [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
    [DllImport("user32.dll")] static extern uint SendInput(uint count, INPUT[] inputs, int size);

    const uint INPUT_MOUSE = 0, MOUSEEVENTF_MOVE = 0x0001;

    public static TimeSpan IdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info)) return TimeSpan.Zero;
        return TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.dwTime));
    }

    /// <summary>Nur eingreifen, wenn seit <paramref name="threshold"/> keine echte Eingabe kam.</summary>
    public static void NudgeIfIdle(TimeSpan threshold)
    {
        if (IdleTime() < threshold) return;
        var inputs = new[] { Move(1), Move(-1) };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    static INPUT Move(int dx) => new()
    {
        type = INPUT_MOUSE,
        mi = new MOUSEINPUT { dx = dx, dwFlags = MOUSEEVENTF_MOVE },
    };
}
