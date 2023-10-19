using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util.SpectralDescription;

internal sealed class KullbackLiebler : BaseSpectralDescription
{
    private readonly double[] oldMagnitude;

    public KullbackLiebler(int realSize) => oldMagnitude = new double[realSize];

    public override void Perform(in Span<Polar> fftGrain, ref double onset)
    {
        onset = default;

        for (var i = 0; i < fftGrain.Length; i++)
        {
            ref var grain = ref fftGrain[i];
            ref var mag = ref oldMagnitude[i];

            onset += grain.Norm * Math.Log(1 + (grain.Norm / (mag + 1e-1)));
            mag = grain.Norm;
        }

        if (double.IsNaN(onset)) onset = default;
    }
}
