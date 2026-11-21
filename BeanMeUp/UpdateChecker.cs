using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BeanMeUp;

/// <summary>Ergebnis einer Update-Prüfung.</summary>
sealed class UpdateResult
{
    public Version? Latest { get; set; }
    public string Url { get; set; } = UpdateChecker.ReleasesPage;
    public string? Error { get; set; }
    public bool IsNewer => Latest != null && UpdateChecker.IsNewer(Latest);
}

/// <summary>
/// Fragt bei GitHub nach, ob es einen neueren Release gibt. Es wird nichts heruntergeladen oder
/// installiert – bei einer neuen Version wird nur auf die Download-Seite hingewiesen.
/// </summary>
static class UpdateChecker
{
    public const string ReleasesPage = "https://github.com/simonseidler82/BeanMeUp/releases/latest";
    const string Api = "https://api.github.com/repos/simonseidler82/BeanMeUp/releases/latest";

    /// <summary>
    /// Installierte Version. Zum Testen kann sie mit der Umgebungsvariable
    /// BEANMEUP_TEST_CURRENT_VERSION (z. B. "1.0.0") vorgetäuscht werden.
    /// </summary>
    public static Version Current
    {
        get
        {
            var test = Environment.GetEnvironmentVariable("BEANMEUP_TEST_CURRENT_VERSION");
            if (!string.IsNullOrEmpty(test) && Version.TryParse(test, out var v)) return v;
            return typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(0, 0);
        }
    }

    public static bool IsNewer(Version latest) => Normalize(latest) > Normalize(Current);

    static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build));

    public static Task<UpdateResult> CheckAsync() => Task.Run(() =>
    {
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var request = (HttpWebRequest)WebRequest.Create(Api);
            request.UserAgent = $"BeanMeUp/{Normalize(Current)}";
            request.Accept = "application/vnd.github+json";
            request.Timeout = 15000;

            string json;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()!))
                json = reader.ReadToEnd();

            var tag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"v?(\\d+(?:\\.\\d+){1,3})\"");
            if (!tag.Success)
                return new UpdateResult { Error = "Unexpected response from GitHub" };

            var page = Regex.Match(json, "\"html_url\"\\s*:\\s*\"(https://github\\.com/[^\"]+/releases/tag/[^\"]+)\"");
            return new UpdateResult
            {
                Latest = new Version(tag.Groups[1].Value),
                Url = page.Success ? page.Groups[1].Value : ReleasesPage,
            };
        }
        catch (Exception ex)
        {
            return new UpdateResult { Error = ex.Message };
        }
    });

    /// <summary>Öffnet die Download-Seite im Standardbrowser.</summary>
    public static void OpenPage(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* kein Browser verfügbar */ }
    }
}
