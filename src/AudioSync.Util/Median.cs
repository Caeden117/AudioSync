using System.Diagnostics;
using System.Numerics;

namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Returns the median number in <paramref name="span"/>.
    /// </summary>
    /// <remarks>
    /// This method stack allocates a copy of <paramref name="span"/> to perform work, so the original <paramref name="span"/> is not modified.
    /// </remarks>
    public static double SafeMedian(in Span<double> span)
    {
        Span<double> work = stackalloc double[span.Length];
        span.CopyTo(work);

        return Median(ref work);
    }

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

#if NET7_0_OR_GREATER
    /// <summary>
    /// Returns the median number in <paramref name="span"/>.
    /// </summary>
    /// <remarks>
    /// This method stack allocates a copy of <paramref name="span"/> to perform work, so the original <paramref name="span"/> is not modified.
    /// </remarks>
    public static T SafeMedian<T>(in Span<double> span) where T : unmanaged, INumber<T>, IDivisionOperators<T, int, T>
    {
        Span<double> work = stackalloc T[span.Length];
        span.CopyTo(work);

        return Median(ref work);
    }

    /// <summary>
    /// Returns the median number in <paramref name="span"/>.
    /// </summary>
    /// <remarks>
    /// This method manipulates and sorts the source <paramref name="span"/>.
    /// </remarks>
    public static T Median<T>(ref Span<T> span) where T : INumber<T>, IDivisionOperators<T, int, T>
    {
        Debug.Assert(span.Length > 0, $"{nameof(span)} must have items to take the median of.");

        QuickSort(ref span);

        var halfIdx = span.Length / 2;

        return (span.Length % 2 == 1)
            ? span[halfIdx]
            : (span[halfIdx - 1] + span[halfIdx]) / 2;
    }
#endif

    /*
     * I did some basic benchmarking between various methods of finding median: MathNET.Numerics, Nth Order Statistics, and the method
     * that seemed to be the fastest... sort and pick median.
     * 
     * .NET doesn't seem to have sort methods for Spans, so here's a quick implementation of Quick Sort (haha) for Span
     */
    private static void QuickSort<T>(ref Span<T> span) where T : IComparable<T>
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

    private static int Partition<T>(ref Span<T> span) where T : IComparable<T>
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