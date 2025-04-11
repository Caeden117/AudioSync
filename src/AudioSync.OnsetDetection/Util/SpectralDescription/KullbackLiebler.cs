using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util.SpectralDescription;

internal sealed class KullbackLiebler : BaseSpectralDescription
{
    private readonly float[] oldMagnitude;

    public KullbackLiebler(int realSize) => oldMagnitude = new float[realSize];

    public override void Perform(in Span<Polar> fftGrain, ref float onset)
    {
        onset = default;

        for (var i = 0; i < fftGrain.Length; i++)
        {
            ref var grain = ref fftGrain[i];
            ref var mag = ref oldMagnitude[i];

            onset += grain.Norm * MathF.Log(1 + (grain.Norm / (mag + 1e-1f)));
            mag = grain.Norm;
        }

        if (float.IsNaN(onset)) onset = default;
    }
}
