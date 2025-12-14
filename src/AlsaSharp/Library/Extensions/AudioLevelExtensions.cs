namespace AlsaSharp.Library.Extensions;

public static class AudioLevelExtensions
{
    public static double ToRms(this double dbfs)
    {
        if (double.IsNaN(dbfs))
            return double.NaN;
        if (double.IsNegativeInfinity(dbfs))
            return 0.0;
        return Math.Pow(10.0, dbfs / 20.0);
    }
}
