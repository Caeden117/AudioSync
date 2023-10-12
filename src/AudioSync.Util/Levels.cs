namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Gets the average linear level of the given <paramref name="span"/>.
    /// </summary>
    public static double LevelLinear(in Span<double> span)
    {
        var energy = 0.0;
        for (var i = 0; i < span.Length; i++)
        {
            energy += span[i] * span[i];
        }
        return energy / span.Length;
    }

    /// <summary>
    /// Gets the average sound pressure level of the given <paramref name="span"/> in db.
    /// </summary>
    public static double DBSoundPressureLevel(in Span<double> span)
        => 10.0 * Math.Log10(LevelLinear(in span));

    /// <summary>
    /// Determines if the given <paramref name="span"/> is "silent", if the average sound pressure level is below the given <paramref name="threshold"/>.
    /// </summary>
    public static bool IsSilence(in Span<double> span, double threshold)
        => DBSoundPressureLevel(in span) < threshold;
}