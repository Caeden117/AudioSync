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

        // REVIEW: Aubio implementation, in the case of even numbered collections, seems to return the middle-left element, not the average.
        //   Compare our BPM detection results to see if we need to make the same decision.
        return (span.Length % 2 == 1)
            ? span[halfIdx]
            : (span[halfIdx - 1] + span[halfIdx]) / 2;
    }

    /*
     * I did some basic benchmarking between various methods of finding median: MathNET.Numerics, Nth Order Statistics, and the method
     * that seemed to be the fastest... sort and pick median.
     * 
     * .NET Standard 2.1 doesn't seem to have sort methods for Spans, so here's a quick implementation of Quick Sort (haha) for Span
     */
    /// <summary>
    /// A Quick Sort implementation for <see cref="Span{T}"/>, offering sorting capabilities for collections on both the heap and the stack.
    /// </summary>
    /// <typeparam name="TComparable">Comparable type.</typeparam>
    /// <param name="span">Span to sort.</param>
    public static void QuickSort<TComparable>(ref Span<TComparable> span) where TComparable : IComparable<TComparable>
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