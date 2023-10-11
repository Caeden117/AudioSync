using System.Diagnostics;

namespace AudioSync.Util;

public static partial class Utils
{
    /*
     * This is an entirely different algorithm from Aubio, so I am unsure if this gives completely correct results compared to Aubio.
     * Will have to test on a direct C# reimplementation.
     */

    /// <summary>
    /// Returns the median number in <paramref name="span"/>.
    /// </summary>
    /// <remarks>
    /// This method manipulates and sorts the source <paramref name="span"/>.
    /// </remarks>
    public static double Median(ref Span<double> span)
    {
        Debug.Assert(span.Length > 0, $"{nameof(span)} must have items to take the median of.");

        QuickSort(ref span);

        var halfIdx = span.Length / 2;

        return (span.Length % 2 == 1)
            ? span[halfIdx]
            : (span[halfIdx - 1] + span[halfIdx]) / 2;
    }

    private static void QuickSort(ref Span<double> span)
    {
        if (span.Length < 2) return;

        var q = Partition(ref span);

        var leftHalf = span[..q++];
        QuickSort(ref leftHalf);

        if (q < span.Length - 1)
        {
            var rightHalf = span[q..];
            QuickSort(ref rightHalf);
        }
    }

    private static int Partition(ref Span<double> span)
    {
        ref var pivot = ref span[^1];
        var i = -1;
        for (int j = 0; j < span.Length - 1; j++)
        {
            if (span[j].CompareTo(pivot) <= 0)
            {
                i++;
                (span[i], span[j]) = (span[j], span[i]);
            }
        }
        var q = i + 1; //pivotPosition
        (span[q], pivot) = (pivot, span[q]);
        return q;
    }
}