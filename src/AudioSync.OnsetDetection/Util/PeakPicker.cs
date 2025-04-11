using AudioSync.Util;

namespace AudioSync.OnsetDetection.Util;

internal sealed class PeakPicker
{
    public float Threshold { get; set; }

    public float Thresholded { get; set; }

    private readonly int windowPost;
    private readonly int windowPre;
    private readonly BiquadFilter biquadFilter;
    private readonly float[] window;
    private readonly float[] onsetPeek;

    public PeakPicker()
    {
        Threshold = 0.1f;
        windowPost = 5;
        windowPre = 1;

        window = new float[windowPost + windowPre + 1];
        onsetPeek = new float[3];

        biquadFilter = new(0.15998789f, 0.31997577f, 0.15998789f, 0.23484048f, 0);
    }

    public void FindPeak(in float onset, ref float lastOnset)
    {
        // Push first onset into our window
        Span<float> windowSpan = window.AsSpan();
        Utils.Push(ref windowSpan, onset);

        // Stackalloc a copy of our window to perform work with
        Span<float> modifiedSpan = stackalloc float[windowSpan.Length];
        windowSpan.CopyTo(modifiedSpan);

        // Perform double biquad filtering on our working set
        biquadFilter.DoubleFilter(ref modifiedSpan);

        // Manually calculate average of our modified span
        // (this span will never be large enough to see improvements from the likes of SIMD)
        var acc = 0.0f;
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

        // Update last onset if we detect one
        if (Utils.PeakPick(in peekSpan, 1))
        {
            lastOnset = Utils.QuadraticPeakPos(in peekSpan, 1);
        }
    }
}
