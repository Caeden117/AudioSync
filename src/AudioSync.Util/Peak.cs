namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Simple peak index algorithm.
    /// </summary>
    public static bool PeakPick(in Span<float> onset, int position)
    {
        if (position <= 0 || position >= onset.Length - 1) throw new ArgumentOutOfRangeException(nameof(position));

        return onset[position] > onset[position - 1]
            && onset[position] > onset[position + 1]
            && onset[position] > 0.0;
    }

    /// <summary>
    /// Use quadratic interpolation to find the exact peak index
    /// </summary>
    public static float QuadraticPeakPos(in Span<float> onset, int position)
    {
        if (position == 0 || position == onset.Length - 1) return position;

        var x0 = (position < 1) ? position : position - 1;
        var x2 = (position + 1 < onset.Length) ? position + 1 : position;
        
        if (x0 == position) return (onset[position] <= onset[x2]) ? position : x2;
        
        if (x2 == position) return (onset[position] <= onset[x0]) ? position : x0;
        
        var s0 = onset[x0];
        var s1 = onset[position];
        var s2 = onset[x2];

        return position + (0.5f * (s0 - s2) / (s0 - (2 * s1) + s2));
    }
}