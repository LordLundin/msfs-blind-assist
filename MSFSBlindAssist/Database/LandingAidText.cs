using System.Globalization;

namespace MSFSBlindAssist.Database;

/// <summary>
/// Names a runway end's radio landing aid the one way the app says it everywhere: the EFB
/// runway page and the runway description (<see cref="Models.Runway.ToString"/>).
///
/// <para>A runway end has at most one localizer. With a glideslope it is an ILS, without one
/// it is a localizer only, and the two must never sound alike: at ESST (Torsby) both ends have
/// a localizer and no glideslope, and the EFB headed them "ILS INFORMATION" with "ILS Ident"
/// and "ILS Frequency" rows, so a pilot planned an ILS the G1000 rightly never offered.</para>
///
/// <para>Whether there is a glideslope is read from the glideslope data, never from the
/// facility's name or its type code alone. In an MSFS 2020 navdatareader build every `ils` row
/// of type "0" (321 of 4,515) has a NULL <c>gs_range</c> and <c>gs_pitch</c>, and 16 of them are
/// still NAMED "ILS RW…"; types "1", "2", "3" and "U" all carry both. The category is said
/// only when it is II or III: CAT I is 3,253 of the 4,082 ILS and would be noise on every
/// one, while CAT II/III is what an autoland in fog is planned on.</para>
///
/// <para>The words carry no redundancy, as agreed with the owner (2026-11-01): "ILS" already
/// means a glideslope and "Localizer" means none, so neither says "glideslope" or "no
/// glideslope". The glideslope ANGLE is said only when it is not 3.0 degrees, which is 483 of
/// 4,194 glideslopes in an MSFS 2020 build: a steep one (London City and São Paulo 5.5, Sion
/// 6.0, Lugano 6.65) changes how the approach is flown, while "3.0" on every ILS is noise.</para>
/// </summary>
public static class LandingAidText
{
    /// <summary>A glideslope exists when the row carries a glideslope range or angle.</summary>
    public static bool HasGlideslope(double? gsRange, double? gsPitch)
        => (gsRange ?? 0) > 0 || (gsPitch ?? 0) > 0;

    /// <summary>"CAT II" or "CAT III" for navdatareader's type codes "2" and "3"; null for
    /// every other code (CAT I, localizer-only, unknown), which is not said.</summary>
    public static string? Category(string? ilsType) => ilsType?.Trim() switch
    {
        "2" => "CAT II",
        "3" => "CAT III",
        _ => null
    };

    /// <summary>Magnetic course from the localizer's true heading and the magnetic variation
    /// (east positive, as navdata stores it), as a whole degree from 1 to 360.
    /// <para>⚠️ Pass the LOCALIZER's own variation (`ils.mag_var`), and the airport's only when
    /// the row has none. navdata stores each localizer's published course as its true heading
    /// plus its own variation: true less `ils.mag_var` is a whole degree for 4,508 of 4,515
    /// rows (MSFS 2020), while the airport's variation gives a different course for 2,372 of
    /// them, 38 degrees off at BGTL (85 published, 123 from the airport's).</para></summary>
    public static int MagneticCourse(double locHeadingTrue, double magVar)
    {
        int rounded = (int)Math.Round(locHeadingTrue - magVar, MidpointRounding.AwayFromZero);
        return ((rounded % 360) + 360 - 1) % 360 + 1;
    }

    /// <summary>
    /// "Localizer 108.70, course 152", "ILS 110.10, course 171", "ILS CAT III 110.30, course
    /// 270", or with a glideslope that is not 3.0 degrees "ILS 110.10, course 90, glideslope
    /// 5.5 degrees". An unknown angle (0) is not said. Every number reads with a dot whatever
    /// the Windows number format.
    /// </summary>
    public static string Describe(bool hasGlideslope, string? ilsType, double frequencyMhz,
        int magneticCourse, double glideslopePitch)
    {
        var inv = CultureInfo.InvariantCulture;
        string freq = Math.Round(frequencyMhz, 2, MidpointRounding.AwayFromZero).ToString("0.00", inv);
        string course = magneticCourse.ToString(inv);

        if (!hasGlideslope)
            return $"Localizer {freq}, course {course}";

        string? category = Category(ilsType);
        string name = category == null ? "ILS" : $"ILS {category}";
        string text = $"{name} {freq}, course {course}";

        double angle = Math.Round(glideslopePitch, 1, MidpointRounding.AwayFromZero);
        if (angle > 0 && angle != StandardGlideslopeDeg)
            text += $", glideslope {angle.ToString("0.0", inv)} degrees";
        return text;
    }

    /// <summary>The usual glideslope angle, which is not said.</summary>
    public const double StandardGlideslopeDeg = 3.0;

    /// <summary>What a runway end with no localizer says.</summary>
    public const string NoLocalizer = "No localizer";
}
