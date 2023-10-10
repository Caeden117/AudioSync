using AudioSync.OnsetDetection.Structures;
using System.Diagnostics;
using System.Numerics;

namespace AudioSync.OnsetDetection.Util;

internal static class Utils
{
    private const double epsilon = 2e-42;

    public static bool IsDenormal(double f) => f < epsilon;

    public static double KillDenormal(double f) => Math.Min(f, 0.0);

    /// <summary>
    /// Multiplies span <paramref name="a"/> and <paramref name="b"/> into span <paramref name="result"/>.
    /// </summary>
    public static void Multiply(in Span<double> a, in Span<double> b, ref Span<double> result)
    {
        Debug.Assert(a.Length == b.Length, $"Lengths of {nameof(a)} and {nameof(b)} are not equal.");
        Debug.Assert(a.Length == result.Length, $"Length of {nameof(result)} expected to be {a.Length}.");

        for (int i = 0; i < a.Length; i++)
        {
            result[i] = a[i] * b[i];
        }
    }

    /// <summary>
    /// Multiplies span <paramref name="a"/> and <paramref name="b"/> into span <paramref name="result"/>.
    /// </summary>
    /// <remarks>
    /// Accelerated by SIMD (Same Instruction, Multiple Data). Not supported on all architectures.
    /// </remarks>
    public static void MultiplySIMD(in Span<double> a, in Span<double> b, ref Span<double> result)
    {
        Debug.Assert(a.Length == b.Length, $"Lengths of {nameof(a)} and {nameof(b)} are not equal.");
        Debug.Assert(a.Length == result.Length, $"Length of {nameof(result)} expected to be {a.Length}.");

        // Variables we will use throughout the method
        var length = a.Length;
        var vectorCount = Vector<double>.Count;
        var remaining = length % vectorCount;

        // Perform SIMD-accelerated multiplication on as many groups of numbers as we can
        for (int i = 0; i < length - remaining; i += vectorCount)
        {
            var sliceA = a.Slice(i, vectorCount);
            var sliceB = b.Slice(i, vectorCount);

            var vectorA = new Vector<double>(sliceA);
            var vectorB = new Vector<double>(sliceB);
            var vectorResult = vectorA * vectorB;

#if NETCOREAPP3_0_OR_GREATER
            var resultSlice = result.Slice(i, vectorCount);
            vectorResult.CopyTo(resultSlice);
#else
            // Manual copy into result span, because .NET Standard doesnt have Vector<T>.CopyTo(Span<T>)???
            for (int j = 0; j < vectorCount; j++)
            {
                result[i + j] = vectorResult[j];
            }
#endif
        }

        // With the remaining couple of elements left in the span, we will just manually multiply them.
        var remainingStartIdx = length - remaining - 1;

        var remainingA = a.Slice(remainingStartIdx, remaining);
        var remainingB = b.Slice(remainingStartIdx, remaining);
        var remainingResult = result.Slice(remainingStartIdx, remaining);
        
        Multiply(in remainingA, in remainingB, ref remainingResult);
    }

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
