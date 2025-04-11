using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util.SpectralDescription;

internal sealed class ComplexDomain : BaseSpectralDescription
{
    private readonly float[] oldMagnitude;
    private readonly float[] currentMeasureVector;
    private readonly float[] lastFrameVector;
    private readonly float[] secondLastFrameVector;

    public ComplexDomain(int realSize)
    {
        oldMagnitude = new float[realSize];
        currentMeasureVector = new float[realSize];
        lastFrameVector = new float[realSize];
        secondLastFrameVector = new float[realSize];
    }

    public override void Perform(in Span<Polar> fftGrain, ref float onset)
    {
        var length = fftGrain.Length;
        onset = default;

        for (var i = 0; i < length; i++)
        {
            ref var oldMag = ref oldMagnitude[i];
            ref var grain = ref fftGrain[i];

            // compute predicted phase
            currentMeasureVector[i] = (2.0f * lastFrameVector[i]) - secondLastFrameVector[i];

            // compute euclidean distance in the complex domain
            var euclideanDistance = (oldMag * oldMag) + (grain.Norm * grain.Norm);
            euclideanDistance -= 2 * oldMag * grain.Norm * (float)Math.Cos(currentMeasureVector[i] - grain.Phase);
            euclideanDistance = (float)Math.Abs(euclideanDistance);
            euclideanDistance = (float)Math.Sqrt(euclideanDistance);
            onset += euclideanDistance;

            // Push back frames
            secondLastFrameVector[i] = lastFrameVector[i];
            lastFrameVector[i] = grain.Phase;

            // Push back magnitude
            oldMag = grain.Norm;
        }
    }
}
