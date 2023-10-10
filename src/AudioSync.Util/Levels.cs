namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Gets the average linear level of the given <paramref name="span"/>.
    /// </summary>
    public static double LevelLinear(Span<double> span)
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
    public static double DBSoundPressureLevel(Span<double> span)
        => 10.0 * Math.Log10(LevelLinear(span));

    /// <summary>
    /// Determines if the given <paramref name="span"/> is "silent", if the average sound pressure level is below the given <paramref name="threshold"/>.
    /// </summary>
    public static bool IsSilence(Span<double> span, double threshold)
        => DBSoundPressureLevel(span) < threshold;
}