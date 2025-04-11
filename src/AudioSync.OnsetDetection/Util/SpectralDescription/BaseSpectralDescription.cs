using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util.SpectralDescription;

internal abstract class BaseSpectralDescription
{
    public abstract void Perform(in Span<Polar> fftGrain, ref float onset);
}
