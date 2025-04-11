namespace AudioSync.OnsetDetection.Util;

internal sealed class BiquadFilter : Filter
{
    // b = forward filter coefficients
    // a = feedback coefficients
    public BiquadFilter(float b0, float b1, float b2, float a1, float a2) : base(3)
    {
        forward[0] = b0;
        forward[1] = b1;
        forward[2] = b2;

        feedback[0] = 1.0f;
        feedback[1] = a1;
        feedback[2] = a2;
    }
}
