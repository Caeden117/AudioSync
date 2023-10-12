using System.Diagnostics;
using System.Numerics;

namespace AudioSync.Util.FFT;

/// <summary>
/// FFT class from DSPLib, accelerated with modern .NET techniques.
/// </summary>
public sealed class FFT
{
    private const double ROOT_2 = 1.414213562367;

    private readonly double fftScale = 1;
    private readonly int fftLog2 = 0;
    private readonly int lengthTotal = 0;
    private readonly int lengthHalf = 0;
    private readonly FFTElement[] fftElements;

    private readonly double[] angleIncSin;
    private readonly double[] angleIncCos;

    public FFT(int dataLength, int zeroPaddingLength = 0)
    {
        // Set length total first, because we immediately perform validation afterwards
        lengthTotal = dataLength + zeroPaddingLength;

        var log2 = Math.Log(lengthTotal, 2);
        Debug.Assert(log2 % 1 == 0, $"{nameof(dataLength)} and {nameof(zeroPaddingLength)} must equal a power of 2.");

        // Set other parameters
        fftLog2 = (int)log2;
        lengthHalf = (lengthTotal / 2) + 1;

        // Overall scale factor
        fftScale = ROOT_2 / lengthTotal;
        fftScale *= lengthTotal / dataLength;

        // Allocate linked list
        fftElements = new FFTElement[lengthTotal];
        for (int i = 0; i < lengthTotal; i++)
        {
            FFTElement element = new()
            {
                ReversePosition = BitReverse(i, fftLog2)
            };
            fftElements[i] = element;

            // Assign "next" pointer
            if (i > 0)
            {
                fftElements[i - 1].Next = element;
            }
        }

        // Allocate sine/cosine LUTs used during FFT execution
        angleIncSin = new double[fftLog2];
        angleIncCos = new double[fftLog2];

        var indexStep = 1;
        for (int i = 0; i < fftLog2; i++)
        {
            var angleInc = indexStep * -2 * Math.PI / lengthTotal;

            angleIncSin[i] = Math.Sin(angleInc);
            angleIncCos[i] = Math.Cos(angleInc);

            // Double index step
            indexStep <<= 1;
        }
    }

    public void Execute(in Span<double> timeSeries, ref Span<Complex> allocatedOutput)
    {
        Debug.Assert(timeSeries.Length <= lengthTotal, $"Length of {nameof(timeSeries)} is expected to be at least {lengthTotal}.");
        Debug.Assert(allocatedOutput.Length <= lengthHalf, $"Length of {nameof(allocatedOutput)} expected to be at least {lengthHalf}.");

        var butterflyCount = lengthTotal >> 1;
        var butterflyWidth = lengthTotal >> 1;
        var spacing = lengthTotal;

        // Reset fft element data with data and zero-padding
        for (var i = 0; i < lengthTotal; i++)
        {
            fftElements[i].Real = timeSeries[i];
            fftElements[i].Imaginary = 0;
        }

        // Perform stages of FFT
        for (var stage = 0; stage < fftLog2; stage++)
        {
            // Compute multipliers for twiddle factors, which are complex unit vectors at regular angle intervals.
            // Using sine/cosine LUTs seems to be marginally faster than manually computing them.
            var multReal = angleIncCos[stage];
            var multImaginary = angleIncSin[stage];

            // DSPLib does not state what this for-loop is for.
            for (int start = 0; start < lengthTotal; start += spacing)
            {
                var top = fftElements[start];
                var bottom = fftElements[start + butterflyWidth];

                if (top == null || bottom == null) break;

                var real = 1.0;
                var imaginary = 0.0;

                // Perform butterflies
                for (int butterfly = 0; butterfly < butterflyCount; butterfly++)
                {
                    if (top == null || bottom == null) break;

                    var topReal = top.Real;
                    var topImaginary = top.Imaginary;
                    var bottomReal = bottom.Real;
                    var bottomImaginary = bottom.Imaginary;

                    // Top butterfly branch is addition
                    top.Real = topReal + bottomReal;
                    top.Imaginary = topImaginary + bottomImaginary;

                    // Bottom branch is subtraction, then multiplication by twiddle
                    bottomReal = topReal - bottomReal;
                    bottomImaginary = topImaginary - bottomImaginary;
                    bottom.Real = bottomReal * real - bottomImaginary * imaginary;
                    bottom.Imaginary = bottomReal * imaginary + bottomImaginary * real;

                    // Advance butterfly
                    top = top.Next;
                    bottom = bottom.Next;

                    // Update twiddle with complex multiplication
                    var tempReal = real;
                    real = real * multReal - imaginary * multImaginary;
                    imaginary = tempReal * multImaginary + imaginary * multReal;
                }
            }

            butterflyCount >>= 1;
            butterflyWidth >>= 1;
            spacing >>= 1;
        }

        // We are left with a scrambled order.
        // The second half of our FFT is all in the imaginary space, so we only want to return the first half of data.
        // We use our precomputed reverse position to unscramble the first half of our data,
        //   while also copying values and multiplying by scale factors.
        for (int i = 0; i < lengthTotal; i++)
        {
            var element = fftElements[i];
            var target = element.ReversePosition;

            if (target < lengthHalf)
            {
                allocatedOutput[target] = new(element.Real * fftScale, element.Imaginary * fftScale);
            }
        }

        // These have slightly different scaling because no imaginary component is involved.
        allocatedOutput[0] = new(allocatedOutput[0].Real / ROOT_2, 0.0);
        allocatedOutput[lengthHalf - 1] = new(allocatedOutput[lengthHalf - 1].Real / ROOT_2, 0.0);
    }

    // Reverse the specified number of bits within a given number
    private int BitReverse(int num, int bits)
    {
        var result = 0;
        for (int i = 0; i < bits; i++)
        {
            result <<= 1;
            result |= num & 0x0001;
            num >>= 1;
        }
        return result;
    }

    private class FFTElement
    {
        public double Real;
        public double Imaginary;
        public FFTElement? Next;
        public int ReversePosition;
    }
}