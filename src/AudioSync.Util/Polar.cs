using AudioSync.Util.Structures;
using System.Numerics;

namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Converts Complex results (usually from FFT) to Polar coordinates.
    /// </summary>
    public static void ToPolar(in Span<Complex> complex, ref Span<Polar> allocatedOutput)
    {
        for (var i = 0; i < complex.Length; i++)
        {
            var complexItem = complex[i];
            allocatedOutput[i] = new(complexItem.Magnitude, complexItem.Phase);
        }
    }
}