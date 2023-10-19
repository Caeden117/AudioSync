using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util.SpectralDescription;

internal sealed class SpectralFlux : BaseSpectralDescription
{
    private readonly double[] oldMagnitude;

    public SpectralFlux(int realSize) => oldMagnitude = new double[realSize];

    public override void Perform(in Span<Polar> fftGrain, ref double onset)
    {
        onset = default;

        for (var i = 0; i < fftGrain.Length; i++)
        {
            ref var grain = ref fftGrain[i];
            ref var mag = ref oldMagnitude[i];

            if (grain.Norm > mag)
            {
                onset += grain.Norm - mag;
            }

            mag = grain.Norm;
        }
    }
}
