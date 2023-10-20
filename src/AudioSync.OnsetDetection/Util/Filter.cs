using System.Diagnostics;

namespace AudioSync.OnsetDetection.Util;

internal abstract class Filter
{
    protected readonly double[] feedback;
    protected readonly double[] forward;

    private readonly int order;

    public Filter(int order)
    {
        Debug.Assert(order > 0, $"{nameof(order)} must be greater than 0.");

        this.order = order;

        feedback = new double[order];
        forward = new double[order];

        // Default to identity
        feedback[0] = forward[0] = 1.0;
    }

    public void ForwardFilter(ref Span<double> span)
    {
        Span<double> x = stackalloc double[order];
        x.Clear();
        
        Span<double> y = stackalloc double[order];
        y.Clear();

        for (var i = 0; i < span.Length; i++)
        {
            ref var input = ref span[i];
            ref var startX = ref x[0];
            ref var startY = ref y[0];

            // new input
            // "denormal" appears to be any value less than 0, so we'll just clamp to that
            startX = Math.Max(input, 0.0);
            startY = forward[0] * startX;

            for (var j = 1; j < order; j++)
            {
                startY += forward[j] * x[j];
                startY -= feedback[j] * y[j];
            }

            // new output
            input = startY;

            // prepare next sample
            for (var j = order - 1; j > 0; j--)
            {
                x[j] = x[j - 1];
                y[j] = y[j - 1];
            }
        }
    }

    /// <summary>
    /// Performs a double filter (forward and backward) over the input span, compensating for any phase shifts from a single filter.
    /// </summary>
    public void DoubleFilter(ref Span<double> input)
    {
        // First filter, reversing our input afterwards in preparation for the second filter
        ForwardFilter(ref input);
        input.Reverse();

        // Second filter, reversing our input afterwards to return our span back to its original order.
        ForwardFilter(ref input);
        input.Reverse();
    }
}