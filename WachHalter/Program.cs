using System.Runtime.InteropServices;

namespace WachHalter;

static class Program
{
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main()
    {
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
