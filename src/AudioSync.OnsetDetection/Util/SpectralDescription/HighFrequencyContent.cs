using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util.SpectralDescription;

internal sealed class HighFrequencyContent : BaseSpectralDescription
{
    public override void Perform(in Span<Polar> fftGrain, ref double onset)
    {
        onset = default;

        for (var i = 0; i < fftGrain.Length; i++)
        {
            onset += (i + 1) * fftGrain[i].Norm;
        }
    }
}
