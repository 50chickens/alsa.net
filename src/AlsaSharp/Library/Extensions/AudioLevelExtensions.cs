namespace AlsaSharp.Library.Extensions;

/// <summary>
/// Extension methods for audio level conversions.
/// </summary>
public static class AudioLevelExtensions
{
    /// <summary>
    /// Converts a dBFS (decibels relative to full scale) value to RMS (root mean square).
    /// </summary>
    /// <param name="dbfs">The dBFS value to convert.</param>
    /// <returns>The RMS value, or NaN if input is NaN, or 0 if input is negative infinity.</returns>
    public static double ToRms(this double dbfs)
    {
        if (double.IsNaN(dbfs))
            return double.NaN;
        if (double.IsNegativeInfinity(dbfs))
            return 0.0;
        return Math.Pow(10.0, dbfs / 20.0);
    }
}
