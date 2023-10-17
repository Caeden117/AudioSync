using AudioSync.Util.Exceptions;

namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Swap the two halves of <paramref name="span"/>.
    /// </summary>
    public static void Swap(ref Span<double> span)
    {
        if (span.Length <= 1) throw new AudioSyncFatalException($"{nameof(span)} needs to have at least 2 elements.");

        var half = span.Length / 2;
        var halfIdx = half + (span.Length % 2);

        Span<double> temp = stackalloc double[half];

        var firstHalf = span.Slice(0, half);
        var secondHalf = span.Slice(halfIdx, half);

        firstHalf.CopyTo(temp);
        secondHalf.CopyTo(firstHalf);
        temp.CopyTo(secondHalf);

        // If we are handling an odd number of elements, push the middle element to the back of the second half.
        if (halfIdx == half) return;

        var secondHalfIncludingMiddle = span[half..];

        Push(ref secondHalfIncludingMiddle, secondHalfIncludingMiddle[0]);
    }

    /// <summary>
    /// Pushes <paramref name="element"/> to the end of <paramref name="span"/>, shifting all existing elements forward.
    /// </summary>
    /// <remarks>
    /// The first element of <paramref name="span"/> will be pushed out and lost.
    /// </remarks>
    public static void Push(ref Span<double> span, double element)
    {
        if (span.IsEmpty) throw new AudioSyncFatalException($"{nameof(span)} cannot be empty.");

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