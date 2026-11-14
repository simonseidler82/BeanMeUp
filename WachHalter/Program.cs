using System.Runtime.InteropServices;

namespace WachHalter;

/// <summary>
/// WachHalter – hält den PC wach und schaltet Windows automatisch zwischen Hell und Dunkel.
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

        using var mutex = new Mutex(true, @"Local\WachHalter_SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show("WachHalter läuft bereits – siehe Infobereich der Taskleiste.",
                "WachHalter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetProcessDPIAware(); // scharfe Darstellung bei Skalierung > 100 %
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }
}
