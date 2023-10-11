using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util;

internal sealed class AdaptiveWhitening
{
    // In seconds, between 22-446
    private const double defaultRelaxTime = 250;

    // Attenuation of roughly -60dB
    private const double defaultDecay = 0.001f;

    // Default floor
    private const double defaultFloor = 1e-4f;

    public double RelaxTime
    {
        get => relaxTime;
        set
        {
            relaxTime = value;
            decay = Math.Pow(defaultDecay, hopSize / sampleRate / relaxTime);
        }
    }

    public double Floor { get; set; }

    private readonly int hopSize;
    private readonly int sampleRate;
    private readonly double[] peaks;

    private double relaxTime;
    private double decay;

    public AdaptiveWhitening(int bufferSize, int hopSize, int sampleRate)
    {
        peaks = new double[(bufferSize / 2) + 1];
        this.hopSize = hopSize;
        this.sampleRate = sampleRate;
        
        Floor = defaultFloor;
        RelaxTime = defaultRelaxTime;

        Reset();
    }

    public void Do(ref Span<Polar> fftGrain)
    {
        var length = Math.Min(fftGrain.Length, peaks.Length);

        for (var i = 0; i < length; i++)
        {
            ref var polar = ref fftGrain[i];
            ref var peak = ref peaks[i];

            var newPeak = Math.Max(decay * peak, Floor);
            peak = Math.Max(polar.Norm, newPeak);

            polar = polar with { Norm = polar.Norm / peak };
        }
    }

    public void Reset() => Array.Fill(peaks, Floor);
}