using AudioSync.OnsetDetection.Structures;
using AudioSync.OnsetDetection.Util;
using AudioSync.OnsetDetection.Util.SpectralDescription;
using AudioSync.Util;
using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection;

public sealed class OnsetDetector
{
    public int MinInterOnsetInterval { get; set; }

    public double MinInterOnsetIntervalSeconds
    {
        get => MinInterOnsetInterval / (double)sampleRate;
        set => MinInterOnsetInterval = (int)Math.Round(value * sampleRate);
    }

    public double MinInterOnsetIntervalMilliseconds
    {
        get => MinInterOnsetIntervalSeconds * 1000;
        set => MinInterOnsetIntervalSeconds = value / 1000;
    }

    public int LastOffset => lastOnset - Delay;

    public double SilenceThreshold { get; set; }

    public int Delay { get; set; }

    public double Threshold
    {
        get => peakPicker.Threshold;
        set => peakPicker.Threshold = value;
    }

    public double Compression
    {
        get => applyCompression ? lambdaCompression : 0.0;
        set
        {
            if (value < 0.0) throw new ArgumentOutOfRangeException("Compression must be greater than or equal to 0.");
            lambdaCompression = value;
            applyCompression = lambdaCompression > 0.0;
        }
    }

    public bool ApplyWhitening { get; set; }

    private readonly PhaseVocoder phaseVocoder;
    private readonly BaseSpectralDescription spectralDescription;
    private readonly PeakPicker peakPicker;
    private readonly AdaptiveWhitening adaptiveWhitening;

    private readonly int sampleRate;
    private readonly int hopSize;
    private readonly int bufferSize;
    private readonly int realSize;

    private double spectralOutput;
    private int totalFrames;
    private int lastOnset;
    private bool applyCompression;
    private double lambdaCompression;

    public OnsetDetector(OnsetType onsetType, int bufferSize, int hopSize, int sampleRate)
    {
        this.sampleRate = sampleRate;
        this.hopSize = hopSize;
        this.bufferSize = bufferSize;

        // The size of the real half of our buffer (the other half is entirely in the imaginary plane)
        realSize = (bufferSize / 2) + 1;

        phaseVocoder = new(bufferSize, realSize, hopSize);
        peakPicker = new();
        adaptiveWhitening = new(bufferSize, hopSize, sampleRate);
        spectralOutput = 0.0;

        // Default values (before we re-initialize based on onset type)
        Threshold = 0.3;
        Delay = (int)(4.3 * hopSize);
        MinInterOnsetIntervalMilliseconds = 50;
        SilenceThreshold = -70;
        ApplyWhitening = false;
        Compression = 0.0;

        // Change settings based on onset type
        switch (onsetType)
        {
            case OnsetType.HighFrequencyContent:
                Threshold = 0.058;
                Compression = 1;
                spectralDescription = new HighFrequencyContent();
                break;
            case OnsetType.ComplexDomain:
                Delay = (int)(4.6 * hopSize);
                Threshold = 0.15;
                ApplyWhitening = true;
                Compression = 1;
                spectralDescription = new ComplexDomain(realSize);
                break;
            case OnsetType.KullbackLiebler:
                Threshold = 0.35;
                ApplyWhitening = true;
                Compression = 0.02;
                spectralDescription = new KullbackLiebler(realSize);
                break;
            case OnsetType.SpectralFlux:
                Threshold = 0.18;
                ApplyWhitening = true;
                adaptiveWhitening.RelaxTime = 100;
                adaptiveWhitening.Floor = 1;
                Compression = 10;
                spectralDescription = new SpectralFlux(realSize);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(onsetType));
        }

        Reset();
    }

    public void DetectOnsets(in Span<double> input, ref double onset)
    {
        // Execute phase vocoding on our input.
        // It is possible to multi-thread this Phase Vocoder step by executing it before Onset Detection.
        // In my personal testing, it seems to save about 0.3 seconds, *but* requires more than double the memory.
        Span<Polar> fftGrain = stackalloc Polar[realSize];
        phaseVocoder.Process(in input, ref fftGrain);

        // Apply whitening if enabled
        if (ApplyWhitening)
        {
            adaptiveWhitening.Do(ref fftGrain);
        }

        // Apply compression if enabled
        if (applyCompression)
        {
            for (var i = 0; i < fftGrain.Length; i++)
            {
                fftGrain[i] = fftGrain[i].LogMag(lambdaCompression);
            }
        }

        // Execute Spectral Description
        spectralDescription.Perform(in fftGrain, ref spectralOutput);

        // Execute Peak Picker
        peakPicker.FindPeak(in spectralOutput, ref onset);

        totalFrames += hopSize;
        
        // Check if we have an onset
        if (onset > 0)
        {
            // Silent onsets should not count as an onset
            if (Utils.IsSilence(in input, SilenceThreshold))
            {
                onset = 0;
                return;
            }

            // Calculate new onset time
            var newOnset = totalFrames + (int)Math.Round(onset * hopSize);

            // Check if this onset far enough away and that it's not blocked by delay
            if (lastOnset + MinInterOnsetInterval >= newOnset || (lastOnset > 0 && Delay > newOnset))
            {
                onset = 0.0;
                return;
            }

            lastOnset = Math.Max(Delay, newOnset);
            return;
        }
        // Check if we are at the beginning of the file with no silence
        else if (totalFrames <= Delay && !Utils.IsSilence(in input, SilenceThreshold))
        {
            var newOnset = totalFrames;
            if (totalFrames == 0 || onset + MinInterOnsetInterval < newOnset)
            {
                onset = Delay / hopSize;
                lastOnset = totalFrames + Delay;
            }
        }
    }

    public void Reset()
    {
        lastOnset = 0;
        totalFrames = 0;
    }
}
