namespace WachHalter;

/// <summary>Sonnenauf- und -untergang offline berechnet (Almanac-for-Computers-Algorithmus, ca. ±2 Min).</summary>
static class SunCalc
{
    public static (DateTime? Sunrise, DateTime? Sunset) Get(DateTime date, double lat, double lon) =>
        (Calc(date, lat, lon, rising: true), Calc(date, lat, lon, rising: false));

    static DateTime? Calc(DateTime date, double lat, double lon, bool rising)
    {
        const double zenith = 90.833; // offizieller Auf-/Untergang inkl. Refraktion
        int n = date.DayOfYear;
        double lngHour = lon / 15.0;
        double t = n + ((rising ? 6 : 18) - lngHour) / 24.0;

        double m = 0.9856 * t - 3.289;
        double l = Norm(m + 1.916 * Sin(m) + 0.020 * Sin(2 * m) + 282.634, 360);

        double ra = Norm(Atan(0.91764 * Tan(l)), 360);
        ra += Math.Floor(l / 90) * 90 - Math.Floor(ra / 90) * 90;
        ra /= 15;

        double sinDec = 0.39782 * Sin(l);
        double cosDec = Math.Cos(Math.Asin(sinDec));
        double cosH = (Cos(zenith) - sinDec * Sin(lat)) / (cosDec * Cos(lat));
        if (cosH > 1 || cosH < -1) return null; // Polartag / Polarnacht

        double h = (rising ? 360 - Acos(cosH) : Acos(cosH)) / 15;
        double localMeanTime = h + ra - 0.06571 * t - 6.622;
        double ut = Norm(localMeanTime - lngHour, 24);

        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc).AddHours(ut).ToLocalTime();
    }

    static double Rad(double d) => d * Math.PI / 180;
    static double Deg(double r) => r * 180 / Math.PI;
    static double Sin(double d) => Math.Sin(Rad(d));
    static double Cos(double d) => Math.Cos(Rad(d));
    static double Tan(double d) => Math.Tan(Rad(d));
    static double Atan(double x) => Deg(Math.Atan(x));
    static double Acos(double x) => Deg(Math.Acos(x));
    static double Norm(double v, double max) => ((v % max) + max) % max;
}
