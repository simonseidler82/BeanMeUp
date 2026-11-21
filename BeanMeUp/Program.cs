using System.Runtime.InteropServices;

namespace BeanMeUp;

/// <summary>
/// Bean me up – hält den PC wach und schaltet Windows automatisch zwischen Hell und Dunkel.
/// (Vorgängername: WachHalter)
/// Erstellt von Simon Seidler · powered by Claude.
/// </summary>
static class Program
{
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main(string[] args)
    {
        // Entwickler-Funktion: Programm-Icon als .ico erzeugen (siehe README)
        if (args.Length == 2 && args[0] == "--icon-export")
        {
            Art.SaveIco(args[1]);
            return;
        }

        // Vor dem ersten Fenster (auch vor der Meldung unten): scharfe Darstellung bei
        // Skalierung > 100 % und moderne Windows-Steuerelemente
        SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var mutex = new Mutex(true, @"Local\BeanMeUp_SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show(
                $"{TrayApp.AppName} läuft bereits.\n\n" +
                "Klicke auf das Tassen-Symbol im Infobereich der Taskleiste, um das Fenster zu öffnen.",
                TrayApp.AppName, MessageBoxButtons.OK, MessageBoxIcon.None);
            return;
        }

        // Nie mehr kommentarlos verschwinden: unerwartete Fehler werden angezeigt
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowError(e.Exception, fatal: false);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ShowError(e.ExceptionObject as Exception ?? new Exception("Unbekannter Fehler"), fatal: true);

        TrayApp app;
        try
        {
            app = new TrayApp();
        }
        catch (Exception ex)
        {
            ShowError(ex, fatal: true);
            return;
        }
        Application.Run(app);
    }

    static void ShowError(Exception ex, bool fatal) =>
        MessageBox.Show(
            (fatal ? $"{TrayApp.AppName} konnte nicht gestartet werden.\n\n"
                   : $"In {TrayApp.AppName} ist ein Fehler aufgetreten. Das Programm läuft weiter.\n\n") +
            $"{ex.GetType().Name}: {ex.Message}",
            TrayApp.AppName, MessageBoxButtons.OK, MessageBoxIcon.None);
}
