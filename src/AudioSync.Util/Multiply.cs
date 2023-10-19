using AudioSync.Util.Exceptions;
using System.Numerics;

namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Multiplies span <paramref name="a"/> and <paramref name="b"/> into span <paramref name="result"/>.
    /// </summary>
    public static void Multiply(in Span<double> a, in Span<double> b, ref Span<double> result)
    {
        if (a.Length != b.Length) throw new AudioSyncFatalException($"Lengths of {nameof(a)} and {nameof(b)} are not equal.");
        if (a.Length != result.Length) throw new AudioSyncFatalException($"Length of {nameof(result)} must match the length of the input parameters.");

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
        if (a.Length != b.Length) throw new AudioSyncFatalException($"Lengths of {nameof(a)} and {nameof(b)} are not equal.");
        if (a.Length != result.Length) throw new AudioSyncFatalException($"Length of {nameof(result)} must match the length of the input parameters.");

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
        var remainingStartIdx = length - remaining;

        var remainingA = a[remainingStartIdx..];
        var remainingB = b[remainingStartIdx..];
        var remainingResult = result[remainingStartIdx..];

        Multiply(in remainingA, in remainingB, ref remainingResult);
    }
}