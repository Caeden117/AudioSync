using System.Diagnostics;

namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Swap the two halves of <paramref name="span"/>.
    /// </summary>
    public static void Shift(ref Span<double> span)
    {
        Debug.Assert(span.Length > 1, $"{nameof(span)} needs to have at least 2 elements.");

        var half = span.Length / 2;
        var halfIdx = half + (span.Length % 2);

        Span<double> temp = stackalloc double[half];

        var firstHalf = span.Slice(0, half);
        var secondHalf = span.Slice(halfIdx, half);

        firstHalf.CopyTo(temp);
        secondHalf.CopyTo(firstHalf);
        temp.CopyTo(secondHalf);
    }

    /// <summary>
    /// Pushes <paramref name="element"/> to the end of <paramref name="span"/>, shifting all existing elements forward.
    /// </summary>
    /// <remarks>
    /// The first element of <paramref name="span"/> will be pushed out and lost.
    /// </remarks>
    // Is this safe?? Need to test.
    public static void Push(ref Span<double> span, double element)
    {
        Debug.Assert(!span.IsEmpty, $"{nameof(span)} cannot be empty.");

        if (span.Length == 1)
        {
            span[0] = element;
            return;
        }

        var slice = span[1..];
        slice.CopyTo(span);
        span[^1] = element;
    }
}