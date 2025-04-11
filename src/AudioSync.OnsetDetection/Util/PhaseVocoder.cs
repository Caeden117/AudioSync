using AudioSync.Util;
using AudioSync.Util.FFT;
using AudioSync.Util.Structures;
using System.Numerics;

namespace AudioSync.OnsetDetection.Util;

internal sealed class PhaseVocoder
{
    private readonly int hopSize;
    private readonly int realSize;
    private readonly FFT fft;
    private readonly float[] hannWindow;
    private readonly int end;

    public PhaseVocoder(int windowSize, int realSize, int hopSize)
    {
        fft = new FFT(windowSize);
        this.hopSize = hopSize;
        this.realSize = realSize;

        // Generate a Hann coefficient window
        hannWindow = SineExpansion(windowSize, 0.5f, -0.5f);

        end = windowSize > hopSize
            ? windowSize - hopSize
            : 0;
    }

    public void Process(in Span<float> dataNew, ref Span<Polar> allocatedOutput)
    {
        // Create working data that matches the size of our Hann window
        Span<float> dataSpan = stackalloc float[hannWindow.Length];
        dataNew.CopyTo(dataSpan);

        var windowSpan = hannWindow.AsSpan();

        // Multiply our data by Hann window
        Utils.MultiplySIMD(in dataSpan, in windowSpan, ref dataSpan);

        // Shift our data window
        Utils.Swap(ref dataSpan);

        // Perform FFT
        Span<Complex> fftData = stackalloc Complex[realSize];
        fft.Execute(in dataSpan, ref fftData);

        // Manually convert to Polar coordinates to get around some weird C# problems
        for (var i = 0; i < realSize; i++)
        {
            var complexItem = fftData[i];
            allocatedOutput[i] = new Polar((float)complexItem.Magnitude, (float)complexItem.Phase);
        }
    }

    private float[] SineExpansion(int points, params float[] coefficients)
    {
        Span<float> z = stackalloc float[points];
        for (var i = 0; i < points; i++)
        {
            z[i] = 2.0f * (float)Math.PI * i / points;
        }

        var window = new float[points];

        for (var i = 0; i < points; i++)
        {
            var coefficient = coefficients[0];
            
            for (var j = 1; j < coefficients.Length; j++)
            {
                coefficient += coefficients[j] * (float)Math.Cos(j * z[i]);
            }

            window[i] = coefficient;
        }

        return window;
    }
}