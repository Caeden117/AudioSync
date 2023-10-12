using AudioSync.Util;

namespace AudioSync.OnsetDetection.Util;

internal sealed class PeakPicker
{
    public double Threshold { get; set; }

    public double Thresholded { get; set; }

    private readonly int windowPost;
    private readonly int windowPre;
    private readonly BiquadFilter biquadFilter;
    private readonly double[] window;
    private readonly double[] onsetPeek;

    public PeakPicker()
    {
        Threshold = 0.1;
        windowPost = 5;
        windowPre = 1;

        window = new double[windowPost + windowPre + 1];
        onsetPeek = new double[3];

        biquadFilter = new(0.15998789, 0.31997577, 0.15998789, 0.23484048, 0);
    }

    public void Do(in Span<double> onset, ref Span<double> output)
    {
        // Push first onset into our window
        Span<double> windowSpan = window.AsSpan();
        Utils.Push(ref windowSpan, onset[0]);

        // Stackalloc a copy of our window to perform work with
        Span<double> modifiedSpan = stackalloc double[windowSpan.Length];
        windowSpan.CopyTo(modifiedSpan);

        // Perform double biquad filtering on our working set
        biquadFilter.DoubleFilter(ref modifiedSpan);

        // Manually calculate average of our modified span
        // (this span will never be large enough to see improvements from the likes of SIMD)
        var acc = 0.0;
        for (var i = 0; i < modifiedSpan.Length; i++)
        {
            acc += modifiedSpan[i];
        }
        var mean = acc / modifiedSpan.Length;

        // Calculate median of our working set (using the safe method, as we do not want to modify our working set)
        var median = Utils.SafeMedian(in modifiedSpan);

        // Calculate thresholded value
        Thresholded = modifiedSpan[windowPost] - median - (mean * Threshold);

        // Push thresholded value onto peek array
        var peekSpan = onsetPeek.AsSpan();
        Utils.Push(ref peekSpan, Thresholded);

        // Push peak onto first output element if we detect one
        if (Utils.PeakPick(in peekSpan, 1))
        {
            output[0] = Utils.QuadraticPeakPos(in peekSpan, 1);
        }
    }
}
