using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util.SpectralDescription;

internal sealed class ComplexDomain : BaseSpectralDescription
{
    private readonly double[] oldMagnitude;
    private readonly double[] currentMeasureVector;
    private readonly double[] lastFrameVector;
    private readonly double[] secondLastFrameVector;

    public ComplexDomain(int realSize)
    {
        oldMagnitude = new double[realSize];
        currentMeasureVector = new double[realSize];
        lastFrameVector = new double[realSize];
        secondLastFrameVector = new double[realSize];
    }

    public override void Perform(in Span<Polar> fftGrain, ref double onset)
    {
        var length = fftGrain.Length;
        onset = default;

        for (var i = 0; i < length; i++)
        {
            ref var oldMag = ref oldMagnitude[i];
            ref var grain = ref fftGrain[i];

            // compute predicted phase
            currentMeasureVector[i] = (2.0 * lastFrameVector[i]) - secondLastFrameVector[i];

            // compute euclidean distance in the complex domain
            var euclideanDistance = (oldMag * oldMag) + (grain.Norm * grain.Norm);
            euclideanDistance -= 2 * oldMag * grain.Norm * Math.Cos(currentMeasureVector[i] - grain.Phase);
            euclideanDistance = Math.Abs(euclideanDistance);
            euclideanDistance = Math.Sqrt(euclideanDistance);
            onset += euclideanDistance;

            // Push back frames
            secondLastFrameVector[i] = lastFrameVector[i];
            lastFrameVector[i] = grain.Phase;

            // Push back magnitude
            oldMag = grain.Norm;
        }
    }
}
