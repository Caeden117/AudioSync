using AudioSync.Util;
using AudioSync.Util.FFT;
using AudioSync.Util.Structures;
using System.Numerics;

namespace AudioSync.OnsetDetection.Util;

internal sealed class PhaseVocoder
{
    private readonly int hopSize;
    private readonly FFT fft;
    private readonly double[] data;
    private readonly double[] dataOld;
    private readonly double[] hannWindow;
    private readonly int end;

    public PhaseVocoder(int windowSize, int hopSize)
    {
        fft = new FFT(windowSize);
        this.hopSize = hopSize;

        data = new double[windowSize];
        dataOld = windowSize > hopSize
            ? new double[windowSize - hopSize]
            : new double[1];

        // Generate a Hann coefficient window
        hannWindow = SineExpansion(windowSize, 0.5, -0.5);

        end = windowSize > hopSize
            ? windowSize - hopSize
            : 0;
    }

    public void Do(in Span<double> dataNew, ref Span<Polar> allocatedOutput)
    {
        // Slide new data
        SwapBuffers(in dataNew);

        var dataSpan = data.AsSpan();
        var windowSpan = hannWindow.AsSpan();

        // Multiply our data by Hann window
        Utils.Multiply(in dataSpan, in windowSpan, ref dataSpan);

        // Shift our data window
        Utils.Swap(ref dataSpan);

        // Perform FFT
        Span<Complex> fftData = stackalloc Complex[data.Length];
        fft.Execute(in dataSpan, ref fftData);

        // To prevent weird C# errors, we need to make a temporary output buffer
        // to store our Complex-converted-Polar values, *then* copy that into the
        // allocatedOutput buffer
        Span<Polar> tempOutput = stackalloc Polar[allocatedOutput.Length];
        Utils.ToPolar(in fftData, ref tempOutput);
        tempOutput.CopyTo(allocatedOutput);
    }

    private void SwapBuffers(in Span<double> dataNew)
    {
        Array.Copy(dataOld, 0, data, 0, end);
        
        var dataOldSpan = dataOld.AsSpan();
        dataNew[..hopSize].CopyTo(dataOldSpan[end..]);

        Array.Copy(data, hopSize, dataOld, 0, end);
    }

    private double[] SineExpansion(int points, params double[] coefficients)
    {
        Span<double> z = stackalloc double[points];
        for (var i = 0; i < points; i++)
        {
            z[i] = 2.0 * Math.PI * i / points;
        }

        var window = new double[points];

        for (var i = 0; i < points; i++)
        {
            var coefficient = coefficients[0];
            
            for (var j = 1; j < coefficients.Length; j++)
            {
                coefficient += coefficients[j] * Math.Cos(j * z[i]);
            }

            window[i] = coefficient;
        }

        return window;
    }
}