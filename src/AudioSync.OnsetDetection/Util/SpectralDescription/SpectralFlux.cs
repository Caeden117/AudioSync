using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util.SpectralDescription;

internal sealed class SpectralFlux : BaseSpectralDescription
{
    private readonly float[] oldMagnitude;

    public SpectralFlux(int realSize) => oldMagnitude = new float[realSize];

    public override void Perform(in Span<Polar> fftGrain, ref float onset)
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
